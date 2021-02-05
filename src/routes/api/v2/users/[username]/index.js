import { dbs } from '$db/dbsetup';
import { jsonRequired, missingRequiredParam } from '$utils/commonErrors';
import { getOneUser, oneUserQuery, patchUser, deleteUser } from '$utils/db/users';

export async function get({ params, path, query }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    return getOneUser(db, params.username);
}

export async function head({ params, query }) {
    if (!params.username) {
        return { status: 400, body: {} };
    }
    const db = query.private ? dbs.private : dbs.public;
    try {
        const userCount = await oneUserQuery(db, params.username).resultSize();
        const status = userCount < 1 ? 404 : userCount > 1 ? 500 : 200;
        return { status, body: {} };
    } catch (error) {
        return { status: 500, body: {} };
    }
}

export async function put({ path, params, body, query }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    if (typeof body !== 'object') {
        return jsonRequired('PATCH', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const result = await createUser(db, params.username, body);
    // Content-Location not strictly needed here, but add it for consistency
    if (result && result.status && result.status >= 200 && result.status < 300) {
        result.headers = { ...result.headers, 'Content-Location': `${path}` };
    }
    return result;
}

export async function patch({ path, params, body, query }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    if (typeof body !== 'object') {
        return jsonRequired('PATCH', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const result = patchUser(db, params.username, body);
    // Add Content-Location header on success so client knows where to find the newly-created project
    if (result && result.status && result.status >= 200 && result.status < 300) {
        result.headers = { ...result.headers, 'Content-Location': `${path}/${username}` };
    }
    return result;
}

export async function del({ params, path, query }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const result = await deleteUser(db, params.username);
    return result;
}
