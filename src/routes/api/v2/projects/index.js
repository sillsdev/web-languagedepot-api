import { dbs } from '$lib/db/dbsetup';
import { jsonRequired, missingRequiredParam, authTokenRequired, notAllowed } from '$lib/utils/commonErrors';
import { retryOnServerError } from '$lib/utils/commonSqlHandlers';
import { allowAdminOnly } from '$lib/utils/db/authRules';
import { getAllProjects, countAllProjectsQuery, createOneProject } from '$lib/utils/db/projects';

// GET /api/v2/projects - return list of all projects
// Security: must be a site admin (list of all projects could contain sensitive names)
export async function get({ query, headers }) {
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowAdminOnly(db, { headers });
    if (authResult.status === 200) {
        // URLSearchParams objects don't destructure well, so convert to a POJO
        const queryParams = Object.fromEntries(query);
        return getAllProjects(db, queryParams);
    } else {
        return authResult;
    }
}

// HEAD /api/v2/projects - return 200 if at least one project exists, 404 if zero projects
// Security: must be a site admin (list of all projects could contain sensitive names)
export async function head({ query, headers }) {
    const db = query.private ? dbs.private : dbs.public;
    const authResult = await allowAdminOnly(db, { headers });
    if (authResult.status === 200) {
        const queryParams = Object.fromEntries(query);
        const count = await retryOnServerError(countAllProjectsQuery(db, queryParams));
        const status = count > 0 ? 200 : 404;
        return { status, body: {} };
    } else {
        return { status: authResult.status, body: {} }
    }
}

// POST /api/v2/projects - create project, or update project if it aleady exists.
// Security: anyone may create a project, and they become the project's first manager. Updating is restricted to existing project managers or site admins.
export async function post({ path, body, query, headers }) {
    if (typeof body !== 'object') {
        return jsonRequired('POST', path);
    }
    if (!body || !body.projectCode) {
        return missingRequiredParam('projectCode', `body of POST request to ${path}`);
    }
    const projectCode = body.projectCode;
    const db = query.private ? dbs.private : dbs.public;
    const result = await createOneProject(db, projectCode, body, headers);
    // Add Content-Location header on success so client knows where to find the newly-created project
    if (result && result.status && result.status >= 200 && result.status < 300) {
        return { ...result, headers: { ...result.headers, 'Content-Location': `${path}/${projectCode}` } };
    } else {
        return result;
    }
}
