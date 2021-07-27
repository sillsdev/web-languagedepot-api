import { User } from '$lib/db/models';
import { onlyOne, catchSqlError, retryOnServerError } from '$lib/utils/commonSqlHandlers';

export function allUsersQuery(db, { limit = undefined, offset = undefined } = {}) {
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
        const users = await retryOnServerError(allUsersQuery(db, params));
        return { status: 200, body: users };
    });
}

export function oneUserQuery(db, username) {
    return User.query(db).where('login', username);
}

export function getOneUser(db, username) {
    const query = oneUserQuery(db, username);
    return onlyOne(query, 'username', 'username', user => ({ status: 200, body: user }));
}
