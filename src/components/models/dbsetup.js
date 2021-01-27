import Knex from 'knex';
import { Model as objection } from 'objection';

import dotenv from 'dotenv';
dotenv.config()

const dbs = {
    get public() {
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Functions/get#smart_self-overwriting_lazy_getters
        console.log('Public DB accessed');
        delete this.public;
        return this.public = Knex({
            client: 'mysql2',
            useNullAsDefault: true,
            connection: {
                host : process.env.MYSQL_HOST || '127.0.0.1',
                user : process.env.MYSQL_USER || '',
                password : process.env.MYSQL_PASSWORD || '',
                database : process.env.MYSQL_DB || 'testldapi'
            },
        });
    },

    get private() {
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Functions/get#smart_self-overwriting_lazy_getters
        console.log('Private DB accessed');
        delete this.private;
        return this.private = Knex({
            client: 'mysql2',
            useNullAsDefault: true,
            connection: {
                host : process.env.MYSQL_HOST || '127.0.0.1',
                user : process.env.MYSQL_USER || '',
                password : process.env.MYSQL_PASSWORD || '',
                database : process.env.MYSQL_DB || 'testldapi' // `${process.env.MYSQL_DB || 'testldapi'}pvt`
            },
        });
    }
}

// I don't understand why this is necessary, but import { Model } and then export { Model } isn't working. 2021-01 RM
const Model = objection;

export { dbs, Model };
