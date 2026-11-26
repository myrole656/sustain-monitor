#!/usr/bin/env sh
# Restores a backup made by backup.sh.  Usage: ./restore.sh 20261126-021500
set -eu
cd "$(dirname "$0")"
STAMP=${1:?Usage: ./restore.sh <timestamp from the backup file names>}
VOLUME=sustainmonitor_api-data

docker compose stop api web
docker run --rm -v "$VOLUME":/data -v "$PWD/backups":/backup alpine:3 sh -c "
  rm -f /data/sustainmonitor.db /data/sustainmonitor.db-wal /data/sustainmonitor.db-shm &&
  cp /backup/sustainmonitor-$STAMP.db /data/sustainmonitor.db &&
  tar xzf /backup/evidence-$STAMP.tgz -C /data &&
  chown -R 1654:1654 /data
"
docker compose start api web
echo "Restored $STAMP"
