import { expect } from '@playwright/test'
import { test } from './fixtures.js'

function withBasicAuth(username: string, password: string) {
    const b64 = Buffer.from(`${username}:${password}`).toString('base64')
    return { headers: { 'Authorization': `Basic ${b64}` } }
}

function withToken(token: string) {
    return { headers: { 'Authorization': `Bearer ${token}` } }
}

function withTokenAndData(token: string, data: any) {
    return { ...withToken(token), data }
}

test.describe('/users API route', function() {
    const testUser = {
        username: 'newTestUser',
        firstname: 'New',
        lastname: 'User',
        language: 'en',
        password: 'x',
        admin: false,
    }

    test.beforeAll(async ({ request, adminToken }) => {
        // Ensure user doesn't exist at start of test suite
        return request.delete(`users/${testUser.username}`, withToken(adminToken));
    })

    test.afterAll(async ({ request, adminToken }) => {
        await request.delete(`users/${testUser.username}`, withToken(adminToken))
    })

    test('GET returns list of all users', async ({ request, adminToken }) => {
        const result = await request.get('users', withToken(adminToken))
        expect(result).toBeOK()
        const json = await result.json()
        expect(Array.isArray(json)).toBeTruthy()
        expect(json).not.toHaveLength(0)
    })

    test('Before a user account exists, that user cannot log in', async ({ request }) => {
        const { username, password } = testUser
        const result = await request.get('login', withBasicAuth(username, password))
        expect(result.status()).toEqual(403)
    })

    test('POST creates a new user', async ({ request, adminToken }) => {
        const postResult = await request.post('users', withTokenAndData(adminToken, testUser))
        expect(postResult.status()).toEqual(201)
        const json: any = await postResult.json()
        const expected: any = {...testUser}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (typeof json.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            json.admin = !! json.admin
        }
        expect(json).not.toHaveProperty('password')
        expect(json).toEqual(expected)
    })

    test('Once a user account exists, that user can log in', async ({ request }) => {
        const { username, password } = testUser
        const result = await request.get('login', withBasicAuth(username, password))
        expect(result).toBeOK()
        const json = await result.json()
        expect(json).toHaveProperty('access_token')
    })

    test('POST can update a user', async ({ request, adminToken }) => {
        const postResult = await request.post('users', withTokenAndData(adminToken, {...testUser, firstname: 'new-name'}))
        expect(postResult).toBeOK()
        const json: any = await postResult.json()
        const expected: any = {...testUser, firstname: 'new-name'}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (typeof json.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            json.admin = !! json.admin
        }
        expect(json).not.toHaveProperty('password')
        expect(json).toEqual(expected)
    })

    test('POST can change a user\'s password', async ({ request, adminToken }) => {
        const postResult = await request.post('users', withTokenAndData(adminToken, {...testUser, password: 'y'}))
        expect(postResult).toBeOK()
        const expected: any = {...testUser}
        const json: any = await postResult.json()
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (typeof json.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            json.admin = !! json.admin
        }
        expect(json).not.toHaveProperty('password')
        expect(json).toEqual(expected)
    })

    test('After a user\'s password is changed, cannot log in with old password', async ({ request }) => {
        const { username, password } = testUser
        const result = await request.get('login', withBasicAuth(username, password))
        expect(result.status()).toEqual(403)
    })

    test('After a user\'s password is changed, can log in with new password', async ({ request }) => {
        const { username } = testUser
        const result = await request.get('login', withBasicAuth(username, 'y'))
        expect(result).toBeOK()
        const json = await result.json()
        expect(json).toHaveProperty('access_token')
    })
})

