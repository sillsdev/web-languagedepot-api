import { dbs } from '$lib/db/dbsetup';
import { basicAuthRequired, notAllowed } from '$lib/utils/commonErrors';
import { verifyBasicAuth, makeJwt } from '$lib/utils/db/auth';

export async function get({ query, headers }) {
    const db = query.private ? dbs.private : dbs.public;
    const authUser = await verifyBasicAuth(db, headers);
    if (authUser) {
        var token = makeJwt(authUser.login);
        return { status: 200, body: { access_token: token, token_type: 'JWT', expires_in: 604800 } };  // 7 days = 604,800 seconds
    } else {
        if (authUser === undefined) {
            return basicAuthRequired();
        } else {
            return notAllowed();
        }
    }
}
