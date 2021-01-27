import { Project } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';

export async function get() {
    try {
        const projects = await Project.query(dbs.private);
        console.log('Projects result:', projects);
        return { status: 200, body: projects };
    } catch (error) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}
