import { Project, projectStatus } from '$components/models/models';
import { managerRoleId } from '$components/models/Role';
import { cannotModifyPrimaryKey, inconsistentParams } from '$utils/commonErrors';
import { onlyOne, atMostOne, catchSqlError } from '$utils/commonSqlHandlers';
import { addUserWithRole } from './usersAndRoles';

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
        const projects = await allProjectsQuery(db, params);
        return { status: 200, body: projects };
    });
}

export function oneProjectQuery(db, projectCode) {
    return Project.query(db).where('identifier', projectCode);
}

export function getOneProject(db, projectCode) {
    // Also want to collect and return member data here
    const query = oneProjectQuery(db, projectCode).withGraphJoined('members.[user, role]');
    return onlyOne(query, 'projectCode', 'project code', project => {
        // console.log(project);
        project.members = project.members.map(m => ({
            user: m.user,
            role: m.role.name,
        }))
        // console.log('Result:', project);
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
        const result = await Project.query(trx).insertAndFetch(newProject);
        return { status: 201, body: result };
    },
    async (project) => {
        const result = await Project.query(trx).updateAndFetchById(project.id, newProject);
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        const initialManagerUsername =
            initialManager && initialManager.login ? initialManager.login :
            initialManager && initialManager.username ? initialManager.username :
            initialManager;
        if (initialManagerUsername) {
            const subResult = await addUserWithRole(trx, projectCode, initialManagerUsername, managerRoleId);
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
        const result = await Project.query(trx).patchAndFetchById(project.id, updateData);
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
        return cannotUpdateMissing(projectCode, 'project');
    },
    async (project) => {
        await Project.query(trx).findById(project.id).patch({ status: projectStatus.archived });
        return { status: 204, body: {} };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        await trx.commit();
    } else {
        await trx.rollback();
    }
    return result;
}