test.describe('/users/[username] API route', function() {
    const testUser = {
        username: 'newTestUser2',
        firstname: 'New2',
        lastname: 'User',
        language: 'en',
        password: 'x',
        admin: false,
    }
    const userUrl = `users/${testUser.username}`
    test.beforeAll(async ({ request, adminToken }) => {
        // Ensure user doesn't exist at start of test
        return request.delete(userUrl, withToken(adminToken))
    })

    test.afterAll(async ({ request, adminToken }) => {
        await request.delete(userUrl, withToken(adminToken))
    })

    test('GET returns 404 when user does not exist', async ({ request, adminToken }) => {
        const result = await request.get(userUrl, withToken(adminToken))
        expect(result.status()).toEqual(404)
    })

    test('Before a user account exists, that user cannot log in', async ({ request }) => {
        const { username, password } = testUser
        const result = await request.get('login', withBasicAuth(username, password))
        expect(result.status()).toEqual(403)
    })

    test('PUT creates a new user', async ({ request, adminToken }) => {
        const putResult = await request.put(userUrl, withTokenAndData(adminToken, testUser))
        expect(putResult).toBeOK()
        const json: any = await putResult.json()
        const expected: any = {...testUser}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (typeof json.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            json.admin = !! json.admin
        }

        expect(putResult.status()).toEqual(201)
        expect(json).not.toHaveProperty('password')
        expect(json).toEqual(expected)
    })

    test('GET returns user details', async ({ request, adminToken }) => {
        const result = await request.get(userUrl, withToken(adminToken))
        expect(result).toBeOK()
        const expected: any = {...testUser}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        const json = await result.json()
        if (typeof json.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            json.admin = !! json.admin
        }
        expect(json).toHaveProperty('username')
        expect(json).toHaveProperty('firstname')
        expect(json).toHaveProperty('lastname')
        expect(json).toHaveProperty('language')
        expect(json).toEqual(expected)
    })

    test('GET requires authentication as either admin or as the user in question', async ({ request, makeJwt }) => {
        const result = await request.get(userUrl, withToken(makeJwt(testUser.username)))
        expect(result).toBeOK()
        const unAuthResult = await request.get(userUrl)
        expect(unAuthResult.status()).toEqual(401)
        const wrongAuthResult = await request.get(userUrl, withToken(makeJwt('manager1')))
        expect(wrongAuthResult.status()).toEqual(403)
    })

    test('HEAD with user that exists returns 200', async ({ request }) => {
        const result = await request.head(userUrl)
        expect(result).toBeOK()
        const text = await result.text()
        expect(text).toHaveLength(0)
    })

    test('HEAD with user that does not exist returns 404', async ({ request }) => {
        const result = await request.head('users/non_existent_user')
        expect(result.status()).toEqual(404)
        const text = await result.text()
        expect(text).toHaveLength(0)
    })

    test('Once a user account exists, that user can log in', async ({ request }) => {
        const { username, password } = testUser
        const result = await request.get('login', withBasicAuth(username, password))
        expect(result).toBeOK()
        const json = await result.json()
        expect(json).toHaveProperty('access_token')
    })

    test('PUT can update a user', async ({ request, adminToken }) => {
        const putResult = await request.put(userUrl, withTokenAndData(adminToken, {...testUser, firstname: 'new-name'}))
        expect(putResult).toBeOK()
        const json: any = await putResult.json()
        const expected: any = {...testUser, firstname: 'new-name'}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (typeof json.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            json.admin = !! json.admin
        }
        expect(json).not.toHaveProperty('password')
        expect(json).toEqual(expected)
    })

    test('PUT requires authentication as admin or as the user in question', async ({ request, makeJwt }) => {
        const result = await request.put(userUrl, withTokenAndData(makeJwt(testUser.username), {...testUser, firstname: 'new-name'}))
        expect(result).toBeOK()
        const unAuthResult = await request.put(userUrl, {data: {...testUser, firstname: 'new-name'}})
        expect(unAuthResult.status()).toEqual(401)
        const wrongAuthResult = await request.put(userUrl, withTokenAndData(makeJwt('manager1'), {...testUser, firstname: 'new-name'}))
        expect(wrongAuthResult.status()).toEqual(403)
    })

    test('PUT can change a user\'s password', async ({ request, adminToken }) => {
        const postResult = await request.put(userUrl, withTokenAndData(adminToken, {...testUser, password: 'y'}))
        expect(postResult).toBeOK()
        const json: any = await postResult.json()
        const expected: any = {...testUser}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (typeof json.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            json.admin = !! json.admin
        }
        expect(json).not.toHaveProperty('password')
        expect(json).toEqual(expected)
    })

    test('After a user\'s password is changed with PUT, cannot log in with old password', async ({ request }) => {
        const { username, password } = testUser
        const result = await request.get('login', withBasicAuth(username, password))
        expect(result.status()).toEqual(403)
    })

    test('After a user\'s password is changed with PUT, can log in with new password', async ({ request }) => {
        const { username } = testUser
        const result = await request.get('login', withBasicAuth(username, 'y'))
        expect(result).toBeOK()
        const json = await result.json()
        expect(json).toHaveProperty('access_token')
    })

    test('PATCH can update a user', async ({ request, adminToken }) => {
        const patchResult = await request.patch(userUrl, withTokenAndData(adminToken, {firstname: 'new-name'}))
        const json: any = await patchResult.json()
        const expected: any = {...testUser, firstname: 'new-name'}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (typeof json.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            json.admin = !! json.admin
        }
        expect(patchResult).toBeOK()
        expect(json).not.toHaveProperty('password')
        expect(json).toEqual(expected)
    })

    test('PATCH requires authentication as admin or as the user in question', async ({ request, makeJwt }) => {
        const result = await request.patch(userUrl, withTokenAndData(makeJwt(testUser.username), {firstname: 'new-name'}))
        expect(result).toBeOK()
        const unAuthResult = await request.patch(userUrl, {data: {firstname: 'new-name'}})
        expect(unAuthResult.status()).toEqual(401)
        const wrongAuthResult = await request.patch(userUrl, withTokenAndData(makeJwt('manager1'), {firstname: 'new-name'}))
        expect(wrongAuthResult.status()).toEqual(403)
    })

    test('PATCH can change a user\'s password', async ({ request, adminToken }) => {
        const postResult = await request.patch(userUrl, withTokenAndData(adminToken, {password: 'z'}))
        expect(postResult).toBeOK()
        const json: any = await postResult.json()
        const expected: any = {...testUser, firstname: 'new-name'}
        if (expected.password) {
            // API never returns passwords
            delete expected['password']
        }
        if (typeof json.admin === 'number') {
            // MySQL returns ints but we want booleans. TODO: Delete this at some point to ensure that both MySQL and PostgreSQL will
            // return a similar JSON shape with real booleans in it, but for now we're okay with the JSON being either an int or a bool
            json.admin = !! json.admin
        }
        expect(json).not.toHaveProperty('password')
        expect(json).toEqual(expected)
    })

    test('After a user\'s password is changed with PATCH, cannot log in with old password', async ({ request }) => {
        const { username, password } = testUser
        const result = await request.get('login', withBasicAuth(username, password))
        expect(result.status()).toEqual(403)
        const result2 = await request.get('login', withBasicAuth(username, 'y'))
        expect(result2.status()).toEqual(403)
    })

    test('After a user\'s password is changed with PATCH, can log in with new password', async ({ request }) => {
        const { username } = testUser
        const result = await request.get('login', withBasicAuth(username, 'z'))
        expect(result).toBeOK()
        const json = await result.json()
        expect(json).toHaveProperty('access_token')
    })

    test('DELETE requires authentication as admin or as the user in question', async ({ request, makeJwt, adminToken }) => {
        const unAuthResult = await request.delete(userUrl)
        expect(unAuthResult.status()).toEqual(401)
        const wrongAuthResult = await request.delete(userUrl, withToken(makeJwt('manager1')))
        expect(wrongAuthResult.status()).toEqual(403)
        const result = await request.delete(userUrl, withToken(makeJwt(testUser.username)))
        expect(result.status()).toEqual(204)
        const adminResult = await request.delete(userUrl, withToken(adminToken))
        expect(adminResult.status()).toEqual(204)
    })

    test('After a user is deleted, cannot log in', async ({ request }) => {
        const { username } = testUser
        const result = await request.get('login', withBasicAuth(username, 'z'))
        expect(result.status()).toEqual(403)
    })

    test('After a user is deleted, user\'s auth tokens are no longer accepted', async ({ request, makeJwt }) => {
        const result = await request.get(userUrl, withToken(makeJwt(testUser.username)))
        expect(result.status()).toEqual(403)
    })
})

