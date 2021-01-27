import { User } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { withoutKey } from '$utils/withoutKey';

export async function get({ params }) {
    if (!params.username) {
        return { status: 500, body: { description: 'No username specified', code: 'missing_username' }};
    }
    try {
        const users = await User.query(dbs.private).where('login', params.username);
        if (users.length < 1) {
            return { status: 500, body: { description: 'No such user', code: 'unknown_username' }};
        } else if (users.length > 1) {
            return { status: 500, body: { description: 'Duplicate username', code: 'duplicate_username' }};
        }
        return { status: 200, body: withoutKey('hashed_password', users[0]) };
    } catch (error) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
// TODO: Build a library of standard errors and reference them here (functions or just looked up by code)

export async function post({ params }) {
    console.log('TODO: Create user with POST')
    return { status: 200, body: "Post user\n" };
}

export async function put({ params }) {
    console.log('TODO: Create user with PUT');
    return { status: 200, body: "Put user\n" };
}

export async function del({ params }) {
    console.log('TODO: Delete user');
    return { status: 200, body: "Delete user\n" };
}
