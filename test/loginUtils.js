const api = require('./testsetup').apiv2
const expect = require('chai').expect
const jwt = require('jsonwebtoken')

// Some tests will construct a JWT and make sure it's accepted by the server,
// so make sure CI passes the same signing key and audience to the tests as the server
const jwtAudience = process.env.JWT_AUDIENCE || 'https://admin.languagedepot.org/api/v2';
const signKey = process.env.JWT_SIGNING_KEY || 'not-really-secret';
function makeJwt(username) {
    var token = jwt.sign({
        sub: username,
        aud: jwtAudience,
        iat: Math.floor(Date.now() / 1000) - 5,  // Backdate 5 seconds in case of clock skew
    }, signKey, { expiresIn: '5m', algorithm: 'HS256' });
    return token;
}

function verifyToken(token) {
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

module.exports = { makeJwt, verifyToken };
