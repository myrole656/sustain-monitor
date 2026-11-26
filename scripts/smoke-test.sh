#!/usr/bin/env bash
# Smoke test against a running deployment (used by the "Test deployment" GitHub Action).
#   API=http://localhost:8081 WEB=http://localhost:8080 ADMIN_EMAIL=... ADMIN_PASSWORD=... bash scripts/smoke-test.sh
set -euo pipefail
API=${API:-http://localhost:8081}
WEB=${WEB:-http://localhost:8080}
: "${ADMIN_EMAIL:?}" "${ADMIN_PASSWORD:?}"
pass() { echo "  ✔ $1"; }
fail() { echo "  ✘ $1"; exit 1; }
json() { curl -sS -H 'Content-Type: application/json' "$@"; }

echo "Waiting for API and website…"
for url in "$API/health" "$WEB/health"; do
  for i in $(seq 1 60); do
    if curl -sf "$url" >/dev/null; then pass "$url healthy"; break; fi
    [ "$i" = 60 ] && fail "$url did not become healthy"
    sleep 2
  done
done

echo "API"
TOKEN=$(json -X POST "$API/api/auth/login" -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}" | jq -r '.accessToken // empty')
[ -n "$TOKEN" ] && pass "platform admin signs in" || fail "admin login failed"
AUTH="Authorization: Bearer $TOKEN"

TOOLS=$(curl -sS -H "$AUTH" "$API/api/tools" | jq -r '[.[].code] | sort | join(",")')
[ "$TOOLS" = "ID,NREB" ] && pass "GBI tools seeded ($TOOLS)" || fail "GBI tools: '$TOOLS'"

SSM="2026$(shuf -i 10000000-99999999 -n 1)"
CEO="ceo$RANDOM@smoke.test"
CID=$(json -X POST "$API/api/companies/register" -d "{\"companyName\":\"Smoke Builders\",\"ssmNo\":\"$SSM\",\"cidbGrade\":\"G5\",\"ceoName\":\"Smoke CEO\",\"ceoEmail\":\"$CEO\",\"password\":\"Smoke2026Pass\"}" | jq -r '.id // empty')
[ -n "$CID" ] && pass "company registers (id $CID)" || fail "company registration failed"

STATUS=$(curl -sS -X POST -H "$AUTH" "$API/api/admin/companies/$CID/verify" | jq -r '.status // empty')
[ "$STATUS" = "Verified" ] && pass "admin verifies company" || fail "verify returned '$STATUS'"

CEO_TOKEN=$(json -X POST "$API/api/auth/login" -d "{\"email\":\"$CEO\",\"password\":\"Smoke2026Pass\"}" | jq -r '.accessToken // empty')
ROLE=$(curl -sS -H "Authorization: Bearer $CEO_TOKEN" "$API/api/company" | jq -r '.myRole // empty')
[ "$ROLE" = "CEO" ] && pass "CEO signs in and sees the company" || fail "CEO check returned '$ROLE'"

CODE=$(curl -s -o /dev/null -w '%{http_code}' "$API/api/projects")
[ "$CODE" = "401" ] && pass "API rejects calls without a token" || fail "expected 401, got $CODE"

CODE=$(curl -s -o /dev/null -w '%{http_code}' "$API/api/verify/not-a-real-token")
[ "$CODE" = "404" ] && pass "public e-cert check answers" || fail "verify endpoint returned $CODE"

echo "Website"
PAGE=$(curl -sS "$WEB/login")
echo "$PAGE" | grep -q "blazor.web" && pass "login page served" || fail "login page did not include the Blazor script"
# Script and styles are linked with fingerprinted names (e.g. blazor.web.<hash>.js); follow the real links.
JS=$(echo "$PAGE" | grep -oE 'src="[^"]*blazor\.web[^"]*\.js"' | head -1 | sed -E 's/^src="//; s/"$//')
CSS=$(echo "$PAGE" | grep -oE 'href="[^"]*app[^"]*\.css"' | head -1 | sed -E 's/^href="//; s/"$//')
[ -n "$JS" ] || fail "no Blazor script link on the page"
CODE=$(curl -s -o /dev/null -w '%{http_code}' "$WEB/${JS#/}")
[ "$CODE" = "200" ] && pass "Blazor script served ($JS)" || fail "$JS returned $CODE"
[ -n "$CSS" ] || fail "no stylesheet link on the page"
CODE=$(curl -s -o /dev/null -w '%{http_code}' "$WEB/${CSS#/}")
[ "$CODE" = "200" ] && pass "styles served ($CSS)" || fail "$CSS returned $CODE"

echo "All smoke tests passed."
