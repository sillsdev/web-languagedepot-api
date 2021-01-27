import { Project } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { missingRequiredParam } from '$utils/commonErrors';
import { catchSqlError, onlyOne, atMostOne } from '$utils/commonSqlHandlers';

export async function get({ params, path }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    catchSqlError(async () => {
        const projects = await Project.query(dbs.public).where('identifier', params.projectCode);
        return onlyOne(projects, 'projectCode', 'project code', project => ({ status: 200, body: project }));
    });
}

export async function put({ path, params, body }) {
    console.log(`PUT /api/projects/${params.projectCode} received:`, body);
    // TODO: Transaction
    const projects = await Project.query(dbs.public).select('id').forUpdate().where('identifier', params.projectCode);
    return atMostOne(projects, 'projectCode', 'project code',
    async () => {
        const result = await Project.query(dbs.public).insertAndFetch(body);
        return { status: 201, body: result, headers: { location: path } };
    },
    async (project) => {
        const result = await Project.query(dbs.public).updateAndFetchById(project.id, body);
        return { status: 200, body: result };
    });
}

export async function patch({ path, params, body }) {
    console.log(`PATCH /api/projects/${params.projectCode} received:`, body);
    // TODO: Transaction
    if (typeof body !== 'object') {
        return jsonRequired('PATCH', path);
    }
    const projects = await Project.query(dbs.public).select('id').forUpdate().where('identifier', params.projectCode);
    return atMostOne(projects, 'projectCode', 'project code',
    async () => {
        const result = await Project.query(dbs.public).patch(body);
        return { status: 201, body: result, headers: { location: path } };
    },
    async (project) => {
        const result = await Project.query(dbs.public).updateAndFetchById(project.id, body);
        return { status: 200, body: result };
    });
}

export async function del({ params }) {
    console.log(`DELETE /api/projects/${params.projectCode} received:`, params);
    // TODO: Transaction
    const projects = await Project.query(dbs.public).select('id').forUpdate().where('identifier', params.projectCode);
    return atMostOne(projects, 'projectCode', 'project code',
    async () => {
        // Deleting a non-existent item is not an error
        return { status: 204, body: {} };
    },
    async (project) => {
        await Project.query(dbs.public).deleteById(project.id);
        return { status: 204, body: {} };
    });

}
