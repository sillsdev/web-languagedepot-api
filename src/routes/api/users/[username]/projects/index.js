import { User } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';

export async function get({ params, query }) {
    if (!params.username) {
        return { status: 500, body: { description: 'No username specified', code: 'missing_username' }};
    }
    const db = query.private ? dbs.private : dbs.public;
    try {
        const users = await User.query(db)
            .withGraphJoined('memberships.[project, role]')
            .where('login', params.username);
        const result = users.map(user => ({
            username: user.login,
            memberships: user.memberships.map(m => ({
                projectCode: m.project.identifier,
                role: m.role.name,
            }))
        }));
        return { status: 200, body: result };
    } catch (error) {
        console.log('Error of type', typeof error, 'has name:', error.name, 'and msg:', error.message);
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
