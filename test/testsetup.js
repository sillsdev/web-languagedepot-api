const got = require('got');

const apiv2 = got.extend({
    prefixUrl: 'http://localhost:3000/api/v2/',
    responseType: 'json',
});

exports.apiv2 = apiv2;
