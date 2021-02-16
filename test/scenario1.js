const api = require('./testsetup').apiv2
const expect = require('chai').expect
const loginUtils = require('./loginUtils')

describe('Scenario 1: admin creates new project, becomes manager of project by default, can add other managers later', function() {
    before('get login token', function() {
        this.adminToken = loginUtils.makeJwt('admin')
        this.projectCode = 'project-for-scenario-1'
    })

    after('clean up just-created project', async function() {
        await api.delete(`projects/${this.projectCode}`, { headers: {authorization: `Bearer ${this.adminToken}`} })
    })

    it('step 1: create project as admin', async function() {
        const result = await api.post('projects', {
            throwHttpErrors: false,
            headers: {authorization: `Bearer ${this.adminToken}`},
            json: {
                projectCode: this.projectCode,
                name: 'project for scenario 1',
                description: 'sample project that ends up belonging to admin by default'
            }
        })
        expect(result.statusCode).to.be.within(200, 299)
    })

    it('step 2: admin should be manager of project', async function() {
        const result = await api(`projects/${this.projectCode}`, { headers: {authorization: `Bearer ${this.adminToken}`} })
        expect(result.statusCode).to.equal(200)
        expect(result.body).to.contain.keys('projectCode', 'name', 'description', 'members')
        expect(result.body.members).to.be.an('array')
        expect(result.body.members).to.not.be.empty
        expect(result.body.members[0]).to.have.keys('user', 'role')
        const adminMemberships = result.body.members.filter(m => m.user && m.user.username === 'admin')
        expect(adminMemberships).to.have.length(1)
        expect(adminMemberships[0]).to.have.keys('user', 'role')
        expect(adminMemberships[0].role).to.equal('Manager')
        const manager1Memberships = result.body.members.filter(m => m.user && m.user.username === 'manager1')
        expect(manager1Memberships).to.be.empty
    })

    it('step 3: manager1 should not have access to project', async function() {
        const manager1Token = loginUtils.makeJwt('manager1')
        const result = await api(`projects/${this.projectCode}`, { headers: {authorization: `Bearer ${manager1Token}`}, throwHttpErrors: false })
        expect(result.statusCode).to.equal(403)
    })

    it('step 4: manager1 is added to project as a manager', async function() {
        const result = await api.patch(`projects/${this.projectCode}`, {
            headers: {authorization: `Bearer ${this.adminToken}`},
            throwHttpErrors: false,
            json: {
                members: { add: { user: 'manager1', role: loginUtils.managerRoleId }}
            }
        })
        expect(result.statusCode).to.equal(200)
    })

    it('step 5: manager1 should have access to project now', async function() {
        const manager1Token = loginUtils.makeJwt('manager1')
        const result = await api(`projects/${this.projectCode}`, { headers: {authorization: `Bearer ${manager1Token}`}, throwHttpErrors: false })
        expect(result.statusCode).to.equal(200)
    })

    it('step 6: manager1 can add user1 to project as regular contributor', async function() {
        const manager1Token = loginUtils.makeJwt('manager1')
        const result = await api.patch(`projects/${this.projectCode}`, {
            headers: {authorization: `Bearer ${manager1Token}`},
            throwHttpErrors: false,
            json: {
                members: { add: { user: 'user1', role: loginUtils.contributorRoleId }}
            }
        })
        expect(result.statusCode).to.equal(200)
    })

    it('step 7: user1 can not access project membership list the way the manager can', async function() {
        const user1Token = loginUtils.makeJwt('user1')
        const result = await api(`projects/${this.projectCode}`, { headers: {authorization: `Bearer ${user1Token}`}, throwHttpErrors: false })
        expect(result.statusCode).to.equal(403)
    })

    it('step 8: user1 can not add other people to the project', async function() {
        const user1Token = loginUtils.makeJwt('user1')
        const result = await api.patch(`projects/${this.projectCode}`, {
            headers: {authorization: `Bearer ${user1Token}`},
            throwHttpErrors: false,
            json: {
                members: { add: { user: 'user2', role: loginUtils.managerRoleId }}
            }
        })
        expect(result.statusCode).to.equal(403)
    })

    it('step 9: user1 can not promote himself to manager', async function() {
        const user1Token = loginUtils.makeJwt('user1')
        const result = await api.patch(`projects/${this.projectCode}`, {
            headers: {authorization: `Bearer ${user1Token}`},
            throwHttpErrors: false,
            json: {
                members: { add: { user: 'user1', role: loginUtils.managerRoleId }}
            }
        })
        expect(result.statusCode).to.equal(403)
    })

    it('step 10: user1 appears on project membership list', async function() {
        const result = await api(`projects/${this.projectCode}`, { headers: {authorization: `Bearer ${this.adminToken}`} })
        expect(result.statusCode).to.equal(200)
        const user1Memberships = result.body.members.filter(m => m.user && m.user.username === 'user1')
        expect(user1Memberships).to.have.length(1)
        expect(user1Memberships[0]).to.have.keys('user', 'role')
        expect(user1Memberships[0].role).to.equal('Contributor')
    })

    it('step 11: user1 can remove himself from the project', async function() {
        const user1Token = loginUtils.makeJwt('user1')
        const result = await api.delete(`projects/${this.projectCode}/user/user1`, {
            headers: {authorization: `Bearer ${user1Token}`},
            throwHttpErrors: false,
            json: {
                members: { add: { user: 'user1', role: loginUtils.managerRoleId }}
            }
        })
        expect(result.statusCode).to.be.within(200, 299)
    })

    it('step 12: user1 no longer appears on project membership list', async function() {
        const result = await api(`projects/${this.projectCode}`, { headers: {authorization: `Bearer ${this.adminToken}`} })
        expect(result.statusCode).to.equal(200)
        const user1Memberships = result.body.members.filter(m => m.user && m.user.username === 'user1')
        expect(user1Memberships).to.be.empty
    })
})