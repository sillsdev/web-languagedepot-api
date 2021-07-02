import { Role } from '$lib/db/models';
import { dbs } from '$lib/db/dbsetup';
import { retryOnServerError } from '$lib/utils/commonSqlHandlers';

// GET /api/v2/roles - return list of all roles
// Security: anonymous access allowed
export async function get({ query }) {
    const db = query.private ? dbs.private : dbs.public;
    try {
        const roles = await retryOnServerError(Role.query(db).select('id', 'name'));
        return { status: 200, body: roles };
    } catch (error) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
