import { Project, defaultRoleName } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { missingRequiredParam } from '$utils/commonErrors';
import { onlyOne } from '$utils/commonSqlHandlers';
import { addUserWithRole } from '$utils/db/usersAndRoles';
import { removeUserFromProject } from '../../../../../../utils/db/usersAndRoles';

// GET /api/projects/{projectCode}/user/{username} - return user's role
export async function get({ params }) {
    console.log('GET', params);
    if (!params || !params.projectCode) {
        return missingRequiredParam('projectCode', 'URL');
    }
    if (!params || !params.username) {
        return missingRequiredParam('username', 'URL');
    }
    const query = Project.query(dbs.public)
        .where('identifier', params.projectCode)
        .withGraphJoined('members.[user, role]')
        ;
    return onlyOne(query, 'projectCode', 'project code',
    async (project) => {
        users = project.members.filter(member => member.user.login === params.username && member.role && member.role.name);
        // return onlyOne(users, 'username', 'username', (member) => ({ status: 200, body: member.role.name }));
        return { status: 200, body: project };
    });
}

// TODO: Handle HEAD, which should either return 200 if there are users, 404 if there are none, or 403 Unauthorized if you're supposed to be logged in to access this
// export async function head({ path }) {
//     console.log(`HEAD ${path} called`);
//     return { status: 204, body: {} }
// }

// DELETE /api/projects/{projectCode}/user/{username} - remove user from project
export async function del({ params }) {
    if (!params || !params.projectCode) {
        return missingRequiredParam('projectCode', 'URL');
    }
    if (!params || !params.username) {
        return missingRequiredParam('username', 'URL');
    }
    return removeUserFromProject(params.projectCode, params.username, dbs.public);
}

export async function post({ params, path, body }) {
    if (!params.projectCode) {
        return missingRequiredParam('projectCode', path);
    }
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    let roleName;
    if (body && typeof body === "string") {
        roleName = body;
    } else if (body && typeof body === "object") {
        roleName = body.role ? body.role : body.roleName ? body.roleName : defaultRoleName;
    } else {
        roleName = defaultRoleName;
    }
    return addUserWithRole(params.projectCode, params.username, roleName, dbs.public);
}
