const api = require('./testsetup').apiv2;
const expect = require('chai').expect;

describe('checking database', () => {
    before(async () => {
        try {
            const result = await api('roles', { retry: 0 });
            return new Promise(resolve => resolve(result));
        } catch (error) {
            console.log('API seems to not be running; tests are probably going to fail. Try "npm run dev" in another console tab');
            throw 'API not running';
        }
    });
    
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

