import { apiv2 as api } from './testsetup.js'
import { expect } from 'chai'
import { makeJwt } from './loginUtils.js'

describe('/users API route', function() {
    before('set up API', function() {
        this.adminToken = makeJwt('admin')
        this.testUser = {
            username: 'newTestUser',
            firstname: 'New',
            lastname: 'User',
            language: 'en',
            password: 'x',
            admin: false,
        }
        this.api = api.extend({
            throwHttpErrors: false,
        })
        this.asAdmin = this.api.extend({
            headers: {authorization: `Bearer ${this.adminToken}`},
        })
        // Ensure user doesn't exist at start of test
        return this.asAdmin.delete(`users/${this.testUser.username}`)
    })

    after('clean up just-created user', async function() {
        await this.asAdmin.delete(`users/${this.testUser.username}`)
    })

    it('GET returns list of all users', async function() {
        const result = await this.asAdmin('users')
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.be.an('array')
        expect(result.body).to.not.be.empty
    })

    it('Before a user account exists, that user cannot log in', async function() {
        const { username, password } = this.testUser
        const result = await this.api('login', {username, password})
        expect(result.statusCode).to.equal(403)
    })

    it('POST creates a new user', async function() {
        const postResult = await this.asAdmin.post('users', {json: this.testUser})
        const expected = {...this.testUser}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (postResult && postResult.body != undefined && typeof postResult.body.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            postResult.body.admin = !! postResult.body.admin
        }
        expect(postResult.statusCode).to.equal(201)
        expect(postResult.body).not.to.contain.keys('password')
        expect(postResult.body).to.deep.equal(expected)
    })

    it('Once a user account exists, that user can log in', async function() {
        const { username, password } = this.testUser
        const result = await this.api('login', {username, password})
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.contain.keys('access_token')
    })

    it('POST can update a user', async function() {
        const postResult = await this.asAdmin.post('users', {json: {...this.testUser, firstname: 'new-name'}})
        const expected = {...this.testUser, firstname: 'new-name'}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (postResult && postResult.body != undefined && typeof postResult.body.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            postResult.body.admin = !! postResult.body.admin
        }
        expect(postResult.statusCode).to.equal(200)
        expect(postResult.body).not.to.contain.keys('password')
        expect(postResult.body).to.deep.equal(expected)
    })

    it('POST can change a user\'s password', async function() {
        const postResult = await this.asAdmin.post('users', {json: {...this.testUser, password: 'y'}})
        const expected = {...this.testUser}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (postResult && postResult.body != undefined && typeof postResult.body.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            postResult.body.admin = !! postResult.body.admin
        }
        expect(postResult.statusCode).to.equal(200)
        expect(postResult.body).not.to.contain.keys('password')
        expect(postResult.body).to.deep.equal(expected)
    })

    it('After a user\'s password is changed, cannot log in with old password', async function() {
        const { username, password } = this.testUser
        const result = await this.api('login', {username, password})
        expect(result.statusCode).to.equal(403)
    })

    it('After a user\'s password is changed, can log in with new password', async function() {
        const { username } = this.testUser
        const result = await this.api('login', {username, password: 'y'})
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.contain.keys('access_token')
    })
})

