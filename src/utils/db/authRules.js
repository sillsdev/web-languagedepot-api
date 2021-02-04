import { Project, managerRoleId, techSupportRoleId } from '$db/models';
import { authTokenRequired, notAllowed } from '$utils/commonErrors';

export async function getMemberRoleInProject(db, { projectCode, username } = {}) {
    if (!projectCode || !username) {
        return undefined;
    }
    try {
        const projects = await Project.query(db)
            .where('identifier', projectCode)
            .withGraphJoined('members.[user, role]')
            ;
        if (projects.length < 1) {
            return undefined;
        }
        const members = projects[0].members.filter(member => member.user.login === username && member.role && member.role.name);
        if (members.length < 1) {
            return undefined;
        }
        return members[0].role;
    } catch (error) {
        console.log(`SQL error looking up member role for ${username} in ${projectCode}`);
        return undefined;
    }
}

export async function isMemberOf(db, { projectCode, authUser }) {
    const role = await getMemberRoleInProject(db, { projectCode, username: authUser ? authUser.login : undefined });
    return !!role;
}

export async function isManagerOf(db, { projectCode, authUser }) {
    const role = await getMemberRoleInProject(db, { projectCode, username: authUser ? authUser.login : undefined });
    return !!(role && role.id === managerRoleId);
}

export async function hasManagerRights(db, { projectCode, authUser }) {
    const role = await getMemberRoleInProject(db, { projectCode, username: authUser ? authUser.login : undefined });
    return !!(role && (role.id === managerRoleId || role.id === techSupportRoleId));
}

export function isAdmin(authUser) {
    return !!(authUser && authUser.admin)
}

// Helpers for quick-and-easy auth
export async function allowManagerOrAdmin(db, { params, authUser } = {}) {
    const projectCode = params ? params.projectCode : undefined;
    if (!authUser) {
        return authTokenRequired();
    }
    const allowed = isAdmin(authUser) || await hasManagerRights(db, { projectCode, authUser });
    if (!allowed) {
        return notAllowed();
    }
    return { status: 200 };
}

export function allowSameUserOrAdmin({ params, authUser } = {}) {
    const username = params ? params.username : undefined;
    if (!authUser) {
        return authTokenRequired();
    }
    const allowed = !!(authUser && authUser.login === username) || isAdmin(authUser);
    if (!allowed) {
        return notAllowed();
    }
    return { status: 200 };
}

// Use like this:
// function get({params, headers, query}) {
//     const db = query.private ? dbs.private : dbs.public;
//     const authUser = verifyJwtAuth(db, headers);
//     const authResponse = allowSameUserOrAdmin({ params, authUser });
//     if (authResponse.status >= 300) {
//         return authResponse;
//     }
//     // Continue with logic now that we know the operation is allowed
// }
