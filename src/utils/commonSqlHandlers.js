import { sqlError, duplicateKeyError, notFound } from './commonErrors';

function catchSqlError(callback) {
    try {
        return callback();
    } catch (error) {
        return sqlError(error);
    }
}

async function onlyOne(query, itemKey, itemName, callback) {
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

async function atMostOne(query, itemKey, itemName, ifNone, ifOne) {
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

// TODO: Write a handler to help with the ER_SERVER_SHUTDOWN error, which should try again with exponential backoff up to 3 times (waiting 0.5, 1, and then 2 seconds). Error code below:
// {"error":{"name":"DBError","nativeError":{"code":"ER_SERVER_SHUTDOWN","errno":1053,"sqlState":"08S01","sqlMessage":"Server shutdown in progress"},"client":"mysql"},"description":"SQL error; see error property for details","code":"sql_error"}
//
// Basic idea is a wrapper function that will wrap around queries. So instead of `const result = await oneUserQuery(params)`,
// you would write `const result = await retryOnServerError(oneUserQuery(params), { retryCount: 3, backoffExponent: 2, firstWaitMilliseconds: 0.5 })` where the option object is, well, optional
// But it should only retry on certain kinds of errors. We don't want to retry if the error is "hey, your SQL was malformed".
//
// See https://dev.mysql.com/doc/mysql-errors/5.7/en/global-error-reference.html and https://dev.mysql.com/doc/mysql-errors/5.7/en/server-error-reference.html
// Error codes I should probably handle besides ER_SERVER_SHUTDOWN:
// ER_CANT_GET_STAT, ER_CANT_GET_WD, ER_CANT_LOCK, ER_CANT_OPEN_FILE, ER_CANT_READ_DIR, ER_CANT_SET_WD, ER_CHECKREAD, ER_ERROR_ON_READ, ER_OUTOFMEMORY, ER_OUT_OF_SORTMEMORY, ER_OUT_OF_RESOURCES
// Also ER_READY, ER_NORMAL_SHUTDOWN, ER_GOT_SIGNAL, ER_SHUTDOWN_COMPLETE, ER_FORCING_CLOSE
// Put this in an array and use Array.contains(nativeError.code) to see if it's one we should handle. Oh, and change catchSqlError to import DBError from 'db-errors' and check instanceof DBError.

export { catchSqlError, onlyOne, atMostOne };
