import { missingRequiredParam } from '$lib/utils/commonErrors'
import { repoPathPublic, repoPathPrivate } from '$lib/utils/repos/config'
import { getHgLog } from '$lib/log/hgLog'

// GET /api/projects/{projectCode}/log - return JSON representation of Mercurial log
export async function get({params, query}) {
    if (!params.projectCode) {
        return missingRequiredParam('project code', 'URL')
    }
    if (params.projectCode.indexOf('/') !== -1) {
        return { status: 400, body: { description: `Project code may not contain / characters`, code: `invalid_projectCode_contains_slashes` }}
    }
    // TODO: Move the business logic into a function in src/lib, which returns the promise from execFile
    const path = `${query.private ? repoPathPrivate : repoPathPublic}/${params.projectCode}`

    try {
        var hgLog = getHgLog(path, query.get('limit'), query.get('rev'))
        // Return result as a JSON string without parsing or modifying it
        return { status: 200, body: hgLog, headers: { 'content-type': 'application/json' } }
    } catch (error) {
        return { status: 500, body: { error, description: `Internal error getting project activity log`, code: 'project_log_error' }};
    }
}

