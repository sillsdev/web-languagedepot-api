import { dbs } from '$components/models/dbsetup';
import { Project } from '$components/models/models';
import { missingRequiredParam } from '$utils/commonErrors';
import { getOneProject, createOneProject, patchOneProject, deleteOneProject } from '$utils/db/projects';

export async function get({ params, path, query }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    return getOneProject(db, params.projectCode);
}

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
    if (typeof body !== 'object') {
        return jsonRequired('PUT', path);
    }
    if (!body || !body.projectCode) {
        return missingRequiredParam('projectCode', `body of PUT request to ${path}`);
    }
    const projectCode = body.projectCode;
    const db = query.private ? dbs.private : dbs.public;
    return await createOneProject(db, projectCode, body);
    // Here we don't return Content-Location because the client already knows it
}

export async function patch({ path, params, body, query }) {
    // TODO: Membership records need special handling
    if (typeof body !== 'object') {
        return jsonRequired('PATCH', path);
    }
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    return await patchOneProject(db, projectCode, body);
}

export async function del({ path, params, query }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    return await deleteOneProject(db, params.projectCode);
}
