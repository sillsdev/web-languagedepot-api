import Role from '$components/models/Role';

export async function get() {
    try {
        const roles = await Role.query().select('id', 'name');
        return { status: 200, body: roles };
    } catch (error) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
