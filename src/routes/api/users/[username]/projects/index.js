import { dbs } from '$components/models/dbsetup';
import { getProjectsForUser } from '$utils/db/usersAndRoles';

export async function get({ params, path, query }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    return getProjectsForUser(db, params);
}
