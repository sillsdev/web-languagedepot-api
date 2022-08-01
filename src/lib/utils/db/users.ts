import { User, Membership, MemberRole, Email } from '$lib/db/models';
import { cannotUpdateMissing } from '$lib/utils/commonErrors';
import { atMostOne, retryOnServerError } from '$lib/utils/commonSqlHandlers';
import type { TransactionOrKnex } from 'objection';
import { allowSameUserOrAdmin } from './authRules';
import { oneUserQuery } from './userQueries';

export async function createUser(db: TransactionOrKnex, username: string, newUser: Record<string, any>, headers: Headers) {
    const trx = await User.startTransaction(db);
    const query = oneUserQuery(trx, username).select('id').forUpdate();
    const result = await atMostOne(query, 'username', 'username',
    async () => {
        const result = await retryOnServerError(User.query(trx).insertAndFetch(newUser));
        return { status: 201, body: result };
    },
    async (user: any) => {
        const authResult = await allowSameUserOrAdmin(db, { params: { username }, headers });
        if (authResult.status === 200) {
            const result = await retryOnServerError(User.query(trx).updateAndFetchById(user.id, newUser));
            return { status: 200, body: result };
        } else {
            return authResult;
        }
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}

export async function patchUser(db: TransactionOrKnex, username: string, updateData: Record<string, any>) {
    const trx = await User.startTransaction(db);
    const query = oneUserQuery(trx, username).select('id').forUpdate();
    const result = await atMostOne(query, 'username', 'username',
    () => {
        return cannotUpdateMissing(username, 'user');
    },
    async (user: any) => {
        // No need to check JWT in this function, as it's checked in the caller
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

export async function deleteUser(db: TransactionOrKnex, username: string) {
    const trx = await User.startTransaction(db);
    const query = oneUserQuery(trx, username).select('id').forUpdate();
    const result = await atMostOne(query, 'username', 'username',
    () => {
        // Deleting a non-existent item is not an error
        return { status: 204, body: {} };
    },
    async (user: any) => {
        // No need to check JWT in this function, as it's checked in the caller
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
