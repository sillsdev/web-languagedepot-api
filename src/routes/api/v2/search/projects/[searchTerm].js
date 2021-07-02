import { Project } from '$lib/db/models';
import { dbs } from '$lib/db/dbsetup';
import { missingRequiredParam } from '$lib/utils/commonErrors';
import { catchSqlError } from '$lib/utils/commonSqlHandlers';

// GET /api/v2/search/projects/{searchTerm} - search projects for text in project code, name, or description
// Security: must be a site admin (searching for "a" could reveal nearly all projects, including some that could contain sensitive names)
// TODO: Add security check
export async function get({ params, query, path }) {
    const db = query.private ? dbs.private : dbs.public;
    if (!params.searchTerm) {
        return missingRequiredParam('searchTerm', path);
    }
    return catchSqlError(async () => {
        let search = Project.query(db)
            .where('identifier', 'like', `%${params.searchTerm}%`)
            .orWhere('name', 'like', `%${params.searchTerm}%`)
            .orWhere('description', 'like', `%${params.searchTerm}%`)
            ;

        const limit = query.get('limit');
        if (limit) {
            search = search.limit(limit);
        }
        const offset = query.get('offset');
        if (offset) {
            search = search.offset(offset);
        }

        const projects = await search;
        return { status: 200, body: projects };
    });
}

// TODO: Consider adding ?withMembers as a query parameter to return membership records alongside the projects returned by the search
