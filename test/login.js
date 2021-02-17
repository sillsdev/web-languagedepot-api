const api = require('./testsetup').apiv2
const expect = require('chai').expect
const loginUtils = require('./loginUtils')

describe('GET /api/v2/login', function() {

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

    it('omitting basic auth causes HTTP 401', async function() {
        const result = await api('login', {throwHttpErrors: false})
        expect(result.statusCode).to.equal(401)
        expect(result.body).to.contain.keys('code', 'description')
        expect(result.body.code).to.equal('basic_auth_required')
    })

    it('username not recognized causes HTTP 403', async function() {
        const result = await api('login', {username: 'no_such_username', password: 'incorrect_password', throwHttpErrors: false})
        expect(result.statusCode).to.equal(403)
        expect(result.body).to.contain.keys('code', 'description')
        expect(result.body.code).to.equal('forbidden')
    })

    it('invalid password causes rejection with HTTP 403', async function() {
        const result = await api('login', {username: 'admin', password: 'incorrect_password', throwHttpErrors: false})
        expect(result.statusCode).to.equal(403)
        expect(result.body).to.contain.keys('code', 'description')
        expect(result.body.code).to.equal('forbidden')
    })

    it('correct password returns JSON response containing valid access token', async function() {
        const result = await api('login', {username: 'admin', password: 'x'})
        expect(result.body).to.contain.keys('access_token', 'expires_in', 'token_type')
        expect(result.body.token_type).to.equal('JWT')
        this.token = result.body.access_token
        expect(this.token).to.be.a('string')
        return loginUtils.verifyToken(this.token)  // Will cause test failure if verifyToken returns rejected promise
    })

    it('routes that require auth will return 401 when no token provided', async function() {
        const result = await api('users/admin/projects', {throwHttpErrors: false})
        expect(result.statusCode).to.equal(401)
        expect(result.body).to.contain.keys('code', 'description')
        expect(result.body.code).to.equal('auth_token_required')
    })

    it('routes that require auth will return 403 when an invalid token is provided', async function() {
        const badToken = this.token + 'A'
        const result = await api('users/admin/projects', {throwHttpErrors: false, headers: {authorization: `Bearer ${badToken}`}})
        expect(result.statusCode).to.equal(403)
        expect(result.body).to.contain.keys('code', 'description')
        expect(result.body.code).to.equal('forbidden')
    })

    it('routes that require auth will return 200 when a valid token is provided', async function() {
        const result = await api('users/admin/projects', {throwHttpErrors: false, headers: {authorization: `Bearer ${this.token}`}})
        expect(result.statusCode).to.equal(200)
    })
})
