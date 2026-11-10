#!/usr/bin/env bash
# Apply the EF Core migrations to the database of one environment. The migration bundle (efbundle, built by the
# pipeline) carries every migration; it applies only the ones the database does not have yet.
#   deploy/migrate.sh staging|production
#
# PostgreSQL listens on the server but is not reachable from the runner. The job opens an SSH tunnel through the
# deploy account instead (runner -> deploy:6022 -> 127.0.0.1:5432), so no database port is ever opened to the network.
set -euo pipefail

ENVIRONMENT="${1:?usage: migrate.sh staging|production}"
: "${DB_PASSWORD:?DB_PASSWORD is not set (a CI/CD variable scoped to the environment ${ENVIRONMENT})}"
DB="dotnet_api_${ENVIRONMENT}"
LOCAL_PORT=15432

echo "==> tunnel to PostgreSQL on the server"
ssh -f -N -o ExitOnForwardFailure=yes -L "${LOCAL_PORT}:127.0.0.1:5432" deploy
trap 'pkill -f "ssh -f -N .*${LOCAL_PORT}:127.0.0.1:5432" || true' EXIT

echo "==> migrate ${DB}"
chmod +x efbundle
./efbundle --connection "Host=127.0.0.1;Port=${LOCAL_PORT};Database=${DB};Username=${DB};Password=${DB_PASSWORD}"
