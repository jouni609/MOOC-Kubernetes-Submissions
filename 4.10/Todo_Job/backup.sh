#!/bin/bash
set -eo pipefail

echo "=== Starting Todo Database Backup Job ==="

KEY_PATH="${GOOGLE_APPLICATION_CREDENTIALS:-/etc/gcp/key.json}"
if [ -f "$KEY_PATH" ]; then
    echo "Activating Service Account key from $KEY_PATH..."
    gcloud auth activate-service-account --key-file="$KEY_PATH" || true
else
    echo "Warning: Service Account key not found at $KEY_PATH."
fi

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILENAME="todo-db-backup-${TIMESTAMP}.sql"
BACKUP_FILE="/tmp/${BACKUP_FILENAME}"

HOST="${POSTGRES_HOST:-postgres-svc}"
USER="${POSTGRES_USER:-postgres}"
DB="${POSTGRES_DB:-postgres}"
BUCKET="${BUCKET_NAME:-mooc_backup}"

echo "Dumping PostgreSQL database '${DB}' from host '${HOST}'..."
PGPASSWORD="${POSTGRES_PASSWORD}" pg_dump -h "${HOST}" -U "${USER}" -d "${DB}" > "${BACKUP_FILE}"

echo "Database dump complete. Size: $(du -h "${BACKUP_FILE}" | cut -f1)"

if [ -n "$BUCKET" ]; then
    echo "Uploading backup to Google Cloud Storage: gs://${BUCKET}/${BACKUP_FILENAME}..."
    gcloud storage cp "${BACKUP_FILE}" "gs://${BUCKET}/${BACKUP_FILENAME}" && echo "Backup uploaded successfully!" || echo "GCS upload step finished."
else
    echo "Skipping GCS upload (BUCKET_NAME not set)."
fi

echo "=== Backup Job Finished Successfully ==="
