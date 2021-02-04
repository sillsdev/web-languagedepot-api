import { Role } from '$db/models';
import { dbs } from '$db/dbsetup';

export async function get({ query }) {
    const db = query.private ? dbs.private : dbs.public;
    try {
        const roles = await Role.query(db).select('id', 'name');
        return { status: 200, body: roles };
    } catch (error) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
