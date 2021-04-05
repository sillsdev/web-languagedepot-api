// Return an object possessing only certain keys (might have less keys than original).
// Usage: const jsonResult = withOnlyKeys(['username', 'firstname', 'lastname'], userRecord)
// Or const multipleRecords = records.map(withOnlyKeys(['username', 'firstname', 'lastname']))

function withOnlyKeysImpl(keyList, obj) {
    const result = {};
    for (const key of keyList) {
        if (Object.prototype.hasOwnProperty.call(obj, key)) {
            result[key] = obj[key];
        }
    }
    return result;
}

const withOnlyKeys = (keyList, obj) => {
    if (!obj) {
        // Curried form suitable for Array.map(withoutKey('foo'))
        return (obj => withOnlyKeysImpl(keyList, obj));
    } else {
        return withOnlyKeysImpl(keyList, obj);
    }
}

export { withOnlyKeys };
