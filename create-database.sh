#!/bin/bash

for _ in {1..50}; do
  if /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $MSSQL_SA_PASSWORD -i create-database.sql; then
    echo "Successfully created database"
    break
  fi
  echo "Failed to create database, trying again in 5 seconds..."
  sleep 5
done
