import { missingRequiredParam } from '$utils/commonErrors';
import { addUserWithRole } from '$utils/db/usersAndRoles';
import { dbs } from '$components/models/dbsetup';

export async function post({ params, path }) {
    // console.log(`POST /api/users received:`, body);
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    if (!params.rolename) {
        return missingRequiredParam('rolename', path);
    }
    return addUserWithRole(params.projectCode, params.username, params.rolename, dbs.public);
}
