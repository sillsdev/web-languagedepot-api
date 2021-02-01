import { dbs } from '$components/models/dbsetup';
import { verifyBasicAuth, makeJwt } from '$utils/db/auth';

export async function get({ query, headers }) {
    const db = query.private ? dbs.private : dbs.public;
    const authUser = await verifyBasicAuth(db, headers);
    if (authUser) {
        var token = makeJwt(authUser.login);
        return { status: 200, body: token };  // TODO: Make this a JSON structure with "access_token": token-string  (or should access_token be access-token?)
    } else {
        return { status: 401, body: { code: 'not_authorized', description: 'Please log in to get a JWT' } };  // TODO: Debug only, in production replace with real error from commonErrors
    }
}
