import { User } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { withoutKey } from '$utils/withoutKey';

export async function get() {
    try {
        const users = await User.query(dbs.private);
        const result = users.map(withoutKey('hashed_password'));
        return { status: 200, body: result };
    } catch (error) {
        console.log(error);
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
