#!/usr/bin/env sh
# Backs up the SQLite database (safe while running) and evidence files to deploy/backups/.
# Schedule daily, e.g. crontab:  15 2 * * * cd /opt/sustainmonitor/deploy && ./backup.sh >> backups/backup.log 2>&1
set -eu
cd "$(dirname "$0")"
mkdir -p backups
STAMP=$(date +%Y%m%d-%H%M%S)
VOLUME=sustainmonitor_api-data

docker run --rm -v "$VOLUME":/data:ro -v "$PWD/backups":/backup alpine:3 sh -c "
  apk add --no-cache sqlite >/dev/null &&
  sqlite3 /data/sustainmonitor.db \".backup '/backup/sustainmonitor-$STAMP.db'\" &&
  tar czf /backup/evidence-$STAMP.tgz -C /data evidence keys
"

# Keep 30 days.
find backups -name 'sustainmonitor-*.db' -mtime +30 -delete
find backups -name 'evidence-*.tgz' -mtime +30 -delete
echo "Backup done: backups/sustainmonitor-$STAMP.db and backups/evidence-$STAMP.tgz"
