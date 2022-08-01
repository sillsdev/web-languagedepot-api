import type { RequestHandler } from '@sveltejs/kit';
import { missingRequiredParam } from '$lib/utils/commonErrors';
import { addUserWithRoleByProjectCode } from '$lib/utils/db/usersAndRoles';
import { dbs } from '$lib/db/dbsetup';
import { allowManagerOrAdmin } from '$lib/utils/db/authRules';

// POST /api/v2/projects/{projectCode}/user/{username}/withRole/{rolename} - add or update user's role in project
// rolename parameter should be a string like "Contributor" or "Manager", but integer IDs are also allowed
// Security: only project managers or admins allowed
export const POST: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', url.pathname);
    }
    if (!params.username) {
        return missingRequiredParam('username', url.pathname);
    }
    if (!params.rolename) {
        return missingRequiredParam('rolename', url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowManagerOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        return addUserWithRoleByProjectCode(db, params.projectCode, params.username, params.rolename);
    } else {
        return authResult;
    }
}
