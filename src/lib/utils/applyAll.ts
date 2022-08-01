// Pass an argument through a list of pure functions, in order

// Example:
// const transforms = [
//     withoutKey('hashed_password'),
//     renameKey('login', 'username')
// ];
// applyAll(userRecord, transforms);
// // Or:
// applyAll(userRecord, withoutKey('hashed_password'), renameKey('login', 'username'));
function applyAll<T, U>(arg: T, ...fns: any[]) {
    // Allow passing a single array
    fns = fns.length === 1 && Array.isArray(fns[0]) ? fns[0] : fns;
    return fns.reduce((val, fn) => fn(val), arg);
}

export { applyAll };
