import { expect } from '@playwright/test'
import { test } from './fixtures.js'

function withToken(token: string) {
    return { headers: { 'Authorization': `Bearer ${token}` } }
}

function withTokenAndData(token: string, data: any) {
    return { ...withToken(token), data }
}

test.describe('/projects API route', () => {
    const projectCode = 'project-for-projects-route'
    const projectDetails = {
        projectCode,
        name: 'project for testing /projects route',
        description: 'sample project for verifying the "shape" of the API results'
    }
    const projectUrl = `projects/${projectCode}`

    test.afterAll(async ({ request, adminToken }) => {
        await request.delete(projectUrl, withToken(adminToken))
    })

    test('POST creates a project', async ({ request, adminToken }) => {
        const result = await request.post('projects', withTokenAndData(adminToken, {...projectDetails, name: 'initial-name'}))
        // Returns 201 CREATED, with a Content-Location header pointing to the permanent URL of the newly-created project
        expect(result.status()).toEqual(201)
        expect(result.headers()).toHaveProperty('content-location')
        expect(result.headers()['content-location']).toMatch(projectUrl)
        const result2 = await request.get(projectUrl, withToken(adminToken))
        await expect(result2).toBeOK()
        const json: any = await result2.json()
        expect(json).toHaveProperty('name')
        expect(json.name).toEqual('initial-name')
    })

    test('POST can update a project that already exists', async ({ request, adminToken }) => {
        const result = await request.post('projects', withTokenAndData(adminToken, projectDetails))
        await expect(result).toBeOK()
        const result2 = await request.get(projectUrl, withToken(adminToken))
        await expect(result2).toBeOK()
        const json: any = await result2.json()
        expect(json).toHaveProperty('name')
        expect(json.name).toEqual(projectDetails.name)
    })

    test('POST /projects requires an authenticated user', async ({ request, adminToken }) => {
        const result = await request.post('projects', {data: {...projectDetails, name: 'name-changed-by-unauth-user'}})  // No token
        expect(result.status()).toEqual(401)
        // Name has not been changed
        const result2 = await request.get(projectUrl, withToken(adminToken))
        await expect(result2).toBeOK()
        const json: any = await result2.json()
        expect(json).toHaveProperty('name')
        expect(json.name).toEqual(projectDetails.name)
    })

    test('POST /projects when the project already exists requires a project manager', async ({ request, makeJwt, adminToken }) => {
        const user1Token = makeJwt('user1')
        const result = await request.post('projects', withTokenAndData(user1Token, {...projectDetails, name: 'name-changed-by-non-manager'}))
        expect(result.status()).toBe(403)
        // Name has not been changed
        const result2 = await request.get(projectUrl, withToken(adminToken))
        await expect(result2).toBeOK()
        const json = await result2.json()
        expect(json).toHaveProperty('name')
        expect(json.name).toEqual(projectDetails.name)
    })

    test('GET should return an array', async ({ request, adminToken }) => {
        const result = await request.get('projects', withToken(adminToken))
        await expect(result).toBeOK()
        const json = await result.json()
        expect(Array.isArray(json)).toBeTruthy()
        expect(json).not.toHaveLength(0)
        expect(json).toContainEqual(projectDetails)
    })
})

