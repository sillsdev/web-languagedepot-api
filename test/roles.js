const api = require('./testsetup').apiv2
const expect = require('chai').expect

describe('a role', function() {
    before('get roles list for tests', async function() {
        const result = await api('roles')
        this.roles = result.body
    })

    it('should be an array', function() {
        expect(this.roles).to.be.an('array')
    })

    it('should be non-empty', function() {
        expect(this.roles).to.have.lengthOf(6)
    })

    it('should have a numeric id and a string name', function() {
        this.roles.forEach(role => {
            expect(role).to.have.all.keys('id', 'name')
            expect(role.id).to.be.a('number')
            expect(role.name).to.be.a('string')
        })
    })

    it('should have an id that is a positive number', function() {
        this.roles.forEach(role => {
            expect(role.id).to.be.greaterThan(0)
        })
    })

    it('the well-known roles should be present and correctly spelled', function() {
        expect(this.roles).to.deep.include({ id: 3, name: 'Manager' })
        expect(this.roles).not.to.deep.include({ id: 4, name: 'Contributer' }, 'Contributor is misspelled as Contributer')
        expect(this.roles).to.deep.include({ id: 4, name: 'Contributor' })
    })
})
