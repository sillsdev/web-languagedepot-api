import { dbs } from '$db/dbsetup';
import { basicAuthRequired, notAllowed } from '$utils/commonErrors';
import { verifyBasicAuth, makeJwt, getUserAndPassFromBasicAuth } from '$utils/db/auth';

export async function get({ query, headers }) {
    const basicAuth = getUserAndPassFromBasicAuth(headers);
    if (!basicAuth) {
        return basicAuthRequired();
    }
    const db = query.private ? dbs.private : dbs.public;
    const authUser = await verifyBasicAuth(db, headers);
    if (authUser) {
        var token = makeJwt(authUser.login);
        return { status: 200, body: { access_token: token, token_type: 'JWT', expires_in: 604800 } };  // 7 days = 604,800 seconds
    } else {
        return notAllowed();
    }
}
