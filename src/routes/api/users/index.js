import { User } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { jsonRequired, missingRequiredParam } from '$utils/commonErrors';
import { atMostOne, catchSqlError } from '$utils/commonSqlHandlers';

export async function get({ query }) {
    const db = query.private ? dbs.private : dbs.public;
    return catchSqlError(async () => {
        const search = User.query(db);
        if (typeof query.limit === 'number') {
            search = search.limit(query.limit);
        }
        if (typeof query.offset === 'number') {
            search = search.offset(query.offset);
        }
        const users = await search;
        console.log('Users result:', users);
        return { status: 200, body: users };
    });
}

// TODO: Handle HEAD, which should either return 200 if there are users, 404 if there are none, or 403 Unauthorized if you're supposed to be logged in to access this
// export async function head({ path }) {
//     console.log(`HEAD ${path} called`);
//     return { status: 204, body: {} }
// }

export async function post({ path, body, query }) {
    if (typeof body !== 'object') {
        return jsonRequired('POST', path);
    }
    if (!body || !body.username) {
        return missingRequiredParam('username', 'body of POST request');
    }
    // TODO: Password needs special handling
    const username = body.username;
    const db = query.private ? dbs.private : dbs.public;
    const trx = Project.startTransaction(db);
    const query = User.query(trx).select('id').forUpdate().where('login', username);
    const result = await atMostOne(query, 'username', 'username',
    async () => {
        const result = await User.query(trx).insertAndFetch(body);
        return { status: 201, body: result, headers: { location: `${path}/${username}`} };
    },
    async (user) => {
        const result = await User.query(trx).updateAndFetchById(user.id, body);
        return { status: 200, body: result, headers: { location: `${path}/${username}`} };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}
