import { Project, defaultRoleId } from '$lib/db/models';
import { dbs } from '$lib/db/dbsetup';
import { missingRequiredParam } from '$lib/utils/commonErrors';
import { onlyOne } from '$lib/utils/commonSqlHandlers';
import { addUserWithRoleByProjectCode, removeUserFromProjectByProjectCode } from '$lib/utils/db/usersAndRoles';
import { allowSameUserOrProjectManagerOrAdmin } from '$lib/utils/db/authRules';

// GET /api/v2/projects/{projectCode}/user/{username} - return user's role
// Security: must be user whose role is being looked up, a project manager on the project in question, or a site admin
export async function get({ params, query, headers }) {
    if (!params || !params.projectCode) {
        return missingRequiredParam('projectCode', 'URL');
    }
    if (!params || !params.username) {
        return missingRequiredParam('username', 'URL');
    }
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowSameUserOrProjectManagerOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        const dbQuery = Project.query(db)
            .where('identifier', params.projectCode)
            .withGraphJoined('members.[user, role]')
            ;
        const x = allowSameUserOrProjectManagerOrAdmin
        return onlyOne(dbQuery, 'projectCode', 'project code',
        async (project) => {
            const users = project.members.filter(member => member.user.login === params.username && member.role && member.role.name);
            return onlyOne(users, 'username', 'username', (member) => ({ status: 200, body: { user: member.user, role: member.role.name }}));
        });
    } else {
        return authResult;
    }
}

// TODO: HEAD /api/v2/projects/{projectCode}/user/{username} - is user a member of the project?
// Possible return codes:
// 200 if user is member of project (with any role)
// 404 if he/she is not, or if user or project not found
// export async function head({ path }) {
//     return { status: 204, body: {} }
// }

// DELETE /api/v2/projects/{projectCode}/user/{username} - remove user from project
// Security: must be user being removed, a project manager on the project in question, or a site admin
export async function del({ params, query, headers }) {
    if (!params || !params.projectCode) {
        return missingRequiredParam('projectCode', 'URL');
    }
    if (!params || !params.username) {
        return missingRequiredParam('username', 'URL');
    }
    const db = query.private ? dbs.private : dbs.public;
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
export async function post({ params, path, body, query, headers }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowSameUserOrProjectManagerOrAdmin(db, { params, headers });
    if (authResult.status === 200) {
        let roleName;
        if (body && typeof body === "string") {
            roleName = body;
        } else if (body && typeof body === "number") {
            roleName = body;
        } else if (body && typeof body === "object") {
            roleName = body.role || body.roleId || body.roleName || defaultRoleId;
        } else {
            roleName = defaultRoleId;
        }
        return addUserWithRoleByProjectCode(db, params.projectCode, params.username, roleName);
    } else {
        return authResult;
    }
}
