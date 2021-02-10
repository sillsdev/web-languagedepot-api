import { dbs } from '$db/dbsetup';
import { jsonRequired, missingRequiredParam } from '$utils/commonErrors';
import { retryOnServerError } from '$utils/commonSqlHandlers';
import { getAllUsers, countAllUsersQuery, createUser } from '$utils/db/users';

export async function get({ query }) {
    const db = query.private ? dbs.private : dbs.public;
    // URLSearchParams objects don't destructure well, so convert to a POJO
    const queryParams = Object.fromEntries(query);
    return getAllUsers(db, queryParams);
}

export async function head({ query }) {
    const db = query.private ? dbs.private : dbs.public;
    const queryParams = Object.fromEntries(query);
    const count = await retryOnServerError(countAllUsersQuery(db, queryParams));
    const status = count > 0 ? 200 : 404;
    return { status, body: {} };
}

export async function post({ path, body, query }) {
    if (typeof body !== 'object') {
        return jsonRequired('POST', path);
    }
    if (!body || !body.username) {
        return missingRequiredParam('username', `body of POST request to ${path}`);
    }
    const username = body.username;
    const db = query.private ? dbs.private : dbs.public;
    const result = await createUser(db, username, body);
    // Add Content-Location header on success so client knows where to find the newly-created project
    if (result && result.status && result.status >= 200 && result.status < 300) {
        result.headers = { ...result.headers, 'Content-Location': `${path}/${username}` };
    }
    return result;
}
