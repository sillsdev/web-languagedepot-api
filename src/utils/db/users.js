import { User } from '$components/models/models';
import { atMostOne, catchSqlError } from '$utils/commonSqlHandlers';

export function allUsersQuery(db, { limit, offset } = {}) {
    let query = User.query(db);
    if (limit) {
        query = query.limit(limit);
    }
    if (offset) {
        query = query.offset(offset);
    }
    return query;
}

export function countAllUsersQuery(db, params) {
    return allUsersQuery(db, params).resultSize();
}

export function getAllUsers(db, params) {
    return catchSqlError(async () => {
        const users = await allUsersQuery(db, params);
        return { status: 200, body: users };
    });
}

export async function createUser(db, username, newUser) {
    // TODO: Decide whether special handling of password needs to belong here, or in API handlers
    const trx = User.startTransaction(db);
    const query = User.query(trx).select('id').forUpdate().where('username', username);
    const result = await atMostOne(query, 'username', 'username',
    async () => {
        const result = await User.query(trx).insertAndFetch(newUser);
        return { status: 201, body: result };
    },
    async (user) => {
        const result = await User.query(trx).updateAndFetchById(user.id, body);
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}