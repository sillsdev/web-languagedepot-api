import type { RequestHandler } from '@sveltejs/kit';
import { dbs } from '$lib/db/dbsetup';
import { missingRequiredParam } from '$lib/utils/commonErrors';
import { allowSameUserOrAdmin } from '$lib/utils/db/authRules';
import { getProjectsForUser } from '$lib/utils/db/usersAndRoles';

// GET /api/v2/users/{username}/projects/withRole/{rolename} - Search for projects where given user has a specific role (e.g. "Manager")
// rolename parameter should be a string like "Contributor" or "Manager", but integer IDs are also allowed
// Security: must be user in question or a site admin
export const GET: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.username) {
        return missingRequiredParam('username', url.pathname);
    }
    if (!params.rolename) {
        return missingRequiredParam('rolename', url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResponse = await allowSameUserOrAdmin(db, { params, headers, allowBasicAuth: true });
    if (authResponse.status === 200) {
        return getProjectsForUser(db, params);
    } else {
        return authResponse;
    }
}
