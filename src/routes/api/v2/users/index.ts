import type { RequestHandler } from '@sveltejs/kit';
import { dbs } from '$lib/db/dbsetup';
import { jsonRequired, missingRequiredParam } from '$lib/utils/commonErrors';
import { allowAdminOnly } from '$lib/utils/db/authRules';
import { createUser } from '$lib/utils/db/users';
import { getAllUsers } from '$lib/utils/db/userQueries';

// GET /api/v2/users - return list of all users
// Security: must be a site admin (list of all users could contain sensitive names or email addresses)
export const GET: RequestHandler = async ({ url, request }) => {
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;
    const authResult = await allowAdminOnly(db, request);
    if (authResult.status === 200) {
        // URLSearchParams objects don't destructure well, so convert to a POJO
        const queryParams = Object.fromEntries(url.searchParams);
        return getAllUsers(db, queryParams);
    } else {
        return authResult;
    }
}

// POST /api/v2/users - create user, or update user if it aleady exists.
// Security: anyone may create an account, but only that user (or a site admin) should be able to update the account details
export const POST: RequestHandler = async ({ url, request }) => {
    let body: any;
    try {
        body = await request.json();
    } catch (e: any) {
        // TODO: Consider letting this throw and letting Svelte-Kit turn the resulting exception into a 500 Server Error that will report the JSON error more precisely
        return jsonRequired('POST', url.pathname);
    }
    if (!body || !body.username) {
        return missingRequiredParam('username', `body of POST request to ${url.pathname}`);
    }
    const username = body.username;
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;
    // Security check is done in createUser()
    const result = await createUser(db, username, body, request.headers);
    // Add Content-Location header on success so client knows where to find the newly-created project
    if (result && result.status && result.status >= 200 && result.status < 300) {
        result.headers = { ...result.headers, 'Content-Location': `${url.pathname}/${username}` };
    }
    return result;
}
