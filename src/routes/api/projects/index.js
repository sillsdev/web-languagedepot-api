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
    if (typeof body !== 'object') {
        return jsonRequired('POST', path);
    }
    if (!body || !body.projectCode) {
        return missingRequiredParam('projectCode', `body of POST request to ${path}`);
    }
    const projectCode = body.projectCode;
    const trx = Project.startTransaction(dbs.public);
    const query = Project.query(trx).select('id').forUpdate().where('identifier', projectCode);
    const result = await atMostOne(query, 'projectCode', 'project code',
    async () => {
        const result = await Project.query(trx).insertAndFetch(body);
        return { status: 201, body: result, headers: { location: `${path}/${projectCode}`} };
    },
    async (project) => {
        const result = await Project.query(trx).updateAndFetchById(project.id, body);
        return { status: 200, body: result, headers: { location: `${path}/${projectCode}`} };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}
