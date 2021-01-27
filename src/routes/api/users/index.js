import User from '$components/models/User';
import { dbs } from '$components/models/dbsetup';

export async function get() {
    try {
        const users = await User.query(dbs.private);
        return { status: 200, body: users };
    } catch (error) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
