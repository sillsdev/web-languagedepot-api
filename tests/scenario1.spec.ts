import { expect } from '@playwright/test'
import { test } from './fixtures.js'

function withToken(token: string) {
    return { headers: { 'Authorization': `Bearer ${token}` } }
}

function withTokenAndData(token: string, data: any) {
    return { ...withToken(token), data }
}

test.describe('Scenario 1: admin creates new project, becomes manager of project by default, can add other managers later', function() {
    const projectCode = 'project-for-scenario-1'
    const managerRoleId = 3;
    const contributorRoleId = 4;

    test.afterAll(async ({ request, adminToken }) => {
        await request.delete(`projects/${projectCode}`, withToken(adminToken))
    })

    test('step 1: create project as admin', async ({ request, adminToken }) => {
        const result = await request.post('projects', withTokenAndData(adminToken, {
            projectCode,
            name: 'project for scenario 1',
            description: 'sample project that ends up belonging to admin by default'
        }))
        await expect(result).toBeOK()
    })

    test('step 2: admin should be manager of project', async ({ request, adminToken }) => {
        const result = await request.get(`projects/${projectCode}`, withToken(adminToken))
        await expect(result).toBeOK()
        const json = await result.json()
        expect(json).toHaveProperty('projectCode')
        expect(json).toHaveProperty('name')
        expect(json).toHaveProperty('description')
        expect(json).toHaveProperty('members')
        expect(Array.isArray(json.members)).toBeTruthy()
        expect(json.members).not.toHaveLength(0)
        expect(json.members[0]).toHaveProperty('user')
        expect(json.members[0]).toHaveProperty('role')
        expect(json.members[0].user).toHaveProperty('username')
        expect(json.members[0].user).toHaveProperty('firstname')
        expect(json.members[0].user).toHaveProperty('lastname')
        expect(json.members[0].user).toHaveProperty('language')
        expect(typeof json.members[0].role).toBe('string')
        const adminMemberships = json.members.filter(m => m.user && m.user.username === 'admin')
        expect(adminMemberships).toHaveLength(1)
        expect(adminMemberships[0]).toHaveProperty('user')
        expect(adminMemberships[0]).toHaveProperty('role')
        expect(adminMemberships[0].role).toEqual('Manager')
        const manager1Memberships = json.members.filter(m => m.user && m.user.username === 'manager1')
        expect(manager1Memberships).toHaveLength(0)
    })

    test('step 3: manager1 should not have access to project', async ({ request, makeJwt, adminToken }) => {
        const result = await request.get(`projects/${projectCode}`, withToken(makeJwt('manager1')))
        expect(result.status()).toEqual(403)
    })

    test('step 4: manager1 is added to project as a manager', async ({ request, adminToken }) => {
        const result = await request.patch(`projects/${projectCode}`, withTokenAndData(adminToken, {
            members: { add: { user: 'manager1', role: managerRoleId }}
        }))
        await expect(result).toBeOK()
    })

    test('step 5: manager1 should have access to project now', async ({ request, makeJwt }) => {
        const result = await request.get(`projects/${projectCode}`, withToken(makeJwt('manager1')))
        await expect(result).toBeOK()
    })

    test('step 6: manager1 can add user1 to project as regular contributor', async ({ request, makeJwt }) => {
        const result = await request.patch(`projects/${projectCode}`, withTokenAndData(makeJwt('manager1'), {
            members: { add: { user: 'user1', role: contributorRoleId }}
        }))
        await expect(result).toBeOK()
    })

    test('step 7: user1 can not access project membership list the way the manager can', async ({ request, makeJwt }) => {
        const result = await request.get(`projects/${projectCode}`, withToken(makeJwt('user1')))
        expect(result.status()).toEqual(403)
    })

    test('step 8: user1 can not add other people to the project', async ({ request, makeJwt }) => {
        const result = await request.patch(`projects/${projectCode}`, withTokenAndData(makeJwt('user1'), {
                members: { add: { user: 'user2', role: managerRoleId }}
        }))
        expect(result.status()).toEqual(403)
    })

    test('step 9: user1 can not promote himself to manager', async ({ request, makeJwt }) => {
        const result = await request.patch(`projects/${projectCode}`, withTokenAndData(makeJwt('user1'), {
            members: { add: { user: 'user1', role: managerRoleId }}
        }))
        expect(result.status()).toEqual(403)
    })

    test('step 10: user1 appears on project membership list', async ({ request, adminToken }) => {
        const result = await request.get(`projects/${projectCode}`, withToken(adminToken))
        await expect(result).toBeOK()
        const json = await result.json()
        const user1Memberships = json.members.filter(m => m.user && m.user.username === 'user1')
        expect(user1Memberships).toHaveLength(1)
        expect(user1Memberships[0]).toHaveProperty('user')
        expect(user1Memberships[0]).toHaveProperty('role')
        expect(user1Memberships[0].role).toEqual('Contributor')
    })

    test('step 11: user1 can remove himself from the project', async ({ request, makeJwt }) => {
        const result = await request.delete(`projects/${projectCode}/user/user1`, withTokenAndData(makeJwt('user1'), {
            members: { add: { user: 'user1', role: managerRoleId }}
        }))
        await expect(result).toBeOK()
    })

    test('step 12: user1 no longer appears on project membership list', async ({ request, adminToken }) => {
        const result = await request.get(`projects/${projectCode}`, withToken(adminToken))
        await expect(result).toBeOK()
        const json = await result.json()
        const user1Memberships = json.members.filter(m => m.user && m.user.username === 'user1')
        expect(user1Memberships).toHaveLength(0)
    })
})