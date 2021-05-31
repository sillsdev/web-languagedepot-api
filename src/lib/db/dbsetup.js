import Knex from 'knex';

import dotenv from 'dotenv';
dotenv.config()

let db = {};
console.log('Running db setup...')
if (process.env['POSTGRES_HOST']) {
    console.log('  db driver: pg (postgres)')
    db.client = 'pg'
    db.host = process.env['POSTGRES_HOST'] || '127.0.0.1',
    db.user = process.env['POSTGRES_USER'] || '',
    db.port = process.env['POSTGRES_PORT'] || '5432',
    db.password = process.env['POSTGRES_PASSWORD'] || '',
    db.database = process.env['POSTGRES_DATABASE'] || 'testldapi'
    db.passwordPvt = process.env['POSTGRES_PASSWORD_PRIVATE'] || process.env['POSTGRES_PASSWORD_PVT'] || process.env['POSTGRES_PASSWORD'] || '',
    db.databasePvt = process.env['POSTGRES_DATABASE_PRIVATE'] || process.env['POSTGRES_DATABASE_PVT'] || (process.env['POSTGRES_DATABASE'] ? `${process.env['POSTGRES_DATABASE']}pvt` : 'testldapi')
} else {
    console.log('  db driver: mysql2')
    db.client = 'mysql2'
    db.host = process.env['MYSQL_HOST'] || '127.0.0.1',
    db.user = process.env['MYSQL_USER'] || '',
    db.port = process.env['MYSQL_PORT'] || '3306',
    db.password = process.env['MYSQL_PASSWORD'] || '',
    db.database = process.env['MYSQL_DATABASE'] || 'testldapi'
    db.passwordPvt = process.env['MYSQL_PASSWORD_PRIVATE'] || process.env['MYSQL_PASSWORD_PVT'] || process.env['MYSQL_PASSWORD'] || '',
    db.databasePvt = process.env['MYSQL_DATABASE_PRIVATE'] || process.env['MYSQL_DATABASE_PVT'] || (process.env['MYSQL_DATABASE'] ? `${process.env['MYSQL_DATABASE']}pvt` : 'testldapi')
}

const dbs = {
    get public() {
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Functions/get#smart_self-overwriting_lazy_getters
        // @ts-ignore
        delete this.public;
        // @ts-ignore
        return this.public = Knex({
            client: db.client,
            useNullAsDefault: true,
            connection: {
                host : db.host,
                user : db.user,
                port : parseInt(db.port),
                password : db.password,
                database : db.database,
            },
        });
    },

    get private() {
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Functions/get#smart_self-overwriting_lazy_getters
        // @ts-ignore
        delete this.private;
        // @ts-ignore
        return this.private = Knex({
            client: db.client,
            useNullAsDefault: true,
            connection: {
                host : db.host,
                user : db.user,
                port : parseInt(db.port),
                password : db.passwordPvt,
                database : db.databasePvt,
            },
        });
    }
}

export { dbs };
