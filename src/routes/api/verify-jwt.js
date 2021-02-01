import { dbs } from '$components/models/dbsetup';
import { verifyJwtAuth } from '$utils/db/auth';

export async function get({ query, headers }) {
    const db = query.private ? dbs.private : dbs.public;
    const authUser = await verifyJwtAuth(db, headers);
    if (authUser) {
        return { status: 200, body: `Welcome, ${authUser.firstname} ${authUser.lastname}!` };
    } else {
        return { status: 403, body: { code: 'not_allowed', description: 'You\'re not welcome here' } };
    }
}
