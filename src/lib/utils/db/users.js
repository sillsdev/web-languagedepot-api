import { User, Membership, MemberRole, Email } from '$lib/db/models';
import { cannotUpdateMissing } from '$lib/utils/commonErrors';
import { atMostOne, onlyOne, catchSqlError, retryOnServerError } from '$lib/utils/commonSqlHandlers';

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

export async function createUser(db, username, newUser) {
    const trx = await User.startTransaction(db);
    const query = oneUserQuery(trx, username).select('id').forUpdate();
    const result = await atMostOne(query, 'username', 'username',
    async () => {
        const result = await retryOnServerError(User.query(trx).insertAndFetch(newUser));
        return { status: 201, body: result };
    },
    async (user) => {
        const result = await retryOnServerError(User.query(trx).updateAndFetchById(user.id, newUser));
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}

export async function patchUser(db, username, updateData) {
    const trx = await User.startTransaction(db);
    const query = oneUserQuery(trx, username).select('id').forUpdate();
    const result = await atMostOne(query, 'username', 'username',
    () => {
        return cannotUpdateMissing(username, 'user');
    },
    async (user) => {
        const result = await retryOnServerError(User.query(trx).patchAndFetchById(user.id, updateData));
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}

export async function deleteUser(db, username) {
    const trx = await User.startTransaction(db);
    const query = oneUserQuery(trx, username).select('id').forUpdate();
    const result = await atMostOne(query, 'username', 'username',
    () => {
        // Deleting a non-existent item is not an error
        return { status: 204, body: {} };
    },
    async (user) => {
        // Delete memberships and email addresses first so there's never any DB inconsistency
        const membershipsQuery = Membership.query(trx).where('user_id', user.id).select('id')
        await retryOnServerError(MemberRole.query(trx).whereIn('member_id', membershipsQuery).delete());
        await retryOnServerError(membershipsQuery.delete());
        await retryOnServerError(Email.query(trx).where('user_id', user.id).delete());
        await retryOnServerError(User.query(trx).deleteById(user.id));
        return { status: 204, body: {} };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}