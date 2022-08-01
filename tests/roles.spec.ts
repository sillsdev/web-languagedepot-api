import { expect, test } from '@playwright/test'

// import { apiv2 as api } from './testsetup.js'
// import { expect } from 'chai'

// describe('GET /api/v2/roles', function() {
test.describe('GET /api/v2/roles', () => {
    // Expected shape:
    // [
    //     ...
    //     { id: 3, name: 'Manager' },
    //     { id: 4, name: 'Contributor' },
    //     ...
    //     { id: 6, name: 'LanguageDepotProgrammer' }
    // ]

    let roles: any[] = [];

    test.beforeAll(async ({ request }) => {
        const result = await request.get('roles');
        roles = await result.json();
    });

    test('should be an array', function() {
        expect(Array.isArray(roles)).toBeTruthy();
    })

    test('should be non-empty', function() {
        expect(roles).toHaveLength(6);
    })

    test('should have a numeric id and a string name', function() {
        roles.forEach(role => {
            expect(role).toHaveProperty('id')
            expect(typeof role.id).toBe('number')
            expect(role).toHaveProperty('name')
            expect(typeof role.name).toBe('string')
        })
    })

    test('should have an id that is a positive number', function() {
        roles.forEach(role => {
            expect(role.id).toBeGreaterThan(0)
        })
    })

    test('the well-known roles should be present and correctly spelled', function() {
        expect(roles).toContainEqual({ id: 3, name: 'Manager' })
        expect(roles, 'Contributor is misspelled as Contributer').not.toContainEqual({ id: 4, name: 'Contributer' })
        expect(roles).toContainEqual({ id: 4, name: 'Contributor' })
    })
});
