import { Project } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { jsonRequired, cannotUpdateMissing, missingRequiredParam } from '$utils/commonErrors';
import { atMostOne } from '$utils/commonSqlHandlers';

export async function get() {
    try {
        const projects = await Project.query(dbs.public);
        console.log('Projects result:', projects);
        return { status: 200, body: projects };
    } catch (error) {
        return { status: 500, body: { error, code: 'sql_error' } };
    }
}

export async function post({ path, params, body }) {  // { params, path, body }
    console.log(`POST /api/projects received:`, body);
    // TODO: Transaction
    if (typeof body !== 'object') {
        return jsonRequired('POST', path);
    }
    if (!body || !body.projectCode) {
        return missingRequiredParam('projectCode', 'body of POST request');
    }
    const projectCode = body.projectCode;
    const projects = await Project.query(dbs.public).select('id').forUpdate().where('identifier', projectCode);
    return atMostOne(projects, 'projectCode', 'project code',
    async () => {
        return cannotUpdateMissing(projectCode, 'project');
    },
    async (project) => {
        const result = await Project.query(dbs.public).updateAndFetchById(project.id, body);
        return { status: 201, body: result, headers: { location: `${path}/${projectCode}`} };
    });
}
