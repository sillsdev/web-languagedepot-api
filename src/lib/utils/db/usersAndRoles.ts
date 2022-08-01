import { Project, Role, User, Membership, MemberRole, defaultRoleId } from '$lib/db/models';
import { onlyOne, atMostOne, catchSqlError, retryOnServerError } from '$lib/utils/commonSqlHandlers';
import type { TransactionOrKnex } from 'objection';

type StringOrNumber = string | number;

async function addUserWithRole(trx: TransactionOrKnex, project: Record<string, any>, username: string, roleNameOrId: StringOrNumber) {
    let query = Role.query(trx).select('id'),
    itemKey,
    itemName;
    if (typeof roleNameOrId === 'number' || /^\d+$/.test(roleNameOrId)) {
        query = query.where('id', roleNameOrId);
        itemKey = 'roleId';
        itemName = 'role ID';
    } else {
        query = query.where('name', roleNameOrId);
        itemKey = 'rolename';
        itemName = 'role name';
    }
    return onlyOne(query, itemKey, itemName,
    async (role) => {
        const query = User.query(trx).select('id').where('login', username);
        return onlyOne(query, 'username', 'username',
        async (user: any) => {
            let membership: any = await retryOnServerError(Membership.query(trx).where({user_id: user.id, project_id: project.id}));
            if (!membership || membership.length === 0) {
                membership = await retryOnServerError(Membership.query(trx).insert({user_id: user.id, project_id: project.id}));
            } else {
                membership = membership[0];
            }
            // Update role if already a member, else add new MemberRole
            const role_id = (role as any).id;
            let memberRole = await retryOnServerError(MemberRole.query(trx).where({member_id: membership.id}));
            if (memberRole.length === 0) {
                await retryOnServerError(MemberRole.query(trx).insert({member_id: membership.id, role_id}));
            } else {
                await retryOnServerError(MemberRole.query(trx).findById((memberRole[0] as any).id).patch({role_id}));
            }
            return { status: 204, body: {} };
        });
    });
}

async function addUserWithRoleByProjectCode(db: TransactionOrKnex, projectCode: string, username: string, roleNameOrId: StringOrNumber) {
    const trx = await Project.startTransaction(db);
    const query = Project.query(trx).select('id').forShare().where('identifier', projectCode);
    const result = await onlyOne(query, 'projectCode', 'project code',
    async (project) => {
        return addUserWithRole(trx, project, username, roleNameOrId);
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}

async function removeUserFromProject(trx: TransactionOrKnex, project: Record<string, any>, username: string) {
    let users;
    if (project.members) {
        users = project.members.filter((member: any) => member.user.login === username);
    } else {
        users = Membership.query(trx).where('project_id', project.id).joinRelated('user').where('user.login', username);
    }
    return atMostOne(users, 'membership', `project membership for ${username} in ${project.identifier}`,
    () => {
        // Not a member? Then there's nothing to delete
        return { status: 204, body: {} };
    },
    async (member: any) => {
        await retryOnServerError(MemberRole.query(trx).where('member_id', member.id).delete());
        // TODO: Find out how to use .unrelate() and .relatedQuery() to achieve this effect
        await retryOnServerError(Membership.query(trx).deleteById(member.id));
        return { status: 204, body: {} };
    });
}

async function removeUserFromProjectByProjectCode(db: TransactionOrKnex, projectCode: string, username: string) {
    const trx = await Project.startTransaction(db);
    const query = Project.query(trx).where('identifier', projectCode).withGraphJoined('members.[user, role]');
    const result = await onlyOne(query, 'projectCode', 'project code',
    async (project) => {
        return removeUserFromProject(trx, project, username);
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}

function getProjectsForUser(db: TransactionOrKnex, { username = '', rolename = undefined } = {}) {
    return catchSqlError(async () => {
        let query = User.query(db)
            .withGraphJoined('memberships.[project, role]')
            .where('login', username);
        // TODO: Need to deal with the "inherit members" feature of Redmine, as some projects inherit membership from parent project
        if (rolename) {
            if (typeof rolename === 'number' || /^\d+$/.test(rolename)) {
                query = query.where('memberships:role.id', rolename);
            } else {
                query = query.where('memberships:role.name', rolename);
            }
        }
        const usersWithMemberships = await retryOnServerError(query);
        if (!usersWithMemberships || usersWithMemberships.length < 1) {
            return { status: 200, body: [] };
        } else {
            const user = usersWithMemberships[0];
            const result = (user as any).memberships.map((m: any) => ({
                projectCode: m.project.identifier,
                name: m.project.name,
                role: m.role.name,
            }));
            return { status: 200, body: result };
        }
    });
}

// A membership record will be *accepted* by the API in one of the following forms:
// {
//     user: string | (user object that must include username; all other properties are ignored)
//     role?: string | number | (role object that must include either "id" or "name", and if both exist then "id" is used and "name" is ignored; all other properties are ignored)
// }
// If "role" is undefined or not present, then the default role ("Contributor") is assigned
// -or-
// username (a string) - in this mode, the default role ("Contributor") is assigned
class InvalidMemberships extends TypeError {
    code: string;
    details: any;
    constructor(code: string, details: any) {
        super(code);  // TODO: Perhaps construct a user-readable message from the various possible codes
        this.name = this.constructor.name;
        this.details = details;
        this.code = code;
    }
}

const hop = Object.prototype.hasOwnProperty;

function canonicalizeRole(role: StringOrNumber): StringOrNumber {
    if (typeof role === 'string' || typeof role === 'number') {
        return role;
    } else if (!role) {
        return defaultRoleId;
    } else if (hop.call(role, 'id') && typeof (role as any).id === 'number') {
        return (role as any).id;
    } else if (hop.call(role, 'name') && typeof (role as any).name === 'string') {
        return (role as any).name;
    } else {
        throw new InvalidMemberships('invalid_role', role);
    }
}

function canonicalizeUser(user: string | Record<string, string>) {
    if (typeof user === 'string') {
        return user;
    } else if (hop.call(user, 'username') && typeof user.username === 'string') {
        return user.username;
    } else {
        throw new InvalidMemberships('invalid_user', user);
    }
}

function canonicalizeMembershipList(memberships: any | any[]) {
    const result = Array.isArray(memberships) ? memberships : [memberships];
    for (let i = 0; i < result.length; i++) {
        const record = result[i];
        if (typeof record === 'string') {
            result[i] = { user: record, role: defaultRoleId };
        } else if (hop.call(record, 'user') && hop.call(record, 'role')) {
            const user = canonicalizeUser(record.user);
            const role = canonicalizeRole(record.role);
            result[i] = { user, role };
        } else if (hop.call(record, 'user')) {
            const user = canonicalizeUser(record.user);
            const role = defaultRoleId;
            result[i] = { user, role };
        } else if (hop.call(record, 'username') && typeof record.username === 'string' && hop.call(record, 'role')) {
            const user = record.username;
            const role = canonicalizeRole(record.role);
            result[i] = { user, role };
        } else if (hop.call(record, 'username') && typeof record.username === 'string') {
            const user = record.username;
            const role = defaultRoleId;
            result[i] = { user, role };
        } else {
            throw new InvalidMemberships('invalid_membership_record', memberships);
        }
    }
    return result;
}

export { addUserWithRole, addUserWithRoleByProjectCode, removeUserFromProject, removeUserFromProjectByProjectCode, getProjectsForUser, canonicalizeUser, canonicalizeRole, canonicalizeMembershipList, InvalidMemberships };
