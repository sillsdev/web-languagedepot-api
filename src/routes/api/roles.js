import Role from '$components/models/Role';
import { dbs } from '$components/models/dbsetup';

export async function get() {
    try {
        const roles = await Role.query(dbs.public).select('id', 'name');
        return { status: 200, body: roles };
    } catch (error) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
