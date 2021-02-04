import Role from '$components/models/Role';
import { dbs } from '$components/models/dbsetup';

export async function get({ query }) {
    const db = query.private ? dbs.private : dbs.public;
    try {
        const roles = await Role.query(db).select('id', 'name');
        return { status: 200, body: roles };
    } catch (error) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
