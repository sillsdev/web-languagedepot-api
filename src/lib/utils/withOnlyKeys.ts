// Return an object possessing only certain keys (might have less keys than original).
// Usage: const jsonResult = withOnlyKeys(['username', 'firstname', 'lastname'], userRecord)
// Or const multipleRecords = records.map(withOnlyKeys(['username', 'firstname', 'lastname']))

function withOnlyKeysImpl(keyList: string[], obj: Record<string,any>) {
    const result: Record<string,any> = {};
    for (const key of keyList) {
        if (Object.prototype.hasOwnProperty.call(obj, key)) {
            result[key] = obj[key];
        }
    }
    return result;
}

const withOnlyKeys = (keyList: string[], obj?: Record<string,any>) => {
    if (!obj) {
        // Curried form suitable for Array.map(withoutKey('foo'))
        return ((obj: Record<string,any>) => withOnlyKeysImpl(keyList, obj));
    } else {
        return withOnlyKeysImpl(keyList, obj);
    }
}

export { withOnlyKeys };
