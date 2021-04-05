import { dbs } from '$lib/db/dbsetup';
import { jsonRequired, missingRequiredParam } from '$lib/utils/commonErrors';
import { retryOnServerError } from '$lib/utils/commonSqlHandlers';
import { allowSameUserOrAdmin } from '$lib/utils/db/authRules';
import { getOneUser, oneUserQuery, patchUser, deleteUser, createUser } from '$lib/utils/db/users';

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

export async function put({ path, params, body, query, headers }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    if (typeof body !== 'object') {
        return jsonRequired('PATCH', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowSameUserOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        const result = await createUser(db, params.username, body);
        // Content-Location not strictly needed here, but add it for consistency
        if (result && result.status && result.status >= 200 && result.status < 300) {
            result.headers = { ...result.headers, 'Content-Location': `${path}` };
        }
        return result;
    } else {
        return authResult;
    }
}

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
        const result = patchUser(db, params.username, body);
        // Content-Location not strictly needed here, but add it for consistency
        if (result && result.status && result.status >= 200 && result.status < 300) {
            result.headers = { ...result.headers, 'Content-Location': `${path}` };
        }
        return result;
    } else {
        return authResult;
    }
}

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
