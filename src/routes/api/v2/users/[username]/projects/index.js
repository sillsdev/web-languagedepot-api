import { dbs } from '$db/dbsetup';
import { authTokenRequired, missingRequiredParam } from '$utils/commonErrors';
import { verifyJwtAuth, verifyBasicAuth } from '$utils/db/auth';
import { allowSameUserOrAdmin } from '$utils/db/authRules';
import { getProjectsForUser } from '$utils/db/usersAndRoles';

export async function get({ params, path, query, headers }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    let authUser = await verifyJwtAuth(db, headers);
    if (!authUser) {
        // To interop with older clients, this route also allows user:pass in URL (HTTP basic auth)
        authUser = await verifyBasicAuth(db, headers);
        if (!authUser) {
            return authTokenRequired();
        }
    }
    const authResponse = allowSameUserOrAdmin({ params, authUser });
    if (authResponse.status === 200) {
        return getProjectsForUser(db, params);
    } else {
        return authResponse;
    }
}
