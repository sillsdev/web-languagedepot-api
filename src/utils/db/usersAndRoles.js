import { Project, Role, User, Membership, MemberRole } from '$components/models/models';
import { onlyOne, atMostOne } from '$utils/commonSqlHandlers';

async function addUserWithRole(projectCode, username, rolename, db) {
    const trx = await Project.startTransaction(db);
    const query = Project.query(trx).select('id').forShare().where('identifier', projectCode);
    const result = await onlyOne(query, 'projectCode', 'project code',
    async (project) => {
        const query = Role.query(trx).select('id').where('name', rolename);
        return onlyOne(query, 'rolename', 'role name',
        async (role) => {
            const query = User.query(trx).select('id').where('login', username);
            return onlyOne(query, 'username', 'username',
            async (user) => {
                let membership = await Membership.query(trx).where({user_id: user.id, project_id: project.id});
                if (!membership || membership.length === 0) {
                    membership = await Membership.query(trx).insert({user_id: user.id, project_id: project.id});
                } else {
                    membership = membership[0];
                }
                // Update role if already a member, else add new MemberRole
                let memberRole = await MemberRole.query(trx).where({member_id: membership.id});
                if (memberRole.length === 0) {
                    await MemberRole.query(trx).insert({member_id: membership.id, role_id: role.id});
                } else {
                    await MemberRole.query(trx).findById(memberRole[0].id).patch({role_id: role.id});
                }
                return { status: 204, body: {} };
            });
        });
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}

async function removeUserFromProject(projectCode, username, db) {
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
            await MemberRole.query(trx).where('member_id', member.id).delete();
            // TODO: Find out how to use .unrelate() and .relatedQuery() to achieve this effect
            await Membership.query(trx).deleteById(member.id);
            return { status: 204, body: {} };
        });
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}

export { addUserWithRole, removeUserFromProject };
