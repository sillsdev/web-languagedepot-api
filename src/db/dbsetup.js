import Knex from 'knex';

import dotenv from 'dotenv';
dotenv.config()

const dbs = {
    get public() {
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Functions/get#smart_self-overwriting_lazy_getters
        delete this.public;
        return this.public = Knex({
            client: 'mysql2',
            useNullAsDefault: true,
            connection: {
                host : process.env.MYSQL_HOST || '127.0.0.1',
                user : process.env.MYSQL_USER || '',
                port : process.env.MYSQL_PORT || '3306',
                password : process.env.MYSQL_PASSWORD || '',
                database : process.env.MYSQL_DATABASE || 'testldapi'
            },
        });
    },

    get private() {
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Functions/get#smart_self-overwriting_lazy_getters
        delete this.private;
        return this.private = Knex({
            client: 'mysql2',
            useNullAsDefault: true,
            connection: {
                host : process.env.MYSQL_HOST || '127.0.0.1',
                user : process.env.MYSQL_USER || '',
                port : process.env.MYSQL_PORT || '3306',
                password : process.env.MYSQL_PASSWORD_PRIVATE || process.env.MYSQL_PASSWORD_PVT || process.env.MYSQL_PASSWORD || '',
                database : process.env.MYSQL_DATABASE_PRIVATE || process.env.MYSQL_DATABASE_PVT || (process.env.MYSQL_DATABASE ? `${process.env.MYSQL_DATABASE}pvt` : 'testldapi')
            },
        });
    }
}

export { dbs };