test.describe('/users/[username]/projects API route', function() {
    const testUser = {
        username: 'newTestUser',
        firstname: 'New',
        lastname: 'User',
        language: 'en',
        password: 'x',
        admin: false,
    }
    let testUserToken
    const projectCode = 'project-for-projects-route'
    const projectDetails = {
        projectCode: projectCode,
        name: 'project for testing /projects route',
        description: 'sample project for verifying the "shape" of the API results'
    }
    const projectUrl = `projects/${projectCode}`
    const userUrl = `users/${testUser.username}`
    const userProjectsUrl = `${userUrl}/projects`
    const userProjectsWithRoleUrl = `${userUrl}/projects/withRole`
    
    test.beforeAll(async ({ request, makeJwt, adminToken }) => {
        testUserToken = makeJwt(testUser.username)

        // Create user first, then create project with user as manager
        await request.put(userUrl, withTokenAndData(adminToken, testUser))
        return request.put(projectUrl, withTokenAndData(testUserToken, projectDetails))
    })

    test.afterAll(async ({ request, adminToken }) => {
        await request.delete(userUrl, withToken(adminToken))
        return request.delete(projectUrl, withToken(adminToken))
    })

    test('GET returns list of projects user is a member of, along with roles', async ({ request, adminToken }) => {
        const result = await request.get(userProjectsUrl, withToken(adminToken))
        expect(result).toBeOK()
        const json = await result.json()
        expect(Array.isArray(json)).toBeTruthy()
        expect(json).not.toHaveLength(0)
        expect(json[0]).toHaveProperty('projectCode')
        expect(json[0]).toHaveProperty('name')
        expect(json[0]).toHaveProperty('role')
        expect(json).toContainEqual({
            projectCode: projectDetails.projectCode,
            name: projectDetails.name,
            role: 'Manager'
        })
    })

    test('GET requires authentication as either admin or as the user in question', async ({ request, makeJwt }) => {
        const result = await request.get(userProjectsUrl, withToken(makeJwt(testUser.username)))
        expect(result).toBeOK()
        const unAuthResult = await request.get(userProjectsUrl)
        expect(unAuthResult.status()).toEqual(401)
        const wrongAuthResult = await request.get(userProjectsUrl, withToken(makeJwt('manager1')))
        expect(wrongAuthResult.status()).toEqual(403)
    })

    test('GET of .../withRole/role endpoint returns list of projects user is a member of, along with roles, filtered by given role', async ({ request, adminToken }) => {
        const result = await request.get(`${userProjectsWithRoleUrl}/3`, withToken(adminToken))
        expect(result).toBeOK()
        const json = await result.json()
        expect(Array.isArray(json)).toBeTruthy()
        expect(json).not.toHaveLength(0)
        expect(json[0]).toHaveProperty('projectCode')
        expect(json[0]).toHaveProperty('name')
        expect(json[0]).toHaveProperty('role')
        expect(json).toContainEqual({
            projectCode: projectDetails.projectCode,
            name: projectDetails.name,
            role: 'Manager'
        })
    })

    for (const role of ['Manager', 3]) {
        test(`GET of .../withRole/${role} endpoint correctly filters by given role`, async ({ request, adminToken }) => {
            const result = await request.get(`${userProjectsWithRoleUrl}/${role}`, withToken(adminToken))
            expect(result).toBeOK()
            const json = await result.json()
            expect(Array.isArray(json)).toBeTruthy()
            expect(json).not.toHaveLength(0)
            expect(json[0]).toHaveProperty('projectCode')
            expect(json[0]).toHaveProperty('name')
            expect(json[0]).toHaveProperty('role')
            expect(json).toContainEqual({
                projectCode: projectDetails.projectCode,
                name: projectDetails.name,
                role: 'Manager'
            })
        })

    }

    for (const role of ['Contributor', 4]) {
        test(`GET of .../withRole/${role} endpoint correctly returns nothing`, async ({ request, adminToken }) => {
            const result = await request.get(`${userProjectsWithRoleUrl}/${role}`, withToken(adminToken))
            expect(result).toBeOK()
            const json = await result.json()
            expect(Array.isArray(json)).toBeTruthy()
            expect(json).toHaveLength(0)
        })

    }

    test('GET of .../withRole/role endpoint requires authentication as either admin or as the user in question', async ({ request, makeJwt }) => {
        const result = await request.get(`${userProjectsWithRoleUrl}/3`, withToken(makeJwt(testUser.username)))
        expect(result).toBeOK()
        const unAuthResult = await request.get(`${userProjectsWithRoleUrl}/3`)
        expect(unAuthResult.status()).toEqual(401)
        const wrongAuthResult = await request.get(`${userProjectsWithRoleUrl}/3`, withToken(makeJwt('manager1')))
        expect(wrongAuthResult.status()).toEqual(403)
    })
})
