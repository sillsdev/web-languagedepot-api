import { User } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { jsonRequired, cannotUpdateMissing, missingRequiredParam } from '$utils/commonErrors';
import { onlyOne, atMostOne, catchSqlError } from '$utils/commonSqlHandlers';

export async function get() {
    return catchSqlError(async () => {
        const users = await User.query(dbs.public);
        console.log('Users result:', users);
        return { status: 200, body: users };
    });
}

// TODO: Handle HEAD, which should either return 200 if there are users, 404 if there are none, or 403 Unauthorized if you're supposed to be logged in to access this
// export async function head({ path }) {
//     console.log(`HEAD ${path} called`);
//     return { status: 204, body: {} }
// }

export async function post({ path, body }) {
    console.log(`POST /api/users received:`, body);
    // TODO: Transaction
    if (typeof body !== 'object') {
        return jsonRequired('POST', path);
    }
    if (!body || !body.username) {
        return missingRequiredParam('username', 'body of POST request');
    }
    // TODO: Password needs special handling
    const username = body.username;
    const users = await User.query(dbs.public).select('id').forUpdate().where('login', username);
    return atMostOne(users, 'username', 'username',
    async () => {
        const result = await User.query(dbs.public).insertAndFetch(body);
        return { status: 201, body: result, headers: { location: path } };
    },
    async (user) => {
        const result = await User.query(dbs.public).updateAndFetchById(user.id, body);
        return { status: 200, body: result, headers: { location: `${path}/${username}`} };
    });
}
