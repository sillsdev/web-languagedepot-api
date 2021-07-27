import { dbs } from '$lib/db/dbsetup';
import { jsonRequired, missingRequiredParam } from '$lib/utils/commonErrors';
import { retryOnServerError } from '$lib/utils/commonSqlHandlers';
import { allowSameUserOrAdmin } from '$lib/utils/db/authRules';
import { patchUser, deleteUser, createUser } from '$lib/utils/db/users';
import { getOneUser, oneUserQuery } from '$lib/utils/db/userQueries';

// GET /api/v2/users/{username} - get details about one user
// Security: must be user in question or a site admin
export async function get({ params, path, query, headers }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowSameUserOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        return getOneUser(db, params.username);
    } else {
        return authResult;
    }
}

// HEAD /api/v2/users/{username} - check whether username already exists (200 if it does, 404 if it does not)
// Security: anonymous access allowed
export async function head({ params, query }) {
    if (!params.username) {
        return { status: 400, body: {} };
    }
    const db = query.private ? dbs.private : dbs.public;
    try {
        const userCount = await retryOnServerError(oneUserQuery(db, params.username).resultSize());
        const status = userCount < 1 ? 404 : userCount > 1 ? 500 : 200;
        return { status, body: {} };
    } catch (error) {
        return { status: 500, body: {} };
    }
}

// PUT /api/v2/users/{username} - update or create details about one user (if update, update should be complete user record)
// Security: anyone may create an account, but only that user (or a site admin) should be able to update the account details
export async function put({ path, params, body, query, headers }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    if (typeof body !== 'object') {
        return jsonRequired('PUT', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    // Security check is done in createUser()
    const result = await createUser(db, params.username, body, headers);
    // Content-Location not strictly needed here, but add it for consistency
    if (result && result.status && result.status >= 200 && result.status < 300) {
        result.headers = { ...result.headers, 'Content-Location': `${path}` };
    }
    return result;
}

// PATCH /api/v2/users/{username} - update details about one user (details can be partial, e.g. {password: "newPassword"})
// Security: must be user in question or a site admin
export async function patch({ path, params, body, query, headers }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    if (typeof body !== 'object') {
        return jsonRequired('PATCH', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowSameUserOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        const result = await patchUser(db, params.username, body);
        // Content-Location not strictly needed here, but add it for consistency
        if (result && result.status && result.status >= 200 && result.status < 300) {
            result.headers = { ...result.headers, 'Content-Location': `${path}` };
        }
        return result;
    } else {
        return authResult;
    }
}

// DELETE /api/v2/users/{username} - unregister (delete) user from site
// Security: must be user in question or a site admin
export async function del({ params, path, query, headers }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowSameUserOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        return deleteUser(db, params.username);
    } else {
        return authResult;
    }
}
