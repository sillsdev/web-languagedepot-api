import { dbs } from '$components/models/dbsetup';
import { Project, projectStatus } from '$components/models/models';
import { missingRequiredParam, cannotUpdateMissing } from '$utils/commonErrors';
import { onlyOne, atMostOne } from '$utils/commonSqlHandlers';

export async function get({ params, path, query }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const dbQuery = Project.query(db).where('identifier', params.projectCode);
    return onlyOne(dbQuery, 'projectCode', 'project code', project => ({ status: 200, body: project }));
}

// TODO: Return Content-Location where appropriate

export async function head({ params, query }) {
    if (!params.projectCode) {
        return { status: 400, body: {} };
    }
    const db = query.private ? dbs.private : dbs.public;
    try {
        const projectCount = await Project.query(db).where('identifier', params.projectCode).resultSize();
        const status = projectCount < 1 ? 404 : projectCount > 1 ? 500 : 200;
        return { status, body: {} };
    } catch (error) {
        return { status: 500, body: {} };
    }
}

export async function put({ path, params, body, query }) {
    const db = query.private ? dbs.private : dbs.public;
    const trx = await Project.startTransaction(db);
    const dbQuery = Project.query(trx).select('id').forUpdate().where('identifier', params.projectCode);
    const result = await atMostOne(dbQuery, 'projectCode', 'project code',
    async () => {
        const result = await Project.query(trx).insertAndFetch(body);
        return { status: 201, body: result, headers: { location: path } };
    },
    async (project) => {
        const result = await Project.query(trx).updateAndFetchById(project.id, body);
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}

export async function patch({ path, params, body, query }) {
    // TODO: Passwords need special handling
    if (typeof body !== 'object') {
        return jsonRequired('PATCH', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const trx = await Project.startTransaction(db);
    const dbQuery = Project.query(trx).select('id').forUpdate().where('identifier', params.projectCode);
    const result = await atMostOne(dbQuery, 'projectCode', 'project code',
    () => {
        return cannotUpdateMissing(params.projectCode, 'project');
    },
    async (project) => {
        const result = await Project.query(trx).patchAndFetchById(project.id, body);
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}

export async function del({ params, query }) {
    const db = query.private ? dbs.private : dbs.public;
    const trx = await Project.startTransaction(db);
    const dbQuery = Project.query(trx).select('id').forUpdate().where('identifier', params.projectCode);
    const result = await atMostOne(dbQuery, 'projectCode', 'project code',
    async () => {
        // Deleting a non-existent item is not an error
        return { status: 204, body: {} };
    },
    async (project) => {
        await Project.query(trx).fetchById(project.id).patch({status: projectStatus.archived});
        return { status: 204, body: {} };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}
