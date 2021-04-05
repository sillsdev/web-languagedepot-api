import { dbs } from '$lib/db/dbsetup';
import { Project } from '$lib/db/models';
import { missingRequiredParam, jsonRequired, cannotModifyPrimaryKey, inconsistentParams, authTokenRequired, notAllowed } from '$lib/utils/commonErrors';
import { retryOnServerError } from '$lib/utils/commonSqlHandlers';
import { verifyJwtAuth } from '$lib/utils/db/auth';
import { allowManagerOrAdmin } from '$lib/utils/db/authRules';
import { getOneProject, createOneProject, patchOneProject, deleteOneProject } from '$lib/utils/db/projects';
import { canonicalizeMembershipList, InvalidMemberships } from '$lib/utils/db/usersAndRoles';

export async function get({ params, path, query, headers }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowManagerOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        return getOneProject(db, params.projectCode);
    } else {
        return authResult;
    }
}

export async function head({ params, query }) {
    if (!params.projectCode) {
        return { status: 400, body: {} };
    }
    const db = query.private ? dbs.private : dbs.public;
    // No auth check here: anyone is allowed to query whether a project exists
    try {
        const projectCount = await retryOnServerError(Project.query(db).where('identifier', params.projectCode).resultSize());
        const status = projectCount < 1 ? 404 : projectCount > 1 ? 500 : 200;
        return { status, body: {} };
    } catch (error) {
        return { status: 500, body: {} };
    }
}

export async function put({ path, params, body, query, headers }) {
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
    return await createOneProject(db, params.projectCode, body, headers);
    // Here we don't return Content-Location because the client already knows it
}

export async function patch({ path, params, body, query, headers }) {
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
                throw new InvalidMemberships('invalid_memberships_record', body.members);
            }
        } catch (error) {
            if (error instanceof InvalidMemberships) {
                return { status: 400, body: {code: error.code, details: error.details, description: 'Could not parse "members" property in PATCH body; see details property for the invalid item'}};
            } else {
                throw error;
            }
        }
    }
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowManagerOrAdmin(db, { params, headers });
    if (authResult.status === 200 && authResult.authUser) {
        return await patchOneProject(db, params.projectCode, body);
    } else {
        return authResult;
    }
}

export async function del({ path, params, query }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    return await deleteOneProject(db, params.projectCode);
}
