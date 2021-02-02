import { dbs } from '$components/models/dbsetup';
import { missingRequiredParam } from '$utils/commonErrors';
import { getProjectsForUser } from '$utils/db/usersAndRoles';

export async function get({ params, path, query }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    if (!params.rolename) {
        return missingRequiredParam('rolename', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    return getProjectsForUser(db, params);
}
