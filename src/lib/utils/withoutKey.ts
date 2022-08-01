// Remove a key from an object.
// Usage: const noPassword = withoutKey('password', userRecord)
// Or const multipleRecords = records.map(withoutKey('password'))
const withoutKey = (key: string, obj?: Record<string, any>) => {
    if (!obj) {
        // Curried form suitable for Array.map(withoutKey('foo'))
        return ({[key]: _, ...result}) => result;
    } else {
        const {[key]: _, ...result} = obj;
        return result;
    }
}

export { withoutKey };
