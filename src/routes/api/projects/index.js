import { dbs } from '$components/models/dbsetup';
import { jsonRequired, missingRequiredParam } from '$utils/commonErrors';
import { getAllProjects, countAllProjectsQuery, createOneProject } from '$utils/db/projects';

export function get({ query }) {
    const db = query.private ? dbs.private : dbs.public;
    // URLSearchParams objects don't destructure well, so convert to a POJO
    const queryParams = Object.fromEntries(query);
    return getAllProjects(db, queryParams);
}

export async function head({ query }) {
    const db = query.private ? dbs.private : dbs.public;
    const queryParams = Object.fromEntries(query);
    const count = await countAllProjectsQuery(db, queryParams);
    const status = count > 0 ? 200 : 404;
    return { status, body: {} };
}

export async function post({ path, body, query }) {
    if (typeof body !== 'object') {
        return jsonRequired('POST', path);
    }
    if (!body || !body.projectCode) {
        return missingRequiredParam('projectCode', `body of POST request to ${path}`);
    }
    const projectCode = body.projectCode;
    const db = query.private ? dbs.private : dbs.public;
    const result = await createOneProject(db, projectCode, body);
    // Add Content-Location header on success so client knows where to find the newly-created project
    if (result && result.status && result.status >= 200 && result.status < 300) {
        return { ...result, headers: { ...result.headers, 'Content-Location': `${path}/${projectCode}` } };
    } else {
        return result;
    }
}
