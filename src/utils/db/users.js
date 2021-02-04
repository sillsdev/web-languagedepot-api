import { User, Membership, MemberRole, Email } from '$db/models';
import { cannotUpdateMissing } from '$utils/commonErrors';
import { atMostOne, onlyOne, catchSqlError } from '$utils/commonSqlHandlers';

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

export function oneUserQuery(db, username) {
    return User.query(db).where('login', username);
}

export function getOneUser(db, username) {
    const query = oneUserQuery(db, username);
    return onlyOne(query, 'username', 'username', user => ({ status: 200, body: user }));
}

export async function createUser(db, username, newUser) {
    // TODO: Decide whether special handling of password needs to belong here, or in API handlers
    const trx = await User.startTransaction(db);
    const query = oneUserQuery(trx, username).select('id').forUpdate();
    const result = await atMostOne(query, 'username', 'username',
    async () => {
        // TODO: Use the verifyPassword functions, or hashRedminePassword functions, for making the hashed_password value, then save it along the new user
        const result = await User.query(trx).insertAndFetch(newUser);
        return { status: 201, body: result };
    },
    async (user) => {
        // TODO: Here, we're updating the whole user account, so we need to set (or maintain) the password.
        // Check whether the clear password exists, and if it does *not*, then don't change the current password.
        // A salt should *not* be accepted, I don't think.
        const result = await User.query(trx).updateAndFetchById(user.id, body);
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
        const result = await User.query(trx).patchAndFetchById(user.id, updateData);
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
        await MemberRole.query(trx).whereIn('member_id', membershipsQuery).delete();
        await membershipsQuery.delete();
        await Email.query(trx).where('user_id', user.id).delete()
        await User.query(trx).deleteById(user.id);
        return { status: 204, body: {} };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}