import type { RequestHandler } from '@sveltejs/kit';
import { dbs } from '$lib/db/dbsetup';
import { Project } from '$lib/db/models';
import { missingRequiredParam, jsonRequired, cannotModifyPrimaryKey, inconsistentParams } from '$lib/utils/commonErrors';
import { allowManagerOrAdmin } from '$lib/utils/db/authRules';
import { getOneProject, createOneProject, patchOneProject, deleteOneProject } from '$lib/utils/db/projects';
import { canonicalizeMembershipList, InvalidMemberships } from '$lib/utils/db/usersAndRoles';

// GET /api/v2/projects/{projectCode} - JSON representation of a single project (requires manager or admin rights)
// Security: must be a project manager on the project in question, or a site admin
export const GET: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowManagerOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        return getOneProject(db, params.projectCode);
    } else {
        return authResult;
    }
}

// HEAD /api/v2/projects/{projectCode} - check whether project exists. Returns 200 if exists, 404 if not found. Response has no body; only HTTP status code is meaningful.
// Security: anonymous access allowed
export const HEAD: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.projectCode) {
        return { status: 400, body: {} };
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    // No auth check here: anyone is allowed to query whether a project exists
    try {
        const projectCount = await Project.query(db).where('identifier', params.projectCode).resultSize();
        const status = projectCount < 1 ? 404 : projectCount > 1 ? 500 : 200;
        return { status, body: {} };
    } catch (error) {
        return { status: 500, body: {} };
    }
}

// PUT /api/v2/projects/{projectCode} - create project, or update project if it aleady exists.
// Security: anyone may create a project, and they become the project's first manager. Updating is restricted to existing project managers or site admins.
export const PUT: RequestHandler = async ({ params, url, request }) => {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', url.pathname);
    }
    let body: any;
    try {
        body = await request.json();
    } catch (e: any) {
        return jsonRequired(request.method, url.pathname);
    }
    if (body && body.projectCode) {
        if (params.projectCode !== body.projectCode) {
            return inconsistentParams('projectCode');
        }
    } else {
        return missingRequiredParam('projectCode', `body of PUT request to ${url.pathname}`);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    return await createOneProject(db, params.projectCode, body, request.headers);
    // Here we don't return Content-Location because the client already knows it
}

// PATCH /api/v2/projects/{projectCode} - update project membership, possibly in bulk
// TODO: Document JSON "shapes" allowed for project membership (many possibilities)
// Security: must be a project manager on the project in question, or a site admin
export const PATCH: RequestHandler = async ({ params, url, request }) => {
    let body: any;
    try {
        body = await request.json();
    } catch (e: any) {
        return jsonRequired(request.method, url.pathname);
    }
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', url.pathname);
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
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowManagerOrAdmin(db, { params, headers: request.headers });
    if (authResult.status === 200 && (authResult as any).authUser) {
        return await patchOneProject(db, params.projectCode, body);
    } else {
        return authResult;
    }
}

// DELETE /api/v2/projects/{projectCode} - delete project
// TODO: Make this archive the project instead, and only allow actual deletion if query contains `?reallyDelete=true`
// Security: must be a project manager on the project in question, or a site admin
export const DELETE: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowManagerOrAdmin(db, { params, headers });
    if (authResult.status === 200 && (authResult as any).authUser) {
        return await deleteOneProject(db, params.projectCode);
    } else {
        return authResult;
    }
}
