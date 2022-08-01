import type { RequestHandler } from '@sveltejs/kit';
import { dbs } from '$lib/db/dbsetup';
import { basicAuthRequired, notAllowed } from '$lib/utils/commonErrors';
import { verifyBasicAuth, makeJwt } from '$lib/utils/db/auth';

// GET /api/v2/login - verify HTTP basic auth and, if password matches, return JWT token allowing API access for one week
// Security: anyone may access, but only valid logins will be given a JWT token
export const GET: RequestHandler = async ({ url, request }) => {
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;
    const authUser = await verifyBasicAuth(db, request.headers);
    if (authUser) {
        var token = makeJwt((authUser as any).login);
        return { status: 200, body: { access_token: token, token_type: 'JWT', expires_in: 604800 } };  // 7 days = 604,800 seconds
    } else {
        if (authUser === undefined) {
            return basicAuthRequired();
        } else {
            return notAllowed();
        }
    }
}
