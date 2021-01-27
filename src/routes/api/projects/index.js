import { Project } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { jsonRequired, cannotUpdateMissing, missingRequiredParam } from '$utils/commonErrors';
import { atMostOne, catchSqlError } from '$utils/commonSqlHandlers';

export async function get() {
    return catchSqlError(async () => {
        const projects = await Project.query(dbs.public);
        console.log('Projects result:', projects);
        return { status: 200, body: projects };
    });
}

// TODO: Handle HEAD, which should either return 200 if there are projects, 404 if there are none, or 403 Unauthorized if you're supposed to be logged in to access this
// export async function head({ path }) {
//     console.log(`HEAD ${path} called`);
//     return { status: 204, body: {} }
// }

export async function post({ path, body }) {
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
        const result = await Project.query(dbs.public).insertAndFetch(body);
        return { status: 201, body: result, headers: { location: path } };
    },
    async (project) => {
        const result = await Project.query(dbs.public).updateAndFetchById(project.id, body);
        return { status: 200, body: result, headers: { location: `${path}/${projectCode}`} };
    });
}