describe('/users/[username] API route', function() {
    before('set up API', function() {
        this.adminToken = makeJwt('admin')
        this.projectCode = 'project-for-users-username-route'
        this.projectDetails = {
            projectCode: this.projectCode,
            name: 'project for testing /users route',
            description: 'sample project for verifying the "shape" of the API results'
        }
        this.testUser = {
            username: 'newTestUser2',
            firstname: 'New2',
            lastname: 'User',
            language: 'en',
            password: 'x',
            admin: false,
        }
        this.api = api.extend({
            throwHttpErrors: false,
        })
        this.asAdmin = this.api.extend({
            headers: {authorization: `Bearer ${this.adminToken}`},
        })
        this.userUrl = `users/${this.testUser.username}`
        // Ensure user doesn't exist at start of test
        return this.asAdmin.delete(this.userUrl)
    })

    after('clean up just-created user', async function() {
        await this.asAdmin.delete(this.userUrl)
    })

    it('GET returns 404 when user does not exist', async function() {
        const result = await this.asAdmin(this.userUrl)
        expect(result.statusCode).to.equal(404)
    })

    it('Before a user account exists, that user cannot log in', async function() {
        const { username, password } = this.testUser
        const result = await this.api('login', {username, password})
        expect(result.statusCode).to.equal(403)
    })

    it('PUT creates a new user', async function() {
        const putResult = await this.asAdmin.put(this.userUrl, {json: this.testUser})
        const expected = {...this.testUser}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (putResult && putResult.body != undefined && typeof putResult.body.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            putResult.body.admin = !! putResult.body.admin
        }
        expect(putResult.statusCode).to.equal(201)
        expect(putResult.body).not.to.contain.keys('password')
        expect(putResult.body).to.deep.equal(expected)
    })

    it('GET returns user details', async function() {
        const result = await this.asAdmin(this.userUrl)
        const expected = {...this.testUser}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (result && result.body != undefined && typeof result.body.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            result.body.admin = !! result.body.admin
        }
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.contain.keys('username', 'firstname', 'lastname', 'language')
        expect(result.body).to.deep.equal(expected)
    })

    it('GET requires authentication as either admin or as the user in question', async function() {
        const userToken = makeJwt(this.testUser.username)
        const result = await this.api(this.userUrl, {headers: {authorization: `Bearer ${userToken}`}})
        expect(result.statusCode).to.equal(200)
        const unAuthResult = await this.api(this.userUrl)
        expect(unAuthResult.statusCode).to.equal(401)
        const manager1Token = makeJwt('manager1')
        const wrongAuthResult = await this.api(this.userUrl, {headers: {authorization: `Bearer ${manager1Token}`}})
        expect(wrongAuthResult.statusCode).to.equal(403)
    })

    it('HEAD with user that exists returns 200', async function() {
        const result = await this.api.head(this.userUrl)
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.be.empty
    })

    it('HEAD with user that does not exist returns 404', async function() {
        const result = await this.api.head('users/non_existent_user')
        expect(result.statusCode).to.equal(404)
        expect(result.body).to.be.empty
    })

    it('Once a user account exists, that user can log in', async function() {
        const { username, password } = this.testUser
        const result = await this.api('login', {username, password})
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.contain.keys('access_token')
    })

    it('PUT can update a user', async function() {
        const putResult = await this.asAdmin.put(this.userUrl, {json: {...this.testUser, firstname: 'new-name'}})
        const expected = {...this.testUser, firstname: 'new-name'}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (putResult && putResult.body != undefined && typeof putResult.body.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            putResult.body.admin = !! putResult.body.admin
        }
        expect(putResult.statusCode).to.equal(200)
        expect(putResult.body).not.to.contain.keys('password')
        expect(putResult.body).to.deep.equal(expected)
    })

    it('PUT requires authentication as admin or as the user in question', async function() {
        const userToken = makeJwt(this.testUser.username)
        const result = await this.api.put(this.userUrl, {json: {...this.testUser, firstname: 'new-name'}, headers: {authorization: `Bearer ${userToken}`}})
        expect(result.statusCode).to.equal(200)
        const unAuthResult = await this.api.put(this.userUrl, {json: {...this.testUser, firstname: 'new-name'}})
        expect(unAuthResult.statusCode).to.equal(401)
        const manager1Token = makeJwt('manager1')
        const wrongAuthResult = await this.api.put(this.userUrl, {json: {...this.testUser, firstname: 'new-name'}, headers: {authorization: `Bearer ${manager1Token}`}})
        expect(wrongAuthResult.statusCode).to.equal(403)
    })

    it('PUT can change a user\'s password', async function() {
        const postResult = await this.asAdmin.put(this.userUrl, {json: {...this.testUser, password: 'y'}})
        const expected = {...this.testUser}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (postResult && postResult.body != undefined && typeof postResult.body.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            postResult.body.admin = !! postResult.body.admin
        }
        expect(postResult.statusCode).to.equal(200)
        expect(postResult.body).not.to.contain.keys('password')
        expect(postResult.body).to.deep.equal(expected)
    })

    it('After a user\'s password is changed with PUT, cannot log in with old password', async function() {
        const { username, password } = this.testUser
        const result = await this.api('login', {username, password})
        expect(result.statusCode).to.equal(403)
    })

    it('After a user\'s password is changed with PUT, can log in with new password', async function() {
        const { username } = this.testUser
        const result = await this.api('login', {username, password: 'y'})
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.contain.keys('access_token')
    })

    it('PATCH can update a user', async function() {
        const putResult = await this.asAdmin.patch(this.userUrl, {json: {firstname: 'new-name'}})
        const expected = {...this.testUser, firstname: 'new-name'}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (putResult && putResult.body != undefined && typeof putResult.body.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            putResult.body.admin = !! putResult.body.admin
        }
        expect(putResult.statusCode).to.equal(200)
        expect(putResult.body).not.to.contain.keys('password')
        expect(putResult.body).to.deep.equal(expected)
    })

    it('PATCH requires authentication as admin or as the user in question', async function() {
        const userToken = makeJwt(this.testUser.username)
        const result = await this.api.patch(this.userUrl, {json: {firstname: 'new-name'}, headers: {authorization: `Bearer ${userToken}`}})
        expect(result.statusCode).to.equal(200)
        const unAuthResult = await this.api.patch(this.userUrl, {json: {firstname: 'new-name'}})
        expect(unAuthResult.statusCode).to.equal(401)
        const manager1Token = makeJwt('manager1')
        const wrongAuthResult = await this.api.patch(this.userUrl, {json: {firstname: 'new-name'}, headers: {authorization: `Bearer ${manager1Token}`}})
        expect(wrongAuthResult.statusCode).to.equal(403)
    })

    it('PATCH can change a user\'s password', async function() {
        const postResult = await this.asAdmin.patch(this.userUrl, {json: {password: 'z'}})
        const expected = {...this.testUser, firstname: 'new-name'}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (postResult && postResult.body != undefined && typeof postResult.body.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            postResult.body.admin = !! postResult.body.admin
        }
        expect(postResult.statusCode).to.equal(200)
        expect(postResult.body).not.to.contain.keys('password')
        expect(postResult.body).to.deep.equal(expected)
    })

    it('After a user\'s password is changed with PATCH, cannot log in with old password', async function() {
        const { username, password } = this.testUser
        const result = await this.api('login', {username, password})
        expect(result.statusCode).to.equal(403)
        const result2 = await this.api('login', {username, password: 'y'})
        expect(result2.statusCode).to.equal(403)
    })

    it('After a user\'s password is changed with PATCH, can log in with new password', async function() {
        const { username } = this.testUser
        const result = await this.api('login', {username, password: 'z'})
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.contain.keys('access_token')
    })

    it('DELETE requires authentication as admin or as the user in question', async function() {
        const unAuthResult = await this.api.delete(this.userUrl)
        expect(unAuthResult.statusCode).to.equal(401)
        const manager1Token = makeJwt('manager1')
        const wrongAuthResult = await this.api.delete(this.userUrl, {headers: {authorization: `Bearer ${manager1Token}`}})
        expect(wrongAuthResult.statusCode).to.equal(403)
        const userToken = makeJwt(this.testUser.username)
        const result = await this.api.delete(this.userUrl, {headers: {authorization: `Bearer ${userToken}`}})
        expect(result.statusCode).to.equal(204)
        const adminResult = await this.asAdmin.delete(this.userUrl)
        expect(adminResult.statusCode).to.equal(204)
    })

    it('After a user is deleted, cannot log in', async function() {
        const { username, password } = this.testUser
        const result = await this.api('login', {username, password: 'z'})
        expect(result.statusCode).to.equal(403)
    })

    it('After a user is deleted, user\'s auth tokens are no longer accepted', async function() {
        const userToken = makeJwt(this.testUser.username)
        const result = await this.api(this.userUrl, {headers: {authorization: `Bearer ${userToken}`}})
        expect(result.statusCode).to.equal(403)
    })
})

