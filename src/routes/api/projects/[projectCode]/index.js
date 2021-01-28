import { dbs } from '$components/models/dbsetup';
import { Project, projectStatus } from '$components/models/models';
import { missingRequiredParam, cannotUpdateMissing } from '$utils/commonErrors';
import { onlyOne, atMostOne } from '$utils/commonSqlHandlers';

export async function get({ params, path }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    const query = Project.query(dbs.public).where('identifier', params.projectCode);
    return onlyOne(query, 'projectCode', 'project code', project => ({ status: 200, body: project }));
}

// TODO: Return Content-Location where appropriate

export async function head({ params }) {
    if (!params.projectCode) {
        return { status: 400, body: {} };
    }
    try {
        const projectCount = await Project.query(dbs.public).count().where('identifier', params.projectCode);
        const status = projectCount < 1 ? 404 : projectCount > 1 ? 500 : 200;
        return { status, body: {} };
    } catch (error) {
        return { status: 500, body: {} };
    }
}

export async function put({ path, params, body }) {
    console.log(`PUT /api/projects/${params.projectCode} received:`, body);
    const trx = Project.startTransaction(dbs.public);
    const query = Project.query(dbs.public).select('id').forUpdate().where('identifier', params.projectCode);
    const result = await atMostOne(query, 'projectCode', 'project code',
    async () => {
        const result = await Project.query(dbs.public).insertAndFetch(body);
        return { status: 201, body: result, headers: { location: path } };
    },
    async (project) => {
        const result = await Project.query(dbs.public).updateAndFetchById(project.id, body);
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}

export async function patch({ path, params, body }) {
    // TODO: Passwords need special handling
    if (typeof body !== 'object') {
        return jsonRequired('PATCH', path);
    }
    const trx = Project.startTransaction(dbs.public);
    const query = Project.query(trx).select('id').forUpdate().where('identifier', params.projectCode);
    const result = await atMostOne(query, 'projectCode', 'project code',
    () => {
        return cannotUpdateMissing(params.projectCode, 'project');
    },
    async (project) => {
        const result = await Project.query(trx).patchAndFetchById(project.id, body);
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}

export async function del({ params }) {
    const trx = Project.startTransaction(dbs.public);
    const query = Project.query(trx).select('id').forUpdate().where('identifier', params.projectCode);
    const result = await atMostOne(query, 'projectCode', 'project code',
    async () => {
        // Deleting a non-existent item is not an error
        return { status: 204, body: {} };
    },
    async (project) => {
        await Project.query(trx).fetchById(project.id).patch({status: projectStatus.archived});
        return { status: 204, body: {} };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}
