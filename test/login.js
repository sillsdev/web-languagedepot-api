const api = require('./testsetup').apiv2
const expect = require('chai').expect
const loginUtils = require('./loginUtils')

describe('/login route', function() {
    it('omitting basic auth causes HTTP 401', async function() {
        const result = await api('login', {throwHttpErrors: false})
        expect(result.statusCode).to.equal(401)
    })

    it('invalid password causes rejection with HTTP 403', async function() {
        const result = await api('login', {username: 'admin', password: 'incorrect_password', throwHttpErrors: false})
        expect(result.statusCode).to.equal(403)
    })

    it('correct password returns JSON response containing valid access token', async function() {
        const result = await api('login', {username: 'admin', password: 'x'})
        expect(result.body).to.contain.keys('access_token', 'expires_in', 'token_type')
        expect(result.body.token_type).to.equal('JWT')
        this.token = result.body.access_token
        expect(this.token).to.be.a('string')
        return loginUtils.verifyToken(this.token)  // Will cause test failure if verifyToken returns rejected promise
    })
})
