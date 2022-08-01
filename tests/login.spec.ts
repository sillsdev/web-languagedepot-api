import { expect } from '@playwright/test'
import { test } from './fixtures.js'
import jwt from 'jsonwebtoken'

function authHeader(username: string, password: string) {
    const b64 = Buffer.from(`${username}:${password}`).toString('base64')
    return { headers: { 'Authorization': `Basic ${b64}` } }
}

test.describe('GET /api/v2/login', () => {

    // Expected shape:
    // If HTTP 200 OK:
    // {
    //     access_token: string (the token),
    //     expires_in: number (in seconds),
    //     token_type: "JWT" (always the same string)
    // }
    //
    // If HTTP error code:
    // {
    //     code: string (e.g., "basic_auth_required")
    //     description: string (e.g., "HTTP basic authentication required")
    // }

    test('omitting basic auth causes HTTP 401', async ({ request }) => {
        const result = await request.get('login');
        expect(result.status()).toBe(401);
        const json = await result.json();
        expect(json).toHaveProperty('code');
        expect(json.code).toEqual('basic_auth_required')
    })

    test('username not recognized causes HTTP 403', async function({ browser }) {
        // This approach does NOT work, as Playwright sends a first request with no credentials, expecting to pass credentials on a 401
        // const context = await browser.newContext({ httpCredentials: {username: 'no_such_username', password: 'incorrect_password'}})
        // const result = await context.request.get('http://localhost:8800/login')
        // console.log(result);
        // Instead, you must create the HTTP Basic Auth header yourself
        const context = await browser.newContext()
        const result = await context.request.get('login', authHeader('no_such_username', 'incorrect_password'))
        expect(result.status()).toBe(403)
        const json = await result.json();
        expect(json).toHaveProperty('code')
        expect(json.code).toEqual('forbidden')
        // But since you have to send the headers yourself, it's easier to just grab the `request` fixture, as in the next test
    })

    test('invalid password causes rejection with HTTP 403', async function({ request }) {
        const result = await request.get('login', authHeader('admin', 'incorrect_password'))
        expect(result.status()).toBe(403)
        const json = await result.json();
        expect(json).toHaveProperty('code')
        expect(json.code).toEqual('forbidden')
    })

    test('correct password returns JSON response containing valid access token', async function({ request }) {
        const result = await request.get('login', authHeader('admin', 'x'))
        const json = await result.json();
        expect(json).toHaveProperty('access_token');
        expect(json).toHaveProperty('expires_in');
        expect(json).toHaveProperty('token_type');
        expect(json.token_type).toEqual('JWT');
        expect(typeof json.access_token).toBe('string');
        const signKey = process.env['JWT_SIGNING_KEY'] || 'not-a-secret';
        jwt.verify(json.access_token, signKey)  // Will throw if verification fails
    })

    test('routes that require auth will return 401 when no token provided', async function({ request }) {
        const result = await request.get('users/admin/projects');
        expect(result.status()).toBe(401);
        const json = await result.json();
        expect(json).toHaveProperty('code');
        expect(json.code).toEqual('auth_token_required')
    })

    test('routes that require auth will return 403 when an invalid token is provided', async function({ request, adminToken }) {
        const badToken = adminToken + 'A'
        const result = await request.get('users/admin/projects', { headers: { 'Authorization': `Bearer ${badToken}`}})
        expect(result.status()).toEqual(403)
        const json = await result.json()
        expect(json).toHaveProperty('code')
        expect(json.code).toEqual('forbidden')
    })

    test('routes that require auth will return 200 when a valid token is provided', async function({ request, adminToken }) {
        const result = await request.get('users/admin/projects', { headers: { 'Authorization': `Bearer ${adminToken}`}})
        expect(result.status()).toEqual(200)
    })
});
