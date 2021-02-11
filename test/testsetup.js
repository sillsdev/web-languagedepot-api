const got = require('got');

const API_HOST = process.env.API_HOST || 'localhost:3000';
const API_SCHEME = process.env.API_SCHEME || 'http';

const apiv2 = got.extend({
    prefixUrl: `${API_SCHEME}://${API_HOST}/api/v2/`,
    responseType: 'json',
});

exports.apiv2 = apiv2;