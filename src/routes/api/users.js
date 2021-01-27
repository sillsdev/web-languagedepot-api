import User from '$components/models/User';

export async function get() {
    try {
        const users = await User.query().select('id', 'login');
        return { status: 200, body: users };
    } catch (error) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
