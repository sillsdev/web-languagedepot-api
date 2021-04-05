import { Project, managerRoleId, techSupportRoleId } from '$lib/db/models';
import { authTokenRequired, missingRequiredParam, notAllowed } from '$lib/utils/commonErrors';
import { retryOnServerError } from '$lib/utils/commonSqlHandlers';
import { verifyBasicAuth, verifyJwtAuth } from './auth';

export async function getMemberRoleInProject(db, { projectCode, username } = {}) {
    if (!projectCode || !username) {
        return undefined;
    }
    try {
        const projects = await retryOnServerError(Project.query(db)
            .where('identifier', projectCode)
            .withGraphJoined('members.[user, role]')
        );
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
export async function allowManagerOrAdmin(db, { params, headers } = {}) {
    if (!params || !params.projectCode) {
        return missingRequiredParam('projectCode');
    }
    let authUser = await verifyJwtAuth(db, headers);
    if (!authUser) {
        if (authUser === undefined) {
            return authTokenRequired();
        } else {
            return notAllowed();
        }
    }
    const projectCode = params ? params.projectCode : undefined;
    const allowed = isAdmin(authUser) || await hasManagerRights(db, { projectCode, authUser });
    if (!allowed) {
        return notAllowed();
    }
    return { status: 200, authUser };
}

export async function allowSameUserOrAdmin(db, { params, headers, allowBasicAuth } = {}) {
    if (!params || !params.username) {
        return missingRequiredParam('username');
    }
    let authUser = await verifyJwtAuth(db, headers);
    if (!authUser) {
        if (authUser === undefined) {
            if (allowBasicAuth) {
                // To interop with older clients, this route also allows user:pass in URL (HTTP basic auth)
                authUser = await verifyBasicAuth(db, headers);
                if (!authUser) {
                    if (authUser === undefined) {
                        // No username or password was presented: return 401
                        return authTokenRequired();
                    } else {
                        // Username and password were presented but they were wrong: return 403
                        return notAllowed();
                    }
                }
            } else {
                return authTokenRequired();
            }
        } else {
            return notAllowed();
        }
    }
    const allowed = !!(authUser && authUser.login === params.username) || isAdmin(authUser);
    if (!allowed) {
        return notAllowed();
    }
    return { status: 200, authUser };
}
