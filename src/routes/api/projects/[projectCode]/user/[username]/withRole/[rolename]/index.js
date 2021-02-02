import { missingRequiredParam } from '$utils/commonErrors';
import { addUserWithRole } from '$utils/db/usersAndRoles';
import { dbs } from '$components/models/dbsetup';

export async function post({ params, path, query }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    if (!params.rolename) {
        return missingRequiredParam('rolename', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    return addUserWithRole(params.projectCode, params.username, params.rolename, db);
}
