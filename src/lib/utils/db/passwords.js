import { createHash, randomBytes } from 'crypto';

export function sha1(x) {
    const hash = createHash('sha1');
    hash.update(x);
    return hash.digest('hex');
}

export function createSalt(byteCount) {
    return randomBytes(byteCount).toString('hex');
}

// Redmine passwords were originally stored as saltless SHA1(password) hashes. Then later versions of
// Redmine realized this was insecure, and stored passwords as SHA1 of (salt + SHA1(password)). This
// ensured that the Redmine upgrade process could add a salt to all unsalted passwords without knowing
// what the old password had been. We need to be backwards-compatible with Redmine passwords, so we
// need to be able to handle either salted or unsalted hashes looked up from the users table.
export function hashRedminePassword(clearPassword, salt) {
    const hashed_password = salt ? sha1(`${salt}${sha1(clearPassword)}`) : sha1(clearPassword);
    return hashed_password;
}

export function verifyPassword(user, clearPassword) {
    if (!user) {
        return false;
    }
    const hashed_password = hashRedminePassword(clearPassword, user.salt);
    return hashed_password === user.hashed_password;
}

export function hashPasswordForStorage(clearPassword, salt) {
    salt = salt || createSalt(16);
    const hashed_password = hashRedminePassword(clearPassword, salt);
    return { hashed_password, salt };
}