describe('/users/[username]/projects API route', function() {
    before('set up API', async function() {
        this.adminToken = makeJwt('admin')
        this.testUser = {
            username: 'newTestUser',
            firstname: 'New',
            lastname: 'User',
            language: 'en',
            password: 'x',
            admin: false,
        }
        this.testUserToken = makeJwt(this.testUser.username)

        this.projectCode = 'project-for-projects-route'
        this.projectDetails = {
            projectCode: this.projectCode,
            name: 'project for testing /projects route',
            description: 'sample project for verifying the "shape" of the API results'
        }
        this.api = api.extend({
            throwHttpErrors: false,
        })
        this.asAdmin = this.api.extend({
            headers: {authorization: `Bearer ${this.adminToken}`},
        })
        this.asUser = this.api.extend({
            headers: {authorization: `Bearer ${this.testUserToken}`},
        })
        this.projectUrl = `projects/${this.projectCode}`
        this.userUrl = `users/${this.testUser.username}`
        this.userProjectsUrl = `${this.userUrl}/projects`
        this.userProjectsWithRoleUrl = `${this.userUrl}/projects/withRole`

        // Create user first, then create project with user as manager
        await this.asAdmin.put(this.userUrl, {json: this.testUser})
        return this.asUser.put(this.projectUrl, {json: this.projectDetails})
    })

    after('clean up user and project', async function() {
        await this.asAdmin.delete(this.userUrl)
        return this.asAdmin.delete(this.projectUrl)
    })

    it('GET returns list of projects user is a member of, along with roles', async function() {
        const result = await this.asAdmin(this.userProjectsUrl)
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.be.an('array')
        expect(result.body).to.not.be.empty
        expect(result.body[0]).to.have.keys('projectCode', 'name', 'role')
        expect(result.body).to.deep.include({
            projectCode: this.projectDetails.projectCode,
            name: this.projectDetails.name,
            role: 'Manager'
        })
    })

    it('GET requires authentication as either admin or as the user in question', async function() {
        const userToken = makeJwt(this.testUser.username)
        const result = await this.api(this.userProjectsUrl, {headers: {authorization: `Bearer ${userToken}`}})
        expect(result.statusCode).to.equal(200)
        const unAuthResult = await this.api(this.userProjectsUrl)
        expect(unAuthResult.statusCode).to.equal(401)
        const manager1Token = makeJwt('manager1')
        const wrongAuthResult = await this.api(this.userProjectsUrl, {headers: {authorization: `Bearer ${manager1Token}`}})
        expect(wrongAuthResult.statusCode).to.equal(403)
    })

    it('GET of .../withRole/role endpoint returns list of projects user is a member of, along with roles, filtered by given role', async function() {
        const result = await this.asAdmin(`${this.userProjectsWithRoleUrl}/3`)
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.be.an('array')
        expect(result.body).to.not.be.empty
        expect(result.body[0]).to.have.keys('projectCode', 'name', 'role')
        expect(result.body).to.deep.include({
            projectCode: this.projectDetails.projectCode,
            name: this.projectDetails.name,
            role: 'Manager'
        })
    })

    for (const role of ['Manager', 3]) {
        it(`GET of .../withRole/${role} endpoint correctly filters by given role`, async function() {
            const result = await this.asAdmin(`${this.userProjectsWithRoleUrl}/${role}`)
            expect(result.statusCode).to.equal(200)
            expect(result.body).to.be.an('array')
            expect(result.body).to.not.be.empty
           expect(result.body[0]).to.have.keys('projectCode', 'name', 'role')
            expect(result.body).to.deep.include({
                projectCode: this.projectDetails.projectCode,
                name: this.projectDetails.name,
                role: 'Manager'
            })
        })

    }

    for (const role of ['Contributor', 4]) {
        it(`GET of .../withRole/${role} endpoint correctly returns nothing`, async function() {
            const result = await this.asAdmin(`${this.userProjectsWithRoleUrl}/${role}`)
            expect(result.statusCode).to.equal(200)
            expect(result.body).to.be.an('array')
            expect(result.body).to.be.empty
        })

    }

    it('GET of .../withRole/role endpoint requires authentication as either admin or as the user in question', async function() {
        const userToken = makeJwt(this.testUser.username)
        const result = await this.api(`${this.userProjectsWithRoleUrl}/3`, {headers: {authorization: `Bearer ${userToken}`}})
        expect(result.statusCode).to.equal(200)
        const unAuthResult = await this.api(`${this.userProjectsWithRoleUrl}/3`)
        expect(unAuthResult.statusCode).to.equal(401)
        const manager1Token = makeJwt('manager1')
        const wrongAuthResult = await this.api(`${this.userProjectsWithRoleUrl}/3`, {headers: {authorization: `Bearer ${manager1Token}`}})
        expect(wrongAuthResult.statusCode).to.equal(403)
    })


})

