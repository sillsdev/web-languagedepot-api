import type { RequestHandler } from '@sveltejs/kit';
import { dbs } from '$lib/db/dbsetup';
import { jsonRequired, missingRequiredParam } from '$lib/utils/commonErrors';
import { allowSameUserOrAdmin } from '$lib/utils/db/authRules';
import { patchUser, deleteUser, createUser } from '$lib/utils/db/users';
import { getOneUser, oneUserQuery } from '$lib/utils/db/userQueries';

// GET /api/v2/users/{username} - get details about one user
// Security: must be user in question or a site admin
export const GET: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.username) {
        return missingRequiredParam('username', url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowSameUserOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        return getOneUser(db, params.username);
    } else {
        return authResult;
    }
}

// HEAD /api/v2/users/{username} - check whether username already exists (200 if it does, 404 if it does not)
// Security: anonymous access allowed
export const HEAD: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.username) {
        return { status: 400, body: {} };
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    try {
        const userCount = await oneUserQuery(db, params.username).resultSize();
        const status = userCount < 1 ? 404 : userCount > 1 ? 500 : 200;
        return { status, body: {} };
    } catch (error) {
        return { status: 500, body: {} };
    }
}

// PUT /api/v2/users/{username} - update or create details about one user (if update, update should be complete user record)
// Security: anyone may create an account, but only that user (or a site admin) should be able to update the account details
export const PUT: RequestHandler = async ({ params, url, request }) => {
    if (!params.username) {
        return missingRequiredParam('username', url.pathname);
    }
    let body: any;
    try {
        body = await request.json();
    } catch (e: any) {
        return jsonRequired(request.method, url.pathname);
    }
    if (!body || !body.username) {
        return missingRequiredParam('username', `body of PUT request to ${url.pathname}`);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;
    // Security check is done in createUser()
    const result = await createUser(db, params.username, body, request.headers);
    // Content-Location not strictly needed here, but add it for consistency
    if (result && result.status && result.status >= 200 && result.status < 300) {
        result.headers = { ...result.headers, 'Content-Location': url.pathname };
    }
    return result;
}

// PATCH /api/v2/users/{username} - update details about one user (details can be partial, e.g. {password: "newPassword"})
// Security: must be user in question or a site admin
export const PATCH: RequestHandler = async ({ params, url, request }) => {
    if (!params.username) {
        return missingRequiredParam('username', url.pathname);
    }
    let body: any;
    try {
        body = await request.json();
    } catch (e: any) {
        return jsonRequired(request.method, url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowSameUserOrAdmin(db, { params, headers: request.headers });
    if (authResult.status === 200) {
        const result = await patchUser(db, params.username, body);
        // Content-Location not strictly needed here, but add it for consistency
        if (result && result.status && result.status >= 200 && result.status < 300) {
            result.headers = { ...result.headers, 'Content-Location': url.pathname };
        }
        return result;
    } else {
        return authResult;
    }
}

// DELETE /api/v2/users/{username} - unregister (delete) user from site
// Security: must be user in question or a site admin
export const DELETE: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.username) {
        return missingRequiredParam('username', url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowSameUserOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        return deleteUser(db, params.username);
    } else {
        return authResult;
    }
}