test.describe('/projects/[projectCode] API route', () => {
    const projectCode = 'project-for-projects-projectCode-route'
    const projectDetails = {
        projectCode,
        name: 'project for testing /projects/[projectCode] route',
        description: 'sample project for verifying the "shape" of the API results'
    }
    const projectUrl = `projects/${projectCode}`

    test.afterAll(async ({ request, adminToken }) => {
        await request.delete(projectUrl, withToken(adminToken))
    })

    test('PUT creates a project', async ({ request, adminToken }) => {
        const result = await request.put(projectUrl, withTokenAndData(adminToken, projectDetails))
        expect(result.status()).toBe(201)
        // No Content-Location headers returned on PUT because they're not needed: the client already knows the URL
        expect(result.headers).not.toHaveProperty('content-location')
    })

    test('PUT requires an authenticated user', async ({ request, adminToken }) => {
        const result = await request.put(projectUrl, {data: {...projectDetails, name: 'name-changed-by-unauth-user'}})  // No token
        expect(result.status()).toBe(401)
        // Name has not been changed
        const result2 = await request.get(projectUrl, withToken(adminToken))
        await expect(result2).toBeOK()
        const json = await result2.json()
        expect(json).toHaveProperty('name')
        expect(json.name).toEqual(projectDetails.name)
    })

    test('PUT when the project already exists requires a project manager', async ({ request, adminToken, makeJwt }) => {
        const result = await request.put(projectUrl, withTokenAndData(makeJwt('user1'), {...projectDetails, name: 'name-changed-by-non-manager'}))
        expect(result.status()).toBe(403)
        // Name has not been changed
        const result2 = await request.get(projectUrl, withToken(adminToken))
        await expect(result2).toBeOK()
        const json = await result2.json()
        expect(json).toHaveProperty('name')
        expect(json.name).toEqual(projectDetails.name)
    })

    test('GET returns project details including membership', async ({ request, adminToken }) => {
        // Expected shape:
        // {
        //     user: { username: string, firstname: string, lastname: string, language: string }
        // }
        const result = await request.get(projectUrl, withToken(adminToken))
        await expect(result).toBeOK()
        const json = await result.json()
        expect(json).toHaveProperty('projectCode')
        expect(json).toHaveProperty('name')
        expect(json).toHaveProperty('description')
        expect(json).toHaveProperty('members')
        expect(json).toMatchObject(projectDetails)
        expect(Array.isArray(json.members)).toBeTruthy()
        expect(json.members).not.toHaveLength(0)
        expect(json.members[0]).toHaveProperty('user')
        expect(json.members[0]).toHaveProperty('role')
        expect(json.members[0].user).toHaveProperty('username')
        expect(json.members[0].user).toHaveProperty('firstname')
        expect(json.members[0].user).toHaveProperty('lastname')
        expect(json.members[0].user).toHaveProperty('language')
        expect(typeof json.members[0].role).toBe('string')
    })

    test('GET requires authenticated user who is either an admin or a project manager', async ({ request, adminToken, makeJwt }) => {
        const noAuth = await request.get(projectUrl)  // No token
        expect(noAuth.status()).toBe(401)
        const wrongAuth = await request.get(projectUrl, withToken(makeJwt('user1')))
        expect(wrongAuth.status()).toBe(403)
    })

    test('PATCH can update project details', async ({ request, adminToken }) => {
        const result = await request.patch(projectUrl, withTokenAndData(adminToken, {name: 'new-name'}))
        await expect(result).toBeOK()
        const newDetails = await request.get(projectUrl, withToken(adminToken))
        // Now undo the changes before we check the results, so an expectation failure here won't cause further tests to fail
        await request.patch(projectUrl, withTokenAndData(adminToken, {name: projectDetails.name}))
        await expect(newDetails).toBeOK()
        const json = await newDetails.json()
        expect(json).toHaveProperty('name')
        expect(json).toMatchObject({...projectDetails, name: 'new-name'})
        // expect(newDetails.body).toHaveProperty('projectCode', 'name', 'description', 'members')
        // expect(newDetails.body).to.include({...projectDetails, name: 'new-name'})
    })

    // PATCH can also update membership lists, but that is thoroughly tested in test/members.js so we won't duplicate those tests here

    test('PATCH requires authenticated user who is either an admin or a project manager', async ({ request, makeJwt }) => {
        const noAuth = await request.patch(projectUrl, {data: {}})  // No token
        expect(noAuth.status()).toBe(401)
        const wrongAuth = await request.patch(projectUrl, withTokenAndData(makeJwt('user1'), {}))
        expect(wrongAuth.status()).toBe(403)
    })

    test('DELETE should delete the project', async ({ request, adminToken }) => {
        const result = await request.delete(projectUrl, withToken(adminToken))
        expect(result.status()).toBe(204)
        const text = await result.text()
        expect(text).toHaveLength(0)
        const result2 = await request.get(projectUrl, withToken(adminToken))
        expect(result2.status()).toBe(404)
        const projectList = await request.get('projects', withToken(adminToken))
        await expect(projectList).toBeOK()
        const json = await projectList.json()
        expect(json).not.toContainEqual(projectDetails)  // TODO: Whoops, should fail but it won't, because of members. Need something like not.toMatchObject but on all array contents
    })
})

