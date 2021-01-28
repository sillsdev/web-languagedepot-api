import { User } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';

export async function get({ query }) {
    const db = query.private ? dbs.private : dbs.public;
    try {
        const users = await User.query(db)
            .withGraphJoined('memberships.[project, role]')
        const result = users.map(user => ({
            username: user.login,
            memberships: user.memberships.map(m => ({
                projectCode: m.project.identifier,
                role: m.role.name,
            }))
        }));
        return { status: 200, body: users };
    } catch (error) {
        console.log(error);
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
