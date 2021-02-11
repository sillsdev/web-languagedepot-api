import { MemberRole, Membership, Project, managerRoleId, projectStatus } from '$db/models';
import { cannotModifyPrimaryKey, inconsistentParams, cannotUpdateMissing } from '$utils/commonErrors';
import { onlyOne, atMostOne, catchSqlError, retryOnServerError } from '$utils/commonSqlHandlers';
import { addUserWithRoleByProjectCode, addUserWithRole } from './usersAndRoles';

export function allProjectsQuery(db, { limit, offset } = {}) {
    let query = Project.query(db);
    if (limit) {
        query = query.limit(limit);
    }
    if (offset) {
        query = query.offset(offset);
    }
    return query;
}

export function countAllProjectsQuery(db, params) {
    return allProjectsQuery(db, params).resultSize();
}

export function getAllProjects(db, params) {
    return catchSqlError(async () => {
        const projects = await retryOnServerError(allProjectsQuery(db, params));
        return { status: 200, body: projects };
    });
}

export function oneProjectQuery(db, projectCode) {
    return Project.query(db).where('identifier', projectCode);
}

export function getOneProject(db, projectCode) {
    const query = oneProjectQuery(db, projectCode).withGraphJoined('members.[user, role]');
    return onlyOne(query, 'projectCode', 'project code', project => {
        project.members = project.members.map(m => ({
            user: m.user,
            role: m.role.name,
        }))
        return { status: 200, body: project };
    });
}

export async function createOneProject(db, projectCode, newProject, initialManager = undefined) {
    if (newProject && newProject.projectCode) {
        if (projectCode !== newProject.projectCode) {
            return inconsistentParams('projectCode');
        }
    }
    const trx = await Project.startTransaction(db);
    const query = Project.query(trx).select('id').forUpdate().where('identifier', projectCode);
    const result = await atMostOne(query, 'projectCode', 'project code',
    async () => {
        const result = await retryOnServerError(Project.query(trx).insertAndFetch(newProject));
        return { status: 201, body: result };
    },
    async (project) => {
        // TODO: Should this automatically reactivate a project that's been archived? Or should an archived project's code be permanently retired?
        const result = await retryOnServerError(Project.query(trx).updateAndFetchById(project.id, newProject));
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        const initialManagerUsername =
            initialManager && initialManager.login ? initialManager.login :
            initialManager && initialManager.username ? initialManager.username :
            initialManager;
        if (initialManagerUsername) {
            const subResult = await addUserWithRoleByProjectCode(trx, projectCode, initialManagerUsername, managerRoleId);
            if (subResult && subResult.status && subResult.status >= 200 && subResult.status < 400) {
                await trx.commit();
            } else {
                await trx.rollback();
            }
        } else {
            await trx.commit();
        }
    } else {
        await trx.rollback();
    }
    return result;
}

export async function patchOneProject(db, projectCode, updateData) {
    if (updateData && updateData.projectCode) {
        if (projectCode !== updateData.projectCode) {
            return cannotModifyPrimaryKey('projectCode', 'project');
        }
    }
    const trx = await Project.startTransaction(db);
    const query = oneProjectQuery(trx, projectCode).select('id').forUpdate();
    const result = await atMostOne(query, 'projectCode', 'project code',
    () => {
        return cannotUpdateMissing(projectCode, 'project');
    },
    async (project) => {
        if (updateData && updateData.members) {
            if (updateData.members.add) {
                for (const addMember of updateData.members.add) {
                    const result = await addUserWithRole(trx, project, addMember.user, addMember.role);
                    if (result && result.status && result.status >= 400) {
                        return result;
                    }
                }
            }
        }
        const result = await retryOnServerError(Project.query(trx).patchAndFetchById(project.id, updateData));
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}

export async function deleteOneProject(db, projectCode) {
    const trx = await Project.startTransaction(db);
    const query = oneProjectQuery(trx, projectCode).select('id').forUpdate();
    const result = await atMostOne(query, 'projectCode', 'project code',
    () => {
        // Deleting a non-existent project is not an error
        return { status: 204, body: {} };
    },
    async (project) => {
        // await retryOnServerError((Project.query(trx).findById(project.id).patch({ status: projectStatus.archived }));
        // DEBUG: Delete all project memberships and member_roles too, so that we can start over with a fresh slate when testing
        const membershipIds = (await retryOnServerError(Membership.query(trx).where({project_id: project.id}).select('id'))).map(m => m.id);
        await retryOnServerError(MemberRole.query(trx).whereIn('member_id', membershipIds).delete());
        await retryOnServerError(Membership.query(trx).whereIn('id', membershipIds).delete());
        await retryOnServerError(Project.query(trx).findById(project.id).delete());
        return { status: 204, body: {} };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}
