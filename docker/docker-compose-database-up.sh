#!/bin/bash
cd "$(dirname "$0")"

docker compose -f docker-compose-database.yml down

docker compose -f docker-compose-database.yml up --build
