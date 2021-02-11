import { dbs } from '$db/dbsetup';
import { Project } from '$db/models';
import { missingRequiredParam, cannotModifyPrimaryKey, inconsistentParams, authTokenRequired } from '$utils/commonErrors';
import { retryOnServerError } from '$utils/commonSqlHandlers';
import { verifyJwtAuth } from '$utils/db/auth';
import { getOneProject, createOneProject, patchOneProject, deleteOneProject } from '$utils/db/projects';
import { canonicalizeMembershipList, InvalidMemberships } from '$utils/db/usersAndRoles';

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
        const projectCount = await retryOnServerError(Project.query(db).where('identifier', params.projectCode).resultSize());
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
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    if (body && body.projectCode) {
        if (params.projectCode !== body.projectCode) {
            return inconsistentParams('projectCode');
        }
    } else {
        return missingRequiredParam('projectCode', `body of PUT request to ${path}`);
    }
    const db = query.private ? dbs.private : dbs.public;
    const authUser = await verifyJwtAuth(db, headers);
    if (!authUser) {
        return authTokenRequired();
    }
    return await createOneProject(db, params.projectCode, body, authUser);
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
    if (body && body.projectCode) {
        if (params.projectCode !== body.projectCode) {
            return cannotModifyPrimaryKey('projectCode', 'project');
        }
    }
    if (body && body.members) {
        try {
            if (body.members.add) {
                body.members = { add: canonicalizeMembershipList(body.members.add) };
            } else if (body.members.remove) {
                body.members = { remove: canonicalizeMembershipList(body.members.remove) };
            } else if (body.members.removeUser && typeof body.members.removeUser === 'string') {
                // Deprecated, backwards compatibility with previous implementation
                body.members = { remove: [{user: body.members.removeUser}] };
            } else if (Array.isArray(body.members)) {
                body.members = { set: canonicalizeMembershipList(body.members) };
            } else {
                throw new InvalidMemberships(body.members);
            }
        } catch (error) {
            if (error instanceof InvalidMemberships) {
                console.log('invalid members', error.message);
                return { status: 400, body: {code: 'invalid_memberships_record', record_with_error: error.message, description: 'Could not parse "members" property in PATCH body; see record_with_error property for the invalid item'}};
            } else {
                throw error;
            }
        }
    }
    const db = query.private ? dbs.private : dbs.public;
    return await patchOneProject(db, params.projectCode, body);
}

export async function del({ path, params, query }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    return await deleteOneProject(db, params.projectCode);
}
