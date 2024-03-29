import type { RequestHandler } from '@sveltejs/kit';
import { dbs } from '$lib/db/dbsetup';
import { missingRequiredParam } from '$lib/utils/commonErrors';
import { allowSameUserOrAdmin } from '$lib/utils/db/authRules';
import { getProjectsForUser } from '$lib/utils/db/usersAndRoles';

// GET /api/v2/users/{username}/projects - List all projects a given user is a member of
// Security: must be user in question or a site admin
export const GET: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.username) {
        return missingRequiredParam('username', url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResponse = await allowSameUserOrAdmin(db, { params, headers, allowBasicAuth: true });
    if (authResponse.status === 200) {
        return getProjectsForUser(db, params);
    } else {
        return authResponse;
    }
}
