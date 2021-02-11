import { missingRequiredParam } from '$utils/commonErrors';
import { addUserWithRoleByProjectCode } from '$utils/db/usersAndRoles';
import { dbs } from '$db/dbsetup';

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
    return addUserWithRoleByProjectCode(db, params.projectCode, params.username, params.rolename);
}
