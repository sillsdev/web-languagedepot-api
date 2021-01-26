import Knex from 'knex';
import { Model as objection } from 'objection';

import dotenv from 'dotenv';
dotenv.config()

const knex = Knex({
    client: 'mysql2',
    useNullAsDefault: true,
    connection: {
        host : process.env.MYSQL_HOST || '127.0.0.1',
        user : process.env.MYSQL_USER || '',
        password : process.env.MYSQL_PASSWORD || '',
        database : process.env.MYSQL_DB || 'testldapi'
    },
});

objection.knex(knex);

const Model = objection;

export { knex, Model };
