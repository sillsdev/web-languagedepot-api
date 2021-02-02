import { dbs } from '$components/models/dbsetup';
import { isAdmin } from '$utils/db/authRules';
import { oneUserQuery } from '$utils/db/users';

export async function get({ query, params }) {
    const db = query.private ? dbs.private : dbs.public;
    const users = await oneUserQuery(db, params.username).select('admin');
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
