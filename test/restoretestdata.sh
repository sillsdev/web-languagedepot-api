#!/bin/bash

SCRIPT_PATH=$(dirname ${BASH_SOURCE[0]})

yesno() {
    read -p "Please press 'y' to continue, anything else to quit: " -n 1
    echo
    case "$REPLY" in
        [Yy]* ) true;;
        * ) false;;
    esac
}

if [ "$POSTGRES_HOST" ];
then
    # Set defaults if not set
    : ${POSTGRES_DATABASE:=testldapi}
    : ${POSTGRES_PORT=5432}
    echo "Will load test data into Postgres DB ${POSTGRES_DATABASE} on ${POSTGRES_HOST}:${POSTGRES_PORT}"
    yesno || exit
    PGPASSWORD="$POSTGRES_PASSWORD" psql -U "$POSTGRES_USER" -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" $POSTGRES_DATABASE < ${SCRIPT_PATH}/pg-testlanguagedepot.sql
else
    # Set defaults if not set
    : ${MYSQL_HOST=localhost}
    : ${MYSQL_DATABASE:=testldapi}
    : ${MYSQL_PORT=5432}
    echo "Will load test data into MySQL DB ${MYSQL_DATABASE} on ${MYSQL_HOST}:${MYSQL_PORT}"
    yesno || exit
    mysql -u "$MYSQL_USER" --password="$MYSQL_PASSWORD" -h "$MYSQL_HOST" -P "$MYSQL_PORT" $MYSQL_DATABASE < ${SCRIPT_PATH}/testlanguagedepot.sql
fi
