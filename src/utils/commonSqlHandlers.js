import { sqlError, duplicateKeyError, notFound } from './commonErrors';

function catchSqlError(callback) {
    try {
        return callback();
    } catch (error) {
        return sqlError(error);
    }
}

function onlyOne(items, itemKey, itemName, callback) {
    if (items.length < 1) {
        return notFound(itemKey, itemName);
    } else if (items.length > 1) {
        return duplicateKeyError(itemKey, itemName);
    } else {
        return catchSqlError(() => callback(items[0]));
    }
}

function atMostOne(items, itemKey, itemName, ifNone, ifOne) {
    if (items.length < 1) {
        return catchSqlError(() => ifNone());
    } else if (items.length > 1) {
        return duplicateKeyError(itemKey, itemName);
    } else {
        return catchSqlError(() => ifOne(items[0]));
    }
}

export { catchSqlError, onlyOne, atMostOne };
