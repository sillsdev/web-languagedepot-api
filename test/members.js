const api = require('./testsetup').apiv2
const expect = require('chai').expect

describe('a project\'s membership list', function() {
    before('get membership list for tests', async function() {
        const result = await api('projects/tha-food')
        this.project = result.body
    })

    it('should be an array', function() {
        expect(this.project.members).to.be.an('array')
    })

    describe('each item in the membership array', function() {        
        it('should contain user and role properties', function() {
            for (let i = 0; i < this.project.members.length; i++) {
                const member = this.project.members[i];
                expect(member).to.have.all.keys('user', 'role')
            }
        })
        
        it('should have user properties with username, firstname, lastname, language and admin properties', function() {
            for (let i = 0; i < this.project.members.length; i++) {
                const member = this.project.members[i];
                expect(member.user).to.include.all.keys('username', 'firstname', 'lastname', 'language', 'admin')
            }
        })
        
        it('should have role properties that are strings', function() {
            for (let i = 0; i < this.project.members.length; i++) {
                const member = this.project.members[i];
                expect(member.role).to.be.a('string')
            }
        })
    })
})

const validMembershipFormats = [
    { user: 'rhood', role: 'Manager' },
    { user: 'rhood', role: { name: 'Manager' } },
    { user: 'rhood', role: { id: 3 } },
    { user: 'rhood', role: { id: 3, name: 'Manager' } },
    { username: 'rhood', role: 'Manager' },
    { username: 'rhood', role: { name: 'Manager' } },
    { username: 'rhood', role: { id: 3 } },
    { username: 'rhood', role: { id: 3, name: 'Manager' } },
    { user: { username: 'rhood' }, role: 'Manager' },
    { user: { username: 'rhood' }, role: { name: 'Manager' } },
    { user: { username: 'rhood' }, role: { id: 3 } },
    { user: { username: 'rhood' }, role: { id: 3, name: 'Manager' } },
    { user: 'rhood' },
    { user: { username: 'rhood' } },
    'rhood'
]

const invalidMembershipFormats = [
    { username: { username: 'rhood' }, role: 'Manager' },
    { users: ['rhood', 'little_john'], role: 'Manager' },
]

describe('updating a project\'s membership list', function() {
    async function checkUserExists(expectedRole) {
        const response = await api('projects/tha-food/user/rhood', {throwHttpErrors: false})
        expect(response.statusCode).to.be.within(200, 299);
        expect(response.body.role).to.equal(expectedRole)
    }

    async function checkUserDoesNotExist() {
        const response = await api('projects/tha-food/user/rhood', {throwHttpErrors: false})
        expect(response.statusCode).to.equal(404);
    }

    it('can be done by POST', async function() {
        await api.delete('projects/tha-food/user/rhood', {throwHttpErrors: false})
        const response = await api.post('projects/tha-food/user/rhood/withRole/Manager', { throwHttpErrors: false})
        expect(response.statusCode).to.be.within(200, 299);
        await checkUserExists('Manager')
    })

    it('can be done by PATCH', async function() {
        await api.delete('projects/tha-food/user/rhood', {throwHttpErrors: false})
        const response = await api.patch('projects/tha-food', {json: {members: {add: { user: 'rhood', role: {name: 'Manager'} }}}})
        expect(response.statusCode).to.be.within(200, 299);
        await checkUserExists('Manager')
    })

    for (const membership of validMembershipFormats) {            
        it('accepts multiple syntaxes', async function() {
            const addResponse = await api.patch('projects/tha-food', {json: {members: {add: membership}}, throwHttpErrors: false})
            expect(addResponse.statusCode).to.be.within(200, 299, `${JSON.stringify(membership)} was not accepted as a syntax in add operation`);
            await checkUserExists(membership.role ? 'Manager' : 'Contributor')
            const removeResponse = await api.patch('projects/tha-food', {json: {members: {remove: membership}}, throwHttpErrors: false})
            expect(removeResponse.statusCode).to.be.within(200, 299, `${JSON.stringify(membership)} was not accepted as a syntax in remove operation`);
            await checkUserDoesNotExist()
        })
    }

    for (const membership of invalidMembershipFormats) {            
        it('rejects invalid syntaxes', async function() {
            await api.delete('projects/tha-food/user/rhood', {throwHttpErrors: false})
            const addResponse = await api.patch('projects/tha-food', {json: {members: {add: membership}}, throwHttpErrors: false})
            expect(addResponse.statusCode).to.be.within(400, 599, `${JSON.stringify(membership)} was not rejected as a syntax error in add operation`);
            await checkUserDoesNotExist()
            // Now add the user with a valid syntax, then remove the user with an invalid syntax, and they should still exist in the project
            const validAddResponse = await api.patch('projects/tha-food', {json: {members: {add: {user: 'rhood', role: 'Manager' }}}, throwHttpErrors: false})
            expect(validAddResponse.statusCode).to.be.within(200, 399, `failed to add user with valid syntax during the invalid-syntax test for ${JSON.stringify(membership)}`);
            await checkUserExists('Manager')
            const removeResponse = await api.patch('projects/tha-food', {json: {members: {remove: membership}}, throwHttpErrors: false})
            expect(removeResponse.statusCode).to.be.within(400, 599, `${JSON.stringify(membership)} was not rejected as a syntax error in remove operation`);
            await checkUserExists('Manager')
        })
    }
})