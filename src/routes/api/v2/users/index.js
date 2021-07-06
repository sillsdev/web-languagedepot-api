import { dbs } from '$lib/db/dbsetup';
import { jsonRequired, missingRequiredParam } from '$lib/utils/commonErrors';
import { allowAdminOnly } from '$lib/utils/db/authRules';
import { getAllUsers, countAllUsersQuery, createUser } from '$lib/utils/db/users';

// GET /api/v2/users - return list of all users
// Security: must be a site admin (list of all users could contain sensitive names or email addresses)
export async function get({ query, headers }) {
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowAdminOnly(db, { headers });
    if (authResult.status === 200) {
        // URLSearchParams objects don't destructure well, so convert to a POJO
        const queryParams = Object.fromEntries(query);
        return getAllUsers(db, queryParams);
    } else {
        return authResult;
    }
}

// POST /api/v2/users - create user, or update user if it aleady exists.
// Security: anyone may create an account, but only that user (or a site admin) should be able to update the account details
export async function post({ path, body, query, headers }) {
    if (typeof body !== 'object') {
        return jsonRequired('POST', path);
    }
    if (!body || !body.username) {
        return missingRequiredParam('username', `body of POST request to ${path}`);
    }
    const username = body.username;
    const db = query.private ? dbs.private : dbs.public;
    // Security check is done in createUser()
    const result = await createUser(db, username, body, headers);
    // Add Content-Location header on success so client knows where to find the newly-created project
    if (result && result.status && result.status >= 200 && result.status < 300) {
        result.headers = { ...result.headers, 'Content-Location': `${path}/${username}` };
    }
    return result;
}
