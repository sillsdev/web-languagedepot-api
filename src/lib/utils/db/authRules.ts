import { Project, managerRoleId, techSupportRoleId, User } from '$lib/db/models';
import { authTokenRequired, missingRequiredParam, notAllowed } from '$lib/utils/commonErrors';
import { retryOnServerError } from '$lib/utils/commonSqlHandlers';
import type { TransactionOrKnex } from 'objection';
import { verifyBasicAuth, verifyJwtAuth } from './auth';

export async function getMemberRoleInProject(db: TransactionOrKnex, params?: { projectCode: string, username: string }) {
    const { projectCode, username } = params ?? {};
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
        const members = (projects[0] as any).members.filter((member: any) => member.user.login === username && member.role && member.role.name);
        if (members.length < 1) {
            return undefined;
        }
        return members[0].role;
    } catch (error) {
        console.log(`SQL error looking up member role for ${username} in ${projectCode}`);
        return undefined;
    }
}

export async function isMemberOf(db: TransactionOrKnex, params: { projectCode: string, authUser: Record<string, any> }) {
    const { projectCode, authUser } = params;
    const role = await getMemberRoleInProject(db, { projectCode, username: authUser ? authUser.login : '' });
    return !!role;
}

export async function isManagerOf(db: TransactionOrKnex, params: { projectCode: string, authUser: Record<string, any> }) {
    const { projectCode, authUser } = params;
    const role = await getMemberRoleInProject(db, { projectCode, username: authUser ? authUser.login : '' });
    return !!(role && role.id === managerRoleId);
}

export async function hasManagerRights(db: TransactionOrKnex, params: { projectCode: string, authUser: Record<string, any> }) {
    const { projectCode, authUser } = params;
    const role = await getMemberRoleInProject(db, { projectCode, username: authUser ? authUser.login : '' });
    return !!(role && (role.id === managerRoleId || role.id === techSupportRoleId));
}

export function isAdmin(authUser: Record<string, any> | boolean | undefined) {
    return !!(authUser && (authUser as Record<string, any>).admin)
}

// Helpers for quick-and-easy auth
export async function allowAdminOnly(db: TransactionOrKnex, request: { headers: Headers }) {
    let authUser = await verifyJwtAuth(db, request.headers);
    if (!authUser) {
        if (authUser === undefined) {
            return authTokenRequired();
        } else {
            return notAllowed();
        }
    }
    const allowed = isAdmin(authUser);
    if (!allowed) {
        return notAllowed();
    }
    return { status: 200, authUser };
}

export async function allowManagerOrAdmin(db: TransactionOrKnex, request: { params: Record<string, any>, headers: Headers }) {
    const { params, headers } = request;
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

export async function allowSameUserOrProjectManagerOrAdmin(db: TransactionOrKnex, request: { params: Record<string, any>, headers: Headers }) {
    const { params, headers } = request;
    if (!params || !params.username) {
        return missingRequiredParam('username');
    }
    let authUser = await verifyJwtAuth(db, headers);
    if (!authUser) {
        if (authUser === undefined) {
            return authTokenRequired();
        } else {
            return notAllowed();
        }
    }
    const allowed = (authUser as any).login === params.username || isAdmin(authUser);
    if (allowed) {
        return { status: 200, authUser };
    } else {
        if (!params || !params.projectCode) {
            return missingRequiredParam('projectCode');
        }
        const isManager = await hasManagerRights(db, { projectCode: params.projectCode, authUser });
        if (isManager) {
            return { status: 200, authUser };
        } else {
            return notAllowed();
        }
    }
}

export async function allowSameUserOrAdmin(db: TransactionOrKnex, request: { params: Record<string, any>, headers: Headers, allowBasicAuth?: boolean }) {
    const { params, headers, allowBasicAuth } = request;
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
    const allowed = !!(authUser && (authUser as any).login === params.username) || isAdmin(authUser);
    if (!allowed) {
        return notAllowed();
    }
    return { status: 200, authUser };
}
