import type { RequestHandler } from '@sveltejs/kit';

// Fallback route if no other routes found - returns 404
// This exists so that Svelte-Kit won't generate its default 404 HTML page

const return404: RequestHandler = async ({ url }) => {
    const path = url.pathname ? url.pathname + ' ' : '';
    const msg = `Endpoint ${path}not found`;
    const err = { description: msg, code: `endpoint_not_found` };
    return { status: 404, body: err};
}

export const GET = return404;
export const HEAD = return404;
export const POST = return404;
export const PUT = return404;
export const DELETE = return404;
export const PATCH = return404;
