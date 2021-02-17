const api = require('./testsetup').apiv2
const expect = require('chai').expect
const loginUtils = require('./loginUtils')

describe('/projects API route', function() {
    before('set up API', function() {
        this.adminToken = loginUtils.makeJwt('admin')
        this.projectCode = 'project-for-projects-route'
        this.projectDetails = {
            projectCode: this.projectCode,
            name: 'project for API "shape" tests',
            description: 'sample project for verifying the "shape" of the API results'
        }
        this.api = api.extend({
            throwHttpErrors: false,
            headers: {authorization: `Bearer ${this.adminToken}`},
        })
        this.projectUrl = `projects/${this.projectCode}`
    })

    after('clean up just-created project', async function() {
        await this.api.delete(this.projectUrl)
    })

    it('POST creates a project', async function() {
        const result = await this.api.post('projects', {
            json: {...this.projectDetails, name: 'initial-name'}
        })
        // Returns 201 CREATED, with a Content-Location header pointing to the permanent URL of the newly-created project
        expect(result.statusCode).to.equal(201)
        expect(result.headers).to.contain.keys('content-location')
        expect(result.headers['content-location']).to.contain(this.projectUrl)
        const result2 = await this.api(this.projectUrl)
        expect(result2.statusCode).to.equal(200)
        expect(result2.body).to.contain.keys('name')
        expect(result2.body.name).to.equal('initial-name')
    })

    it('POST can update a project that already exists', async function() {
        const result = await this.api.post('projects', {
            json: this.projectDetails
        })
        const result2 = await this.api(this.projectUrl)
        expect(result2.statusCode).to.equal(200)
        expect(result2.body).to.contain.keys('name')
        expect(result2.body.name).to.equal(this.projectDetails.name)
    })

    it('POST /projects requires an authenticated user', async function() {
        const result = await this.api.post('projects', {
            json: {...this.projectDetails, name: 'name-changed-by-unauth-user'},
            headers: {authorization: undefined}
        })
        expect(result.statusCode).to.equal(401)
        // Name has not been changed
        const result2 = await this.api(this.projectUrl)
        expect(result2.statusCode).to.equal(200)
        expect(result2.body).to.contain.keys('name')
        expect(result2.body.name).to.equal(this.projectDetails.name)
    })

    it('POST /projects when the project already exists requires a project manager', async function() {
        const user1Token = loginUtils.makeJwt('user1')
        const result = await this.api.post('projects', {
            json: {...this.projectDetails, name: 'name-changed-by-non-manager'},
            headers: {authorization: `Bearer ${user1Token}`}
        })
        expect(result.statusCode).to.equal(403)
        // Name has not been changed
        const result2 = await this.api(this.projectUrl)
        expect(result2.statusCode).to.equal(200)
        expect(result2.body).to.contain.keys('name')
        expect(result2.body.name).to.equal(this.projectDetails.name)
    })

    it('GET should return an array', async function() {
        // No authorization needed for the GET /projects endpoint
        const result = await this.api('projects', {headers: {authorization: undefined}})
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.be.an('array')
        expect(result.body).to.not.be.empty
        expect(result.body).to.deep.include(this.projectDetails)
    })
})

