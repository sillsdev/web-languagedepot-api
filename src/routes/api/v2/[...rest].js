// Fallback route if no other routes found - returns 404
// This exists so that Svelte-Kit won't generate its default 404 HTML page

function return404(req) {
    const path = req.path ? req.path + ' ' : '';
    const msg = `Endpoint ${path}not found`;
    const err = { description: msg, code: `endpoint_not_found` };
    return { status: 404, body: err};
}

export function get(req) {
    return return404(req);
}

export function head(req) {
    return return404(req);
}

export function post(req) {
    return return404(req);
}

export function put(req) {
    return return404(req);
}

export function del(req) {
    return return404(req);
}

export function patch(req) {
    return return404(req);
}
