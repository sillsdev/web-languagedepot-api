import { dbs } from '$db/dbsetup';
import { missingRequiredParam, authTokenRequired, notAllowed } from '$utils/commonErrors';
import { verifyBasicAuth, verifyJwtAuth } from '$utils/db/auth';
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
    let authUser = await verifyJwtAuth(db, headers);
    if (!authUser) {
        if (authUser === undefined) {
            // To interop with older clients, this route also allows user:pass in URL (HTTP basic auth)
            authUser = await verifyBasicAuth(db, headers);
            if (!authUser) {
                if (authUser === undefined) {
                    // No username or password was presented: return 401
                    return authTokenRequired();
                } else {
                    // Username and password were presented but they were wrong: return 403
                    return notAllowed();
                }
            }
        } else {
            return notAllowed();
        }
    }
    const authResponse = allowSameUserOrAdmin({ params, authUser });
    if (authResponse.status === 200) {
        return getProjectsForUser(db, params);
    } else {
        return authResponse;
    }
}
