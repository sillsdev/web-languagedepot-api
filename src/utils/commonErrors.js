function missingRequiredParam(paramName, location) {
    const where = location ? ` in ${location}` : '';
    return { status: 400, body: { description: `No ${paramName} specified${where}`, code: `missing_${paramName}` }};
}

function authTokenRequired() {
    return { status: 401, body: { description: `Bearer authentication token required; please login to get a token`, code: `auth_token_required` }};
}

function notAllowed() {
    return { status: 403, body: { description: `Access denied`, code: `forbidden` }};
}

function sqlError(error) {
    console.log('SQL error:', error);
    return { status: 500, body: { error, description: `SQL error; see error property for details`, code: 'sql_error' }};
}

function duplicateKeyError(itemKey, itemName) {
    return { status: 500, body: { description: `Duplicate ${itemName} found in database`, code: `duplicate_${itemKey}` }};
}

function notFound(itemKey, itemName) {
    return { status: 404, body: { description: `No such ${itemName}`, code: `unknown_${itemKey}` }};
}

function jsonRequired(method, path) {
    return { status: 400, body: { description: `Body of ${method} request to ${path} should be JSON`, code: 'json_required' }};
}

function cannotUpdateMissing(itemKey, itemName) {
    return { status: 404, body: { description: `${itemName} ${itemKey} not found; cannot update a missing ${itemName}`, code: `unknown_${itemKey}` }};
}

export { missingRequiredParam, authTokenRequired, notAllowed, sqlError, duplicateKeyError, notFound, jsonRequired, cannotUpdateMissing };
