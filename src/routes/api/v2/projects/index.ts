import type { RequestHandler } from '@sveltejs/kit';
import { dbs } from '$lib/db/dbsetup';
import { jsonRequired, missingRequiredParam, authTokenRequired, notAllowed } from '$lib/utils/commonErrors';
import { allowAdminOnly } from '$lib/utils/db/authRules';
import { getAllProjects, countAllProjectsQuery, createOneProject } from '$lib/utils/db/projects';

// GET /api/v2/projects - return list of all projects
// Security: must be a site admin (list of all projects could contain sensitive names)
export const GET: RequestHandler = async ({ url, request }) => {
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const authResult = await allowAdminOnly(db, request);
    if (authResult.status === 200) {
        // URLSearchParams objects don't destructure well, so convert to a POJO
        const queryParams = Object.fromEntries(url.searchParams);
        return getAllProjects(db, queryParams);
    } else {
        return authResult;
    }
}

// POST /api/v2/projects - create project, or update project if it aleady exists.
// Security: anyone may create a project, and they become the project's first manager. Updating is restricted to existing project managers or site admins.
export const POST: RequestHandler = async ({ url, request }) => {
    let body: any;
    try {
        body = await request.json();
    } catch (e: any) {
        // TODO: Consider letting this throw and letting Svelte-Kit turn the resulting exception into a 500 Server Error that will report the JSON error more precisely
        return jsonRequired('POST', url.pathname);
    }
    if (!body || !body.projectCode) {
        return missingRequiredParam('projectCode', `body of POST request to ${url.pathname}`);
    }
    const projectCode = body.projectCode;
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;

    const result = await createOneProject(db, projectCode, body, request.headers);
    // Add Content-Location header on success so client knows where to find the newly-created project
    if (result && result.status && result.status >= 200 && result.status < 300) {
        return { ...result, headers: { ...result.headers, 'Content-Location': `${url.pathname}/${projectCode}` } };
    } else {
        return result;
    }
}
