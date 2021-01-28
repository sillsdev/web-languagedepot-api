import { Project } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { missingRequiredParam } from '$utils/commonErrors';
import { catchSqlError } from '$utils/commonSqlHandlers';

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
        if (typeof query.limit === 'number') {
            search = search.limit(query.limit);
        }
        if (typeof query.offset === 'number') {
            search = search.offset(query.offset);
        }
        const users = await search;
        return { status: 200, body: users };
    });
}
