import objection, { Model, QueryBuilder } from 'objection';
import { sqlError, duplicateKeyError, notFound } from './commonErrors';

function catchSqlError(callback: () => any) {
    try {
        return callback();
    } catch (error) {
        return sqlError(error);
    }
}

const defaultRetryOptions = { retryCount: 3, backoffExponent: 2, firstWaitMilliseconds: 500 };
const errorCodesToRetry = [
    'ER_SERVER_SHUTDOWN',
    'ER_READY',
    'ER_NORMAL_SHUTDOWN',
    'ER_GOT_SIGNAL',
    'ER_SHUTDOWN_COMPLETE',
    'ER_FORCING_CLOSE',
];
async function retryOnServerError<T extends Model, U>(query: QueryBuilder<T, U>, options = {}) {
    const { retryCount, backoffExponent, firstWaitMilliseconds } = {
        ...defaultRetryOptions,
        ...options
    };
    let result = undefined;
    let currentWaitMs = firstWaitMilliseconds;
    let mostRecentError = undefined;
    for (let count = 0; count <= retryCount; count++) {
        try {
            result = await query;
            return result;
        } catch (error) {
            if (error instanceof objection.DBError &&
                error.nativeError &&
                (error.nativeError as any).code &&
                errorCodesToRetry.includes((error.nativeError as any).code)) {
                    await new Promise(resolve => setTimeout(resolve, currentWaitMs));
                    currentWaitMs *= backoffExponent;
                    mostRecentError = error;
                    continue;
            } else {
                throw error;
            }
        }
    }
    if (mostRecentError) {
        throw mostRecentError;
    } else {
        throw new objection.DBError('Could not run query due to server errors');
    };
}

async function onlyOne<T extends Model>(query: QueryBuilder<T, T[]>, itemKey: string, itemName: string, callback: (t: T) => any) {
    try {
        const items = await retryOnServerError(query);
        if (items.length < 1) {
            return notFound(itemKey, itemName);
        } else if (items.length > 1) {
            return duplicateKeyError(itemKey, itemName);
        } else {
            return callback(items[0]);
        }
    } catch (error) {
        return sqlError(error);
    }
}

async function atMostOne<T extends Model>(query: QueryBuilder<T, T[]>, itemKey: string, itemName: string, ifNone: () => any, ifOne: (t: T) => any) {
    try {
        const items = await retryOnServerError(query);
        if (items.length < 1) {
            return ifNone();
        } else if (items.length > 1) {
            return duplicateKeyError(itemKey, itemName);
        } else {
            return ifOne(items[0]);
        }
    } catch (error) {
        return sqlError(error);
    }
}

export { catchSqlError, retryOnServerError, onlyOne, atMostOne };
