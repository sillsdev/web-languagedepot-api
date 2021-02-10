import { Project, Role, User, Membership, MemberRole } from '$db/models';
import { onlyOne, atMostOne, catchSqlError, retryOnServerError } from '$utils/commonSqlHandlers';

async function addUserWithRole(db, projectCode, username, roleNameOrId) {
    const trx = await Project.startTransaction(db);
    const query = Project.query(trx).select('id').forShare().where('identifier', projectCode);
    const result = await onlyOne(query, 'projectCode', 'project code',
    async (project) => {
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
            async (user) => {
                let membership = await retryOnServerError(Membership.query(trx).where({user_id: user.id, project_id: project.id}));
                if (!membership || membership.length === 0) {
                    membership = await retryOnServerError(Membership.query(trx).insert({user_id: user.id, project_id: project.id}));
                } else {
                    membership = membership[0];
                }
                // Update role if already a member, else add new MemberRole
                let memberRole = await retryOnServerError(MemberRole.query(trx).where({member_id: membership.id}));
                if (memberRole.length === 0) {
                    await retryOnServerError(MemberRole.query(trx).insert({member_id: membership.id, role_id: role.id}));
                } else {
                    await retryOnServerError(MemberRole.query(trx).findById(memberRole[0].id).patch({role_id: role.id}));
                }
                return { status: 204, body: {} };
            });
        });
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}

async function removeUserFromProject(db, projectCode, username) {
    const trx = await Project.startTransaction(db);
    const query = Project.query(trx).where('identifier', projectCode).withGraphJoined('members.[user, role]');
    const result = await onlyOne(query, 'projectCode', 'project code',
    async (project) => {
        users = project.members.filter(member => member.user.login === username);
        return atMostOne(users, 'membership', `project membership for ${username} in ${projectCode}`,
        () => {
            // Not a member? Then there's nothing to delete
            return { status: 204, body: {} };
        },
        async (member) => {
            await retryOnServerError(MemberRole.query(trx).where('member_id', member.id).delete());
            // TODO: Find out how to use .unrelate() and .relatedQuery() to achieve this effect
            await retryOnServerError(Membership.query(trx).deleteById(member.id));
            return { status: 204, body: {} };
        });
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}

function getProjectsForUser(db, { username, rolename } = {}) {
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
            const result = user.memberships.map(m => ({
                projectCode: m.project.identifier,
                role: m.role.name,
            }));
            return { status: 200, body: result };
        }
    });
}


export { addUserWithRole, removeUserFromProject, getProjectsForUser };
