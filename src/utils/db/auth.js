import { createHash, randomBytes } from 'crypto';
import { oneUserQuery } from './users';
import jwt from 'jsonwebtoken';

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
    return { hash_password, salt };
}

const userAndPassRegex = /^([^:]*):(.*)$/;
export function getUserAndPassFromBasicAuth(headers) {
    if (headers && headers.authorization) {
        const auth = headers.authorization;
        if (typeof auth === 'string' && auth.startsWith('Basic ')) {
            const authValue = Buffer.from(auth.slice(6), 'base64').toString();
            const userAndPass = userAndPassRegex.exec(authValue);
            if (userAndPass && userAndPass.length > 2) {
                return [userAndPass[1], userAndPass[2]];
            }
        }
    }
    return undefined;
}

// Returns a user model instance if basic auth matches, or undefined if anything fails
export async function verifyBasicAuth(db, headers) {
    const usernameAndPass = getUserAndPassFromBasicAuth(headers);
    if (usernameAndPass) {
        try {
            const users = await oneUserQuery(db, usernameAndPass[0]);
            if (users && users.length === 1) {
                if (verifyPassword(users[0], usernameAndPass[1])) {
                    return users[0];
                }
            }
        } catch (error) { }  // Suppress errors and just fail the auth
    }
    return undefined;
}

const jwtAudience = process.env.JWT_AUDIENCE || 'https://admin.languagedepot.org/api/v2'; // TODO: Should this have a default?
const signKey = process.env.JWT_SIGNING_KEY || 'not-really-secret'; // TODO: Decide whether to use env or some other method here
export function makeJwt(username) {
    var token = jwt.sign({
        sub: username,
        aud: jwtAudience,
        iat: Math.floor(Date.now() / 1000) - 5,  // Backdate 5 seconds in case of clock skew
    }, signKey, { expiresIn: '7d', algorithm: 'HS256' });
    return token;
}

// If we move to auth0-provided tokens in the future, we'll want to do something like this:
// import jwksClient from 'jwks-rsa';
// var client = jwksClient({
//   jwksUri: 'https://example.auth0.com/.well-known/jwks.json'
// });
// function getKey(header, callback){
//     client.getSigningKey(header.kid, function(err, key) {
//         var signingKey = key.publicKey || key.rsaPublicKey;
//         callback(null, signingKey);
//     });
// }
// jwt.verify(token, getKey, { audience: jwtAudience }, function(err, payload) { ... });
//
// Note that Auth0 docs say "We recommend that you cache your signing keys to improve application performance and
// avoid running into rate limits, but you will want to make sure that if decoding a token fails, you invalidate
// the cache and retrieve new signing keys before trying only one more time."

export function verifyToken(token) {
    return new Promise((resolve, reject) => {
        jwt.verify(token, signKey, { audience: jwtAudience, algorithms: ['HS256'] }, function(err, payload) {
            if (err) {
                reject(err);
            } else {
                resolve(payload);
            }
        });
    });
}

export async function verifyUsernameFromToken(token) {
    try {
        var payload = await verifyToken(token);
        if (payload.sub) {
            return payload.sub;
        }
    } catch (err) {}
}

export function getJwtTokenFromAuthHeaders(headers) {
    if (headers && headers.authorization) {
        const auth = headers.authorization;
        if (typeof auth === 'string' && auth.startsWith('Bearer ')) {
            const token = auth.slice(7);
            return token;
        }
    }
    return undefined;
}

export async function verifyJwtAuth(db, headers) {
    const token = getJwtTokenFromAuthHeaders(headers);
    if (token) {
        const username = await verifyUsernameFromToken(token);
        if (username) {
            try {
                const users = await oneUserQuery(db, username);
                if (users && users.length === 1) {
                    return users[0];
                }
            } catch (error) { }  // Suppress errors and just fail the auth
        }
    }
    return undefined;
}
