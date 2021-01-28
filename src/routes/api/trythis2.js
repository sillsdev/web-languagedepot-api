import { dbs } from '$components/models/dbsetup';
import { Project } from '../../components/models/models';

export async function get({ query }) {
    const db = query.private ? dbs.private : dbs.public;
    try {
        const projects = await Project.query(db)
            .withGraphJoined('members.[user, role]')
        const result = projects.map(project => ({
            projectCode: project.identifier,
            members: project.members.map(m => ({
                username: m.user.login,
                role: m.role.name,
            }))
        }));
        return { status: 200, body: projects };
    } catch (error) {
        console.log(error);
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
