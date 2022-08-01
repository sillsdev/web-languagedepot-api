import type { RequestHandler } from '@sveltejs/kit';
import { Project } from '$lib/db/models';
import { dbs } from '$lib/db/dbsetup';
import { missingRequiredParam } from '$lib/utils/commonErrors';
import { catchSqlError } from '$lib/utils/commonSqlHandlers';
import { allowAdminOnly } from '$lib/utils/db/authRules';

// GET /api/v2/search/projects/{searchTerm} - search projects for text in project code, name, or description
// Security: must be a site admin (searching for "a" could reveal nearly all projects, including some that could contain sensitive names)
export const GET: RequestHandler = async ({ params, url, request: { headers } }) => {
    if (!params.searchTerm) {
        return missingRequiredParam('searchTerm', url.pathname);
    }
    const db = url.searchParams.get('private') ? dbs.private : dbs.public;


    const authResult = await allowAdminOnly(db, { headers });
    if (authResult.status === 200) {
        if (!params.searchTerm) {
            return missingRequiredParam('searchTerm', url.pathname);
        }
        return catchSqlError(async () => {
            let search = Project.query(db)
                .where('identifier', 'like', `%${params.searchTerm}%`)
                .orWhere('name', 'like', `%${params.searchTerm}%`)
                .orWhere('description', 'like', `%${params.searchTerm}%`)
                ;

            const limit = url.searchParams.get('limit') ?? '';
            if (limit) {
                search = search.limit(+limit);
            }
            const offset = url.searchParams.get('offset') ?? '';
            if (offset) {
                search = search.offset(+offset);
            }

            const projects = await search;
            return { status: 200, body: projects };
        });
    } else {
        return authResult;
    }
}

// TODO: Consider adding ?withMembers as a query parameter to return membership records alongside the projects returned by the search
