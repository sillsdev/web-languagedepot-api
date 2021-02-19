import { oneUserQuery } from './users';
import jwt from 'jsonwebtoken';
import { verifyPassword } from './passwords';
import { retryOnServerError } from '$utils/commonSqlHandlers';

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

// Returns a user model instance if basic auth matches, undefined if there is no basic auth present, or false if anything fails (wrong username/password, etc)
export async function verifyBasicAuth(db, headers) {
    const usernameAndPass = getUserAndPassFromBasicAuth(headers);
    if (usernameAndPass) {
        try {
            const users = await retryOnServerError(oneUserQuery(db, usernameAndPass[0]));
            if (users && users.length === 1) {
                if (verifyPassword(users[0], usernameAndPass[1])) {
                    return users[0];
                }
            }
            return false;  // Username wrong, or password invalid? Either way we want to return 403
        } catch (error) {
            return undefined;  // A server error should return 401, not 403, since we don't know that we should reject this
        }
    } else {
        return undefined;  // No basic auth presented
    }
}

const jwtAudience = process.env.JWT_AUDIENCE || 'https://admin.languagedepot.org/api/v2';
const signKey = process.env.JWT_SIGNING_KEY || 'not-a-secret';
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
                const users = await retryOnServerError(oneUserQuery(db, username));
                if (users && users.length === 1) {
                    return users[0];
                } else {
                    return false;
                }
            } catch (error) {
                return undefined;  // Same as with basic auth, a server error should return 401, not 403
            }
        } else {
            return false;  // Invalid username is a 403
        }
    } else {
        return undefined;  // No JWT token is a 401
    }
}
