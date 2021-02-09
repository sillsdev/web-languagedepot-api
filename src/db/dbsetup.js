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
                password : process.env.MYSQL_PASSWORD || '',
                database : process.env.MYSQL_DATABASE || 'testldapi' // `${process.env.MYSQL_DATABASE || 'testldapi'}pvt`
            },
        });
    }
}

export { dbs };
