import { withoutKey } from './withoutKey';

function renameKeyImpl(oldKey, newKey, obj) {
    const result = withoutKey(oldKey, obj);
    if (Object.prototype.hasOwnProperty.call(obj, oldKey)) {
        result[newKey] = obj[oldKey];
    }
    return result;
}

const renameKey = (oldKey, newKey, obj) => {
    if (!obj) {
        // Curried form suitable for Array.map(renameKey('foo', 'bar'))
        return obj => renameKeyImpl(oldKey, newKey, obj);
    } else {
        return renameKeyImpl(oldKey, newKey, obj);
    }
}

export { renameKey };
