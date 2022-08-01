import { User } from '$lib/db/models';
import { onlyOne, catchSqlError, retryOnServerError } from '$lib/utils/commonSqlHandlers';
import type { TransactionOrKnex } from 'objection';

export interface LimitOffset {
    limit?: number,
    offset?: number,
}

export function allUsersQuery(db: TransactionOrKnex, params: LimitOffset = {}) {
    const { limit, offset } = params; // TODO: Check if this throws when properties not present
    let query = User.query(db);
    if (limit) {
        query = query.limit(limit);
    }
    if (offset) {
        query = query.offset(offset);
    }
    return query;
}

export function countAllUsersQuery(db: TransactionOrKnex, params: LimitOffset) {
    return allUsersQuery(db, params).resultSize();
}

export function getAllUsers(db: TransactionOrKnex, params: LimitOffset) {
    return catchSqlError(async () => {
        const users = await retryOnServerError(allUsersQuery(db, params));
        return { status: 200, body: users };
    });
}

export function oneUserQuery(db: TransactionOrKnex, username: string) {
    return User.query(db).where('login', username);
}

export function getOneUser(db: TransactionOrKnex, username: string) {
    const query = oneUserQuery(db, username);
    return onlyOne(query, 'username', 'username', user => ({ status: 200, body: user }));
}
