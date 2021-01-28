import { User } from '$components/models/models';
import { dbs } from '$components/models/dbsetup';
import { jsonRequired, cannotUpdateMissing, missingRequiredParam } from '$utils/commonErrors';
import { onlyOne, atMostOne } from '$utils/commonSqlHandlers';

export async function get({ params, path, query }) {
    if (!params.username) {
        return missingRequiredParam('username', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const dbQuery = User.query(db).where('login', params.username);
    return onlyOne(dbQuery, 'username', 'username', user => ({ status: 200, body: user }));
}

// TODO: Return Content-Location where appropriate

export async function head({ params, query }) {
    if (!params.username) {
        return { status: 400, body: {} };
    }
    const db = query.private ? dbs.private : dbs.public;
    try {
        const userCount = await User.query(db).where('login', params.username).resultSize();
        const status = userCount < 1 ? 404 : userCount > 1 ? 500 : 200;
        return { status, body: {} };
    } catch (error) {
        return { status: 500, body: {} };
    }
}

export async function put({ path, params, body, query }) {
    const db = query.private ? dbs.private : dbs.public;
    const trx = User.startTransaction(db);
    const dbQuery = User.query(trx).select('id').forUpdate().where('username', params.username);
    const result = await atMostOne(dbQuery, 'username', 'user code',
    async () => {
        const result = await User.query(trx).insertAndFetch(body);
        return { status: 201, body: result, headers: { location: path } };
    },
    async (user) => {
        const result = await User.query(trx).updateAndFetchById(user.id, body);
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}

export async function patch({ path, params, body, query }) {
    if (typeof body !== 'object') {
        return jsonRequired('PATCH', path);
    }
    const db = query.private ? dbs.private : dbs.public;
    const trx = User.startTransaction(db);
    const dbQuery = User.query(trx).select('id').forUpdate().where('username', params.username);
    const result = await atMostOne(dbQuery, 'username', 'user code',
    () => {
        return cannotUpdateMissing(params.projectCode, 'project');
    },
    async (user) => {
        const result = await User.query(trx).patchAndFetchById(user.id, body);
        return { status: 200, body: result };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}

export async function del({ params, query }) {
    const db = query.private ? dbs.private : dbs.public;
    const trx = User.startTransaction(db);
    const dbQuery = User.query(trx).select('id').forUpdate().where('username', params.username);
    const result = await atMostOne(dbQuery, 'username', 'user code',
    () => {
        // Deleting a non-existent item is not an error
        return { status: 204, body: {} };
    },
    async (user) => {
        await User.query(trx).deleteById(user.id);
        return { status: 204, body: {} };
    });
    if (result && result.status && result.status >= 200 && result.status < 400) {
        trx.commit();
    } else {
        trx.rollback();
    }
    return result;
}
