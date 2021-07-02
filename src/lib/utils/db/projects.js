import { MemberRole, Membership, Project, managerRoleId, projectStatus } from '$lib/db/models';
import { cannotModifyPrimaryKey, inconsistentParams, cannotUpdateMissing, authTokenRequired, notAllowed } from '$lib/utils/commonErrors';
import { onlyOne, atMostOne, catchSqlError, retryOnServerError } from '$lib/utils/commonSqlHandlers';
import { verifyJwtAuth } from './auth';
import { allowManagerOrAdmin } from './authRules';
import { addUserWithRole, removeUserFromProject } from './usersAndRoles';

export function allProjectsQuery(db, { limit = undefined, offset = undefined } = {}) {
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

export async function createOneProject(db, projectCode, newProject, headers) {
    const authUser = await verifyJwtAuth(db, headers);
    if (!authUser) {
        if (authUser === undefined) {
            return authTokenRequired();
        } else {
            return notAllowed();
        }
    }
    if (newProject && newProject.projectCode) {
        if (projectCode !== newProject.projectCode) {
            return inconsistentParams('projectCode');
        }
    }
    const trx = await Project.startTransaction(db);
    const query = Project.query(trx).forUpdate().where('identifier', projectCode);
    const result = await atMostOne(query, 'projectCode', 'project code',
    async () => {
        const project = await retryOnServerError(Project.query(trx).insertAndFetch(newProject));
        const subResult = await addUserWithRole(trx, project, authUser.login, managerRoleId);
        if (subResult && subResult.status && subResult.status >= 200 && subResult.status < 400) {
            return { status: 201, body: project };
        } else {
            if (subResult.status === 404 && subResult.code === 'unknown_roleId') {
                // Manager role ID missing? Something is badly wrong
                console.log(`Internal server error creating ${projectCode}: Manager role appears missing!`);
                return { status: 500, code: 'internal_server_error', description: 'Internal Server Error' }
            } else if (subResult.status === 404 && subResult.code === 'unknown_username') {
                return { status: 400, code: subResult.code, description: subResult.description }
            } else {
                console.log(`Internal server error creating ${projectCode}. Details:`, subResult);
                return { status: 500, code: 'internal_server_error', description: 'Sorry, something went wrong' }
            }
        }
    },
    async (project) => {
        const authResult = await allowManagerOrAdmin(db, { params: { projectCode }, headers })
        if (authResult.status === 200) {
            const result = await retryOnServerError(Project.query(trx).updateAndFetchById(project.id, newProject));
            return { status: 200, body: result };
        } else {
            return authResult;
        }
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
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
            } else if (updateData.members.remove) {
                for (const removeMember of updateData.members.remove) {
                    const result = await removeUserFromProject(trx, project, removeMember.user);
                    if (result && result.status && result.status >= 400) {
                        return result;
                    }
                }
            } else if (updateData.members.removeUser && typeof updateData.members.removeUser === 'string') {
                const result = await removeUserFromProject(trx, project, updateData.members.removeUser);
                if (result && result.status && result.status >= 400) {
                    return result;
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