describe('/projects/[projectCode] API route', function() {
    before('set up API', function() {
        this.adminToken = loginUtils.makeJwt('admin')
        this.projectCode = 'project-for-projects-projectCode-route'
        this.projectDetails = {
            projectCode: this.projectCode,
            name: 'project for /projects/[projectCode] route',
            description: 'sample project for verifying the "shape" of the API results'
        }
        this.api = api.extend({
            throwHttpErrors: false,
            headers: {authorization: `Bearer ${this.adminToken}`},
        })
        this.projectUrl = `projects/${this.projectCode}`
    })

    after('clean up just-created project', async function() {
        await this.api.delete(this.projectUrl)
    })

    it('PUT creates a project', async function() {
        const result = await this.api.put(this.projectUrl, {
            json: this.projectDetails
        })
        expect(result.statusCode).to.equal(201)
        // No Content-Location headers returned on PUT because they're not needed: the client already knows the URL
    })

    it('PUT requires an authenticated user', async function() {
        const result = await this.api.put(this.projectUrl, {
            json: {...this.projectDetails, name: 'name-changed-by-unauth-user'},
            headers: {authorization: undefined}
        })
        expect(result.statusCode).to.equal(401)
        // Name has not been changed
        const result2 = await this.api(this.projectUrl)
        expect(result2.statusCode).to.equal(200)
        expect(result2.body).to.contain.keys('name')
        expect(result2.body.name).to.equal(this.projectDetails.name)
    })

    it('PUT when the project already exists requires a project manager', async function() {
        const user1Token = loginUtils.makeJwt('user1')
        const result = await this.api.put(this.projectUrl, {
            json: {...this.projectDetails, name: 'name-changed-by-non-manager'},
            headers: {authorization: `Bearer ${user1Token}`}
        })
        expect(result.statusCode).to.equal(403)
        // Name has not been changed
        const result2 = await this.api(this.projectUrl)
        expect(result2.statusCode).to.equal(200)
        expect(result2.body).to.contain.keys('name')
        expect(result2.body.name).to.equal(this.projectDetails.name)
    })

    it('GET returns project details including membership', async function() {
        // Expected shape:
        // {
        //     user: { username: string, firstname: string, lastname: string, language: string }
        // }
        const result = await this.api(this.projectUrl)
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.contain.keys('projectCode', 'name', 'description', 'members')
        expect(result.body).to.include(this.projectDetails)
        expect(result.body.members).to.be.an('array')
        expect(result.body.members).to.not.be.empty
        expect(result.body.members[0]).to.contain.keys('user', 'role')
        expect(result.body.members[0].user).to.contain.keys('username', 'firstname', 'lastname', 'language')
        expect(result.body.members[0].role).to.be.a('string')
    })

    it('GET requires authenticated user who is either an admin or a project manager', async function() {
        const noAuth = await this.api(this.projectUrl, {headers: {authorization: undefined}})
        expect(noAuth.statusCode).to.equal(401)
        const user1Token = loginUtils.makeJwt('user1')
        const wrongAuth = await this.api(this.projectUrl, {headers: {authorization: `Bearer ${user1Token}`}})
        expect(wrongAuth.statusCode).to.equal(403)
    })

    it('PATCH can update project details', async function() {
        const result = await this.api.patch(this.projectUrl, {json: {name: 'new-name'}})
        expect(result.statusCode).to.equal(200)
        const newDetails = await this.api(this.projectUrl)
        // Now undo the changes before we check the results, so an expectation failure here won't cause further tests to fail
        await this.api.patch(this.projectUrl, {json: {name: this.projectDetails.name}})
        expect(newDetails.statusCode).to.equal(200)
        expect(newDetails.body).to.contain.keys('projectCode', 'name', 'description', 'members')
        expect(newDetails.body).to.include({...this.projectDetails, name: 'new-name'})
    })

    // PATCH can also update membership lists, but that is thoroughly tested in test/members.js so we won't duplicate those tests here

    it('PATCH requires authenticated user who is either an admin or a project manager', async function() {
        const noAuth = await this.api.patch(this.projectUrl, {headers: {authorization: undefined}, json: {}})
        expect(noAuth.statusCode).to.equal(401)
        const user1Token = loginUtils.makeJwt('user1')
        const wrongAuth = await this.api.patch(this.projectUrl, {headers: {authorization: `Bearer ${user1Token}`}, json: {}})
        expect(wrongAuth.statusCode).to.equal(403)
    })

    it('DELETE should delete the project', async function() {
        const result = await this.api.delete(this.projectUrl)
        expect(result.statusCode).to.equal(204)
        expect(result.body).to.be.empty
        const result2 = await this.api(this.projectUrl)
        expect(result2.statusCode).to.equal(404)
        const projectList = await this.api('projects')
        expect(projectList.statusCode).to.equal(200)
        expect(projectList.body).to.not.deep.include(this.projectDetails)
    })
})
