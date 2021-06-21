import { missingRequiredParam } from '$lib/utils/commonErrors'
import { repoPathPublic, repoPathPrivate } from '$lib/utils/repos/config'
import { execFile as origExecFile } from 'child_process'
import { promisify } from 'util'
const execFile = promisify(origExecFile)

const template = `\\{"rev":{rev|json},"hash":{node|json},"shorthash":{node|short|json},"branch":{branch|json},"parents":{parents|json},"date":{date|isodate|json},"author":{author|json},"desc":{desc|json}},`
// date format could also be rfc3339date for dates formatted like '2018-03-26T08:07:45+00:00'
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
    var limit = query.get('limit')
    limit = limit ? parseInt(limit) : 1000;
    if (!limit || limit <= 0 || limit > 1000) {
        limit = 1000
    }
    var rev = query.get('rev')
    rev = rev ? parseInt(rev) : NaN;
    const args = ['log', '-C', path, '--template', template, '-l', limit.toString()]
    if (!isNaN(rev)) {
        args.push('-r', `${rev}:0`)
    }
    try {
        var hgLogJson = await execFile('hg', args)
        // Trim final comma and turn into a list, even if only one result
        var asList = hgLogJson.stdout ? `[${hgLogJson.stdout.slice(0,-1)}]\n` : '[]\n'
        return { status: 200, body: asList, headers: { 'content-type': 'application/json' } }
    } catch (error) {
        return { status: 500, body: { error, description: `Internal error getting project activity log`, code: 'project_log_error' }};
    }
}

