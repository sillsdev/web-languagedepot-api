import { dbs } from '$lib/db/dbsetup';
import { retryOnServerError } from '$lib/utils/commonSqlHandlers';
import { isAdmin } from '$lib/utils/db/authRules';
import { oneUserQuery } from '$lib/utils/db/users';

export async function get({ query, params }) {
    const db = query.private ? dbs.private : dbs.public;
    const users = await retryOnServerError(oneUserQuery(db, params.username).select('admin'));
    if (users && users.length > 0) {
        const result = await isAdmin(users[0]);
        return { status: 200, body: { isAdmin: result } };
    } else {
        return { status: 404, body: { isAdmin: false } };
    }
}

export async function head(req) {
    const { status, headers } = await get(req);
    return { status, headers, body: {} };
}
