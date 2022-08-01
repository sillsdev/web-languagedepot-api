import { expect } from '@playwright/test'
import { test } from './fixtures.js'

function withToken(token: string) {
    return { headers: { 'Authorization': `Bearer ${token}` } }
}

// import { apiv2 as api } from './testsetup.js'
// import { expect } from 'chai'
// import { makeJwt } from './loginUtils.js'

test.describe('a project\'s membership list', () => {
    let project;
    test.beforeAll(async ({ request, adminToken }) => {
        const result = await request.get('projects/tha-food', {headers: {authorization: `Bearer ${adminToken}`}})
        project = await result.json()
    })

    test('should be an array', () => {
        expect(Array.isArray(project.members)).toBeTruthy()
        expect(project.members).not.toHaveLength(0)
    })

    test.describe('each item in the membership array', () => {
        test('should contain user and role properties', () => {
            for (let i = 0; i < project.members.length; i++) {
                const member = project.members[i];
                expect(member).toHaveProperty('user')
                expect(member).toHaveProperty('role')
            }
        })

        test('should have user properties with username, firstname, lastname, language and admin properties', () => {
            for (let i = 0; i < project.members.length; i++) {
                const member = project.members[i];
                expect(member.user).toHaveProperty('username')
                expect(member.user).toHaveProperty('firstname')
                expect(member.user).toHaveProperty('lastname')
                expect(member.user).toHaveProperty('language')
                expect(member.user).toHaveProperty('admin')
            }
        })

        test('should have role properties that are strings', () => {
            for (let i = 0; i < project.members.length; i++) {
                const member = project.members[i];
                expect(typeof member.role).toBe('string')
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

test.describe('updating a project\'s membership list', () => {
    let checkUserExists: ((role: string) => Promise<void>)
    let checkUserDoesNotExist: (() => Promise<void>)
    test.beforeEach(({ request, adminToken }) => {
        checkUserExists = async (role) => {
            const response = await request.get('projects/tha-food/user/rhood', withToken(adminToken))
            await expect(response).toBeOK()
            const json = await response.json()
            expect(json.role).toEqual(role)
        }
        checkUserDoesNotExist = async () => {
            const response = await request.get('projects/tha-food/user/rhood', withToken(adminToken))
            expect(response.status()).toBe(404)
        }
    })

    test('can be done by POST', async ({ request, adminToken }) => {
        await request.delete('projects/tha-food/user/rhood', withToken(adminToken))
        const response = await request.post('projects/tha-food/user/rhood/withRole/Manager', withToken(adminToken))
        await expect(response).toBeOK()
        await checkUserExists('Manager')
    })

    test('can be done by PATCH', async ({ request, adminToken }) => {
        await request.delete('projects/tha-food/user/rhood')
        const patchOpts: any = withToken(adminToken)
        patchOpts.data = {members: {add: { user: 'rhood', role: {name: 'Manager'} }}}
        const response = await request.patch('projects/tha-food', patchOpts)
        await expect(response).toBeOK()
        // User should now exist with role Manager
        await checkUserExists('Manager')
    })

    for (const membership of validMembershipFormats) {
        test(`accepts multiple syntaxes: ${JSON.stringify(membership)}`, async ({ request, adminToken }) => {
            const addOpts: any = withToken(adminToken)
            addOpts.data = {members: {add: membership}}
            const addResponse = await request.patch('projects/tha-food', addOpts)
            await expect(addResponse, `${JSON.stringify(membership)} was not accepted as a syntax in add operation`).toBeOK()
            let expectedRole = (membership as any).role ? 'Manager' : 'Contributor'
            await checkUserExists(expectedRole)
            const removeOpts: any = withToken(adminToken)
            removeOpts.data = {members: {remove: membership}}
            const removeResponse = await request.patch('projects/tha-food', removeOpts)
            await expect(removeResponse, `${JSON.stringify(membership)} was not accepted as a syntax in remove operation`).toBeOK()
            await checkUserDoesNotExist()
        })

        if (typeof membership === 'string') {
            test('accepts backwards-compatibility removeUser syntax for strings only', async ({ request, adminToken }) => {
                await request.post(`projects/tha-food/user/rhood/withRole/Manager`, withToken(adminToken))
                await checkUserExists('Manager')
                const removeOpts: any = withToken(adminToken)
                removeOpts.data = {members: {removeUser: membership}}
                const removeResponse = await request.patch('projects/tha-food', removeOpts)
                await expect(removeResponse, `${JSON.stringify(membership)} was not accepted as a syntax in removeUser operation`).toBeOK();
                await checkUserDoesNotExist()
            })
        } else {
            test(`rejects backwards-compatibility removeUser syntax for non-string membership formats: ${JSON.stringify(membership)}`, async ({ request, adminToken }) => {
                await request.post(`projects/tha-food/user/rhood/withRole/Manager`, withToken(adminToken))
                await checkUserExists('Manager')
                const removeOpts: any = withToken(adminToken)
                removeOpts.data = {members: {removeUser: membership}}
                const removeResponse = await request.patch('projects/tha-food', removeOpts)
                await expect(removeResponse, `${JSON.stringify(membership)} was not rejected as a syntax error in removeUser operation`).not.toBeOK()
                await checkUserExists('Manager')
            })
        }
    }

    for (const membership of invalidMembershipFormats) {
        test(`rejects invalid syntaxes: ${JSON.stringify(membership)}`, async ({ request, adminToken }) => {
            await request.delete(`projects/tha-food/user/rhood`, withToken(adminToken))
            const addOpts: any = withToken(adminToken)
            addOpts.data = {members: {add: membership}}
            const addResponse = await request.patch('projects/tha-food', addOpts)
            await expect(addResponse, `${JSON.stringify(membership)} was not rejected as a syntax error in add operation`).not.toBeOK();
            await checkUserDoesNotExist()
            // Now add the user with a valid syntax, then remove the user with an invalid syntax, and they should still exist in the project
            const validAddOpts = { ...addOpts, data: {members: {add: {user: 'rhood', role: 'Manager' }}}}
            const validAddResponse = await request.patch('projects/tha-food', validAddOpts)
            await expect(validAddResponse, `failed to add user with valid syntax during the invalid-syntax test for ${JSON.stringify(membership)}`).toBeOK();
            // TODO: Also consider it okay if a 300-399 status code is returned here? Or not?
            await checkUserExists('Manager')
            const removeOpts = { ...addOpts, data: {members: {remove: membership}}}
            const removeResponse = await request.patch('projects/tha-food', removeOpts)
            await expect(removeResponse, `${JSON.stringify(membership)} was not rejected as a syntax error in remove operation`).not.toBeOK();
            await checkUserExists('Manager')
        })
    }
})
