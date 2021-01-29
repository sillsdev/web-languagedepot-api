import { Project } from '$components/models/models';
import { atMostOne, catchSqlError } from '$utils/commonSqlHandlers';

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
        // console.log('Projects result:', projects);
        return { status: 200, body: projects };
    });
}

export async function createProject(db, projectCode, newProject) {
    const trx = Project.startTransaction(db);
    const query = Project.query(trx).select('id').forUpdate().where('identifier', projectCode);
    const result = await atMostOne(query, 'projectCode', 'project code',
    async () => {
        const result = await Project.query(trx).insertAndFetch(newProject);
        return { status: 201, body: result };
    },
    async (project) => {
        const result = await Project.query(trx).updateAndFetchById(project.id, body);
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}

// TODO: Reusable functions for project CRUD