test.describe('/projects/[projectCode]/user/[username] API route', () => {
    const projectCode = 'project-for-projects-projectCode-user-username-route'
    const projectDetails = {
        projectCode,
        name: 'project for testing /projects/[projectCode]/user/[username] route',
        description: 'sample project for verifying the "shape" of the API results'
    }
    const projectUrl = `projects/${projectCode}`

    test.beforeAll(async ({ request, adminToken }) => {
        await request.put(projectUrl, withTokenAndData(adminToken, projectDetails))
    })

    test.afterAll(async ({ request, adminToken }) => {
        await request.delete(projectUrl, withToken(adminToken))
    })

    test('GET returns user\'s role', async ({ request, adminToken }) => {
        const result = await request.get(`${projectUrl}/user/admin`, withToken(adminToken))
        await expect(result).toBeOK()
        const json: any = await result.json()
        expect(json).toHaveProperty('user')
        expect(json).toHaveProperty('role')
        expect(json.user).toHaveProperty('username')
        expect(json.user.username).toEqual('admin')
        expect(json.role).toEqual('Manager')
    })

    test('GET for non-member returns HTTP 404', async ({ request, adminToken }) => {
        const result = await request.get(`${projectUrl}/user/user2`, withToken(adminToken))
        expect(result.status()).toBe(404)
    })

    test('POST with no body adds a new user as Contributor', async ({ request, adminToken }) => {
        const postResult = await request.post(`${projectUrl}/user/user1`, withToken(adminToken))
        expect(postResult.status()).toBe(204)
        const result = await request.get(`${projectUrl}/user/user1`, withToken(adminToken))
        await expect(result).toBeOK()
        const json: any = await result.json()
        expect(json).toHaveProperty('user')
        expect(json).toHaveProperty('role')
        expect(json.user).toHaveProperty('username')
        expect(json.user.username).toEqual('user1')
        expect(json.role).toEqual('Contributor')
    })

    test('POST can change an existing user\'s role', async ({ request, adminToken }) => {
        const postResult = await request.post(`${projectUrl}/user/user1`, withTokenAndData(adminToken, {role: 'Manager'}))
        expect(postResult.status()).toBe(204)
        const result = await request.get(`${projectUrl}/user/user1`, withToken(adminToken))
        await expect(result).toBeOK()
        const json: any = await result.json()
        expect(json).toHaveProperty('user')
        expect(json).toHaveProperty('role')
        expect(json.user).toHaveProperty('username')
        expect(json.user.username).toEqual('user1')
        expect(json.role).toEqual('Manager')
    })

    const jsonFormats = [
        {role: 'Manager'},
        {roleName: 'Manager'},
        {roleId: 'Manager'},
        'Manager',
        {role: 3},
        {roleId: 3},
        {roleName: 3},  // Yes, even though roleName is a bit of a misnomer
        3,
    ];
    for (const json of jsonFormats) {
        test(`POST accepts roles in format ${JSON.stringify(json)}`, async ({ request, adminToken }) => {
            // Setup
            await request.post(`${projectUrl}/user/user1`, withTokenAndData(adminToken, {role: 'Contributor'}))
            // Test
            const postResult = await request.post(`${projectUrl}/user/user1`, withTokenAndData(adminToken, json))
            expect(postResult.status()).toBe(204)
            const result = await request.get(`${projectUrl}/user/user1`, withToken(adminToken))
            await expect(result).toBeOK()
            const json2: any = await result.json()
            expect(json2.role).not.toEqual('Contributor')
            expect(json2.role).toEqual('Manager')
        })
    }

    test('POST to /projects/[projectCode]/user/[username]/withRole/[rolename] can also change an existing user\'s role', async ({ request, adminToken }) => {
        const postResult = await request.post(`${projectUrl}/user/user1/withRole/Contributor`, withToken(adminToken))
        expect(postResult.status()).toBe(204)
        const result = await request.get(`${projectUrl}/user/user1`, withToken(adminToken))
        await expect(result).toBeOK()
        const json: any = await result.json()
        expect(json).toHaveProperty('user')
        expect(json).toHaveProperty('role')
        expect(json.user).toHaveProperty('username')
        expect(json.user.username).toEqual('user1')
        expect(json.role).toEqual('Contributor')
    })

    test('POST to /projects/[projectCode]/user/[username]/withRole/[rolename] can accept role IDs as well as names', async ({ request, adminToken }) => {
        const postResult = await request.post(`${projectUrl}/user/user1/withRole/3`, withToken(adminToken))
        expect(postResult.status()).toBe(204)
        const result = await request.get(`${projectUrl}/user/user1`, withToken(adminToken))
        await expect(result).toBeOK()
        const json: any = await result.json()
        expect(json).toHaveProperty('user')
        expect(json).toHaveProperty('role')
        expect(json.user).toHaveProperty('username')
        expect(json.user.username).toEqual('user1')
        expect(json.role).toEqual('Manager')
    })

    test('DEL will remove a user from the project', async ({ request, adminToken }) => {
        const postResult = await request.delete(`${projectUrl}/user/user1`, withToken(adminToken))
        expect(postResult.status()).toBe(204)
        const result = await request.get(`${projectUrl}/user/user1`, withToken(adminToken))
        expect(result.status()).toBe(404)
    })
})
