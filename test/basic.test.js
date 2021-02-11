const api = require('./testsetup').apiv2;
const expect = require('chai').expect;

describe('checking database', function() {
    it('roles should be non-empty', async () => {
        const result = await api('roles');
        expect(result.body).to.have.lengthOf(6);
    });
    
    // Duplicate names are OK
    it('roles should be non-empty', async () => {
        const result = await api('roles');
        expect(result.body).to.have.lengthOf(6);
    });
});

