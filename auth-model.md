Authentication model for the API:

* Some requests do not require logging in, and can be used by anonymous users. Most will require authentication.
* Authentication is done with a JWT that identifies the Language Depot username of the autorized user.
* The JWT algorithm will be HS256 (HMAC + SHA256), which means that any application that knows the shared secret will be able to authenticate as any LD user.
* This means that we can share the secret between LD and LF, and LF will be able to create JWTs to identify as the appropriate LD user when calling the API.
* This is also future-proof: if we want better security, we can let Auth0 handle logins, and switch the API JWT verification to expect Auth0-signed JWTs rather than our own.

Future-proofing:

* If/when we develop a Redmine UI replacement that's separate from the LF UI, it can use the /api/login route to get a JWT, then submit that with all future requests.
* Requests to /api/login will come in with a username and password via HTTP Basic Auth over HTTPS.
* The Redmine UI replacement can store the user's JWT in an http-only cookie to protect it from XSS. (*NEVER* store a JWT in localStorage!)
* Then the Redmine UI backend would read the JWT out of the cookie, and send the JWT to the API. (Just like what the LF UI does right now.)
