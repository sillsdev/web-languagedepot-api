import { dbs } from '$db/dbsetup';
import { missingRequiredParam } from '$utils/commonErrors';
import { allowSameUserOrAdmin } from '$utils/db/authRules';
import { getProjectsForUser } from '$utils/db/usersAndRoles';

export async function get({ params, path, query, headers }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    if (!params.rolename) {
        return missingRequiredParam('rolename', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const authResponse = await allowSameUserOrAdmin(db, { params, headers, allowBasicAuth: true });
    if (authResponse.status === 200) {
        return getProjectsForUser(db, params);
    } else {
        return authResponse;
    }
}
