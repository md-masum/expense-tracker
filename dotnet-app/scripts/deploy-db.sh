#!/usr/bin/env bash
set -euo pipefail

# Creates/updates the production database schema from EF Core migrations.
#
# NOTE: The application auto-migrates on startup (Program.cs MigrateAsync).
# This script is useful for:
#   - CI/CD pipelines that need to pre-apply migrations before a deploy.
#   - Generating an idempotent SQL file for DBA review.
#   - Environments where the app process does not have DDL permissions.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PROJECT_FILE="${REPO_ROOT}/dotnet-app/FinanceTracker.Web.csproj"
OUTPUT_DIR="${SCRIPT_DIR}/artifacts"
SQL_OUTPUT="${OUTPUT_DIR}/finance-tracker-idempotent.sql"

if [[ -z "${FINANCE_TRACKER_CONNECTION_STRING:-}" ]]; then
  echo "ERROR: FINANCE_TRACKER_CONNECTION_STRING is required."
  echo "Example:"
  echo "  export FINANCE_TRACKER_CONNECTION_STRING='Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;Encrypt=True'"
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK is not installed or not available in PATH."
  exit 1
fi

if ! dotnet ef --version >/dev/null 2>&1; then
  echo "ERROR: dotnet-ef is required. Install it with: dotnet tool install --global dotnet-ef"
  exit 1
fi

mkdir -p "${OUTPUT_DIR}"

echo "[1/3] Restoring project dependencies..."
dotnet restore "${PROJECT_FILE}"

echo "[2/3] Generating idempotent migration SQL..."
dotnet ef migrations script \
  --idempotent \
  --project "${PROJECT_FILE}" \
  --output "${SQL_OUTPUT}"

echo "Idempotent SQL generated at: ${SQL_OUTPUT}"

if [[ "${SKIP_APPLY:-false}" == "true" ]]; then
  echo "SKIP_APPLY=true, migration apply step skipped."
  exit 0
fi

echo "[3/3] Applying migrations to target database..."
dotnet ef database update \
  --project "${PROJECT_FILE}" \
  --connection "${FINANCE_TRACKER_CONNECTION_STRING}"

echo "Database schema is up to date."



