import { dbs } from '$lib/db/dbsetup';
import { retryOnServerError } from '$lib/utils/commonSqlHandlers';
import { isAdmin } from '$lib/utils/db/authRules';
import { oneUserQuery } from '$lib/utils/db/users';

// GET /api/v2/users/{username}/isAdmin - check whether username in question is a site admin
// Security: anonymous access allowed (used by Language Forge UI to check whether user should be shown LD admin pages)
// TODO: Consider whether this should be restricted to "only this user or admin", e.g. only allow checking *own* admin status, and not everyone's (although site admins can check everyone's status)
export async function get({ query, params }) {
    const db = query.private ? dbs.private : dbs.public;
    // const authResult = await allowSameUserOrAdmin(db, { params, headers });
    // if (authResult.status === 200) {
    const users = await retryOnServerError(oneUserQuery(db, params.username).select('admin'));
    if (users && users.length > 0) {
        const result = await isAdmin(users[0]);
        return { status: 200, body: { isAdmin: result } };
    } else {
        return { status: 404, body: { isAdmin: false } };
    }
    // } else {
    //     return authResult;
    // }
}
