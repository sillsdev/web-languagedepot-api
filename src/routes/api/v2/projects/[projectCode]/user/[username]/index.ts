import type { RequestHandler } from '@sveltejs/kit';
import { Project, defaultRoleId } from '$lib/db/models';
import { dbs } from '$lib/db/dbsetup';
import { missingRequiredParam } from '$lib/utils/commonErrors';
import { onlyOne } from '$lib/utils/commonSqlHandlers';
import { addUserWithRoleByProjectCode, removeUserFromProjectByProjectCode } from '$lib/utils/db/usersAndRoles';
import { allowSameUserOrProjectManagerOrAdmin } from '$lib/utils/db/authRules';

// GET /api/v2/projects/{projectCode}/user/{username} - return user's role
// Security: must be user whose role is being looked up, a project manager on the project in question, or a site admin
export const GET: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', 'URL');
    }
    if (!params.username) {
        return missingRequiredParam('username', 'URL');
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowSameUserOrProjectManagerOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        const dbQuery = Project.query(db)
            .where('identifier', params.projectCode)
            .withGraphJoined('members.[user, role]')
            ;
        return onlyOne(dbQuery, 'projectCode', 'project code',
        async (project: any) => {
            const users = project.members.filter((member: any) => member.user.login === params.username && member.role && member.role.name);
            return onlyOne(users, 'username', 'username', (member: any) => ({ status: 200, body: { user: member.user, role: member.role.name }}));
        });
    } else {
        return authResult;
    }
}

// HEAD /api/v2/projects/{projectCode}/user/{username} - is user a member of the project?
// Security: must be user whose role is being looked up, a project manager on the project in question, or a site admin
// Possible return codes:
// 200 if user is member of project (with any role)
// 404 if he/she is not, or if user or project not found
export const HEAD: RequestHandler = async (event) => {
    const result = await GET(event)
    return { ...result, body: {} }
}

// DELETE /api/v2/projects/{projectCode}/user/{username} - remove user from project
// Security: must be user being removed, a project manager on the project in question, or a site admin
export const DELETE: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', url.pathname);
    }
    if (!params.username) {
        return missingRequiredParam('username', url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowSameUserOrProjectManagerOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        return removeUserFromProjectByProjectCode(db, params.projectCode, params.username);
    } else {
        return authResult;
    }
}

// POST /api/v2/projects/{projectCode}/user/{username} - add user to project or update role
// Role should be given in POST body; either a string or integer, or a JSON object with a `role`, `roleId`, or `roleName` property
// If no role specified, defaults to Contributor
// Security: must be user being removed, a project manager on the project in question, or a site admin
export const POST: RequestHandler = async ({ params, url, request }) => {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', url.pathname);
    }
    if (!params.username) {
        return missingRequiredParam('username', url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowSameUserOrProjectManagerOrAdmin(db, { params, headers: request.headers });
    if (authResult.status === 200) {
        let roleName: string | number;
        const text = await request.text();
        let body;
        try {
            body = JSON.parse(text);
        } catch (_) {
            body = text;
        }
        if (body && typeof body === "string") {
            roleName = body;
        } else if (body && typeof body === "number") {
            roleName = body;
        } else if (body) {
            roleName = body.role || body.roleId || body.roleName || defaultRoleId;
        } else {
            roleName = defaultRoleId;
        }
        return addUserWithRoleByProjectCode(db, params.projectCode, params.username, roleName);
    } else {
        return authResult;
    }
}
