import { withoutKey } from './withoutKey';

function renameKeyImpl(oldKey: string, newKey: string, obj: Record<string, any>) {
    const result = withoutKey(oldKey, obj);
    if (Object.prototype.hasOwnProperty.call(obj, oldKey)) {
        result[newKey] = obj[oldKey];
    }
    return result;
}

const renameKey = (oldKey: string, newKey: string, obj?: Record<string, any>) => {
    if (!obj) {
        // Curried form suitable for Array.map(renameKey('foo', 'bar'))
        return (obj: Record<string, any>) => renameKeyImpl(oldKey, newKey, obj);
    } else {
        return renameKeyImpl(oldKey, newKey, obj);
    }
}

export { renameKey };
