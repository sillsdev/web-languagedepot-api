import { sqlError, duplicateKeyError, notFound } from './commonErrors';

function catchSqlError(callback) {
    try {
        return callback();
    } catch (error) {
        return sqlError(error);
    }
}

// TODO: Change "items" to "query" here and in atMostOne, and wrap in catchSqlError for the query as well

function onlyOne(query, itemKey, itemName, callback) {
    try {
        const items = await query;
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

function atMostOne(query, itemKey, itemName, ifNone, ifOne) {
    try {
        const items = await query;
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

export { catchSqlError, onlyOne, atMostOne };
