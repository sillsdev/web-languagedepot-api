// Pass an argument through a list of pure functions, in order

import { renameKey } from './renameKey';
import { withoutKey } from './withoutKey';

// Example:
// const transforms = [
//     withoutKey('hashed_password'),
//     renameKey('login', 'username')
// ];
// applyAll(userRecord, transforms);
// // Or:
// applyAll(userRecord, withoutKey('hashed_password'), renameKey('login', 'username'));
const applyAll = (arg, ...fns) => {
    // Allow passing a single array
    fns = fns.length === 1 && Array.isArray(fns[0]) ? fns[0] : fns;
    return fns.reduce((val, fn) => fn(val), arg);
}

export { applyAll };
