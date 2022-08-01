import type { RequestHandler } from '@sveltejs/kit';
import { Role } from '$lib/db/models';
import { dbs } from '$lib/db/dbsetup';
import { retryOnServerError } from '$lib/utils/commonSqlHandlers';

// GET /api/v2/roles - return list of all roles
// Security: anonymous access allowed
export const GET: RequestHandler = async ({ url }) => {
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;
    try {
        const roles = await retryOnServerError(Role.query(db).select('id', 'name'));
        return { status: 200, body: roles };
    } catch (error: any) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
