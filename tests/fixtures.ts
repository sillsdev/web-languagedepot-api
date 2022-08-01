import { test as base } from '@playwright/test';
import jwt from 'jsonwebtoken'

let adminToken = undefined

const adminUsername = process.env['ADMIN_USERNAME'] || 'admin'
const adminPassword = process.env['ADMIN_PASSWORD'] || 'x'
const jwtAudience = process.env['JWT_AUDIENCE'] || 'https://admin.languagedepot.org/api/v2';
// Some tests will construct a JWT and make sure it's accepted by the server,
// so make sure CI passes the same signing key and audience to the tests as the server
const signKey = process.env['JWT_SIGNING_KEY'] || 'not-a-secret';

function makeJwt(username) {
    var token = jwt.sign({
        sub: username,
        aud: jwtAudience,
        iat: Math.floor(Date.now() / 1000) - 5,  // Backdate 5 seconds in case of clock skew
    }, signKey, { expiresIn: '5m', algorithm: 'HS256' });
    return token;
}

function authHeader(username: string, password: string) {
    const b64 = Buffer.from(`${username}:${password}`).toString('base64')
    return { headers: { 'Authorization': `Basic ${b64}` } }
}

export const test = base.extend({
    adminToken: async ({ request }, use) => {
        if (adminToken) {
            use(adminToken)
        } else {
            const result = await request.get('login', authHeader(adminUsername, adminPassword));
            const json = await result.json();
            jwt.verify(json.access_token, signKey)  // Will throw if verification fails
            adminToken = json.access_token
            use(adminToken)
        }
    },
    makeJwt: async ({}, use) => {
        use(makeJwt)
    }
})
