import { User } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { jsonRequired, cannotUpdateMissing, missingRequiredParam } from '$utils/commonErrors';
import { onlyOne, atMostOne, catchSqlError } from '$utils/commonSqlHandlers';

export async function get({ params, path }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    const query = User.query(dbs.public).where('login', params.username);
    return onlyOne(query, 'username', 'username', user => ({ status: 200, body: user }));
}

// TODO: Return Content-Location where appropriate

export async function head({ params }) {
    if (!params.username) {
        return { status: 400, body: {} };
    }
    try {
        const userCount = await User.query(dbs.public).count().where('login', params.username);
        const status = userCount < 1 ? 404 : userCount > 1 ? 500 : 200;
        return { status, body: {} };
    } catch (error) {
        return { status: 500, body: {} };
    }
}

export async function put({ path, params, body }) {
    console.log(`PUT /api/users/${params.username} received:`, body);
    // TODO: Transaction
    const query = User.query(dbs.public).select('id').forUpdate().where('username', params.username);
    return atMostOne(query, 'username', 'user code',
    async () => {
        const result = await User.query(dbs.public).insertAndFetch(body);
        return { status: 201, body: result, headers: { location: path } };
    },
    async (user) => {
        const result = await User.query(dbs.public).updateAndFetchById(user.id, body);
        return { status: 200, body: result };
    });
}

export async function patch({ path, params, body }) {
    console.log(`PATCH /api/users/${params.username} received:`, body);
    // TODO: Transaction
    if (typeof body !== 'object') {
        return jsonRequired('PATCH', path);
    }
    const query = User.query(dbs.public).select('id').forUpdate().where('username', params.username);
    return atMostOne(query, 'username', 'user code',
    () => {
        return cannotUpdateMissing(params.projectCode, 'project');
    },
    async (user) => {
        const result = await User.query(dbs.public).patchAndFetchById(user.id, body);
        return { status: 200, body: result };
    });
}

export async function del({ params }) {
    console.log(`DELETE /api/users/${params.username} received:`, params);
    // TODO: Transaction
    const query = User.query(dbs.public).select('id').forUpdate().where('username', params.username);
    return atMostOne(query, 'username', 'user code',
    () => {
        // Deleting a non-existent item is not an error
        return { status: 204, body: {} };
    },
    async (user) => {
        await User.query(dbs.public).deleteById(user.id);
        return { status: 204, body: {} };
    });

}
