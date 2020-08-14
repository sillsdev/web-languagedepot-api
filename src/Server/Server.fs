open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Authentication.JwtBearer
open System.Security.Claims
open Microsoft.Extensions.Hosting
open FSharp.Control.Tasks.V2
open Giraffe
open Giraffe.HttpStatusCodeHandlers
open Saturn
open Shared
open Shared.Settings
open Thoth.Json.Net
open Microsoft.IdentityModel.Tokens

// let [<Literal>] SecretApiToken = "not-a-secret"
// let [<Literal>] BearerToken = "Bearer " + SecretApiToken

// TODO: Create an API endpoint that looks this up in the database
let adminEmails = [
    "robin_munn@sil.org"
    // "robin.munn@gmail.com"
]

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let errorHandler : Giraffe.Core.ErrorHandler = fun ex logger ->
    logger.LogError(ex, "")
    // Unfortunately the Thoth library just throws generic System.Exceptions, so we have to
    // inspect the message to detect JSON parsing failures
    if ex.Message.StartsWith("Error at: `$`") then
        // JSON parsing failure
        setStatusCode 400 >=> json {| status = "error"; message = ex.Message |}
    else
        setStatusCode 500 >=> json "Internal Server Error"

let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let requireAdmin : HttpHandler = fun next ctx -> task {
    let! isAdmin =
        match ctx.User.FindFirst ClaimTypes.Email with
        | null -> task { return false }
        | claim -> Controller.emailIsAdminImpl claim.Value ctx
    if isAdmin then
        return! next ctx
    else
        return! (setStatusCode 403 >=> Controller.jsonError "Unauthorized") next ctx
        // TODO: Decide whether to return a more detailed explanation for unauthorized API requests
        // E.g., if it said "Only admins are allowed to do that", would that be an information leak?
}

let securedApp = router {
    pipe_through requireAdmin  // TODO: Only do this on a subset of the API endpoints, not all of them
    get "/api/projects/private" Controller.getAllPrivateProjects
    getf "/api/projects/private/%s" Controller.getPrivateProject
    get "/api/projects" Controller.getAllPublicProjects
    getf "/api/projects/%s" Controller.getPublicProject
    // TODO: Not in real API spec. Why not? Probably need to add it
    get "/api/users" Controller.listUsers
    get "/api/privateUsers" Controller.listUsersPrivate  // TODO: Test-only. Remove before going to production.
    getf "/api/users/limit/%i" Controller.listUsersLimit
    getf "/api/users/offset/%i" Controller.listUsersOffset
    getf "/api/users/limit/%i/offset/%i" Controller.listUsersLimitOffset
    getf "/api/users/%s" Controller.getUser  // Note this needs to come below the limit & offset endpoints so that we don't end up trying to fetch a user called "limit" or "offset"
    postf "/api/searchUsers/%s" (fun searchText -> bindJson<Api.LoginCredentials> (Controller.searchUsers searchText))
    // TODO: Change limit and offset above to be query parameters, because forbidding usernames called "limit" or "offset" would be an artificial restriction
    getf "/api/projects/exists/%s" Controller.projectExists
    getf "/api/users/exists/%s" Controller.userExists
    postf "/api/users/%s/projects" (fun username -> bindJson<Api.LoginCredentials> (Controller.projectsAndRolesByUser username))
    postf "/api/users/%s/projects/withRole/%s" (fun (username,roleName) -> bindJson<Api.LoginCredentials> (Controller.projectsAndRolesByUserRole username roleName))
    patchf "/api/projects/%s" (fun projId -> bindJson<Api.EditProjectMembershipApiCall> (Controller.addOrRemoveUserFromProject projId))
    // Suggested by Chris Hirt: POST to add, DELETE to remove, no JSON body needed
    postf "/api/projects/%s/user/%s/withRole/%s" Controller.addUserToProjectWithRole
    postf "/api/projects/%s/user/%s" Controller.addUserToProject  // Default role is "Contributer", yes, spelled with "er"
    deletef "/api/projects/%s/user/%s" Controller.removeUserFromProject
    postf "/api/users/%s/projects/withRole/%s" (fun (username,roleName) -> bindJson<Api.LoginCredentials> (Controller.projectsAndRolesByUserRole username roleName))
    post "/api/users" (bindJson<Api.CreateUser> Controller.createUser)
    putf "/api/users/%s" (fun login -> bindJson<Api.CreateUser> (Controller.upsertUser login))
    patchf "/api/users/%s" (fun login -> bindJson<Api.ChangePassword> (Controller.changePassword login))
    post "/api/verify-password" (bindJson<Api.LoginCredentials> Controller.verifyPassword)
    post "/api/projects" (bindJson<Api.CreateProject> Controller.createProject)
    deletef "/api/projects/%s" Controller.archiveProject
    deletef "/api/projects/private/%s" Controller.archivePrivateProject
}

let publicWebApp = router {
    // Backwards compatibility (old API used /api/user/{username}/projects with just the password in JSON)
    get "/api/count/users" Controller.countUsers
    get "/api/count/projects" Controller.countProjects
    get "/api/count/non-test-projects" Controller.countRealProjects
    postf "/api/user/%s/projects" (fun username -> bindJson<Api.LegacyLoginCredentials> (Controller.legacyProjectsAndRolesByUser username))
    get "/api/roles" Controller.getAllRoles
    // Rejected API: POST /api/project/{projId}/add-user/{username}
    getf "/api/isAdmin/%s" Controller.emailIsAdmin
}

let webApp = choose [ publicWebApp; securedApp ]

let setupAppConfig (context : WebHostBuilderContext) (configBuilder : IConfigurationBuilder) =
    configBuilder.AddIniFile("/etc/ldapi-server/ldapi-server.ini", optional=true, reloadOnChange=false) |> ignore
    // TODO: Find out how to catch "configuration reloaded" event and re-register MySQL services when that happens. Then set reloadOnChange=true instead

let registerMySqlServices (context : WebHostBuilderContext) (svc : IServiceCollection) =
    let x = getSettingsValue<MySqlSettings> context.Configuration
    Model.ModelRegistration.registerServices svc x.ConnString

let hostConfig (builder : IWebHostBuilder) =
    builder
        .ConfigureAppConfiguration(setupAppConfig)
        .ConfigureServices(registerMySqlServices)

let jwtConfig (options : JwtBearerOptions) =
    let tvp =
        TokenValidationParameters(
            ValidateActor = true
        )
    options.Audience <- "https://admin.languagedepot.org"
    options.Authority <- "https://dev-rmunn-ldapi.us.auth0.com"  // TODO: Move into config file
    options.RequireHttpsMetadata <- false  // FIXME: ONLY do this during development!
    options.TokenValidationParameters <- tvp

let extraJsonCoders =
    Extra.empty
    |> Extra.withInt64

let app = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router webApp
    memory_cache
    disable_diagnostics  // Don't create site.map file
    error_handler errorHandler
    use_json_serializer(Thoth.Json.Giraffe.ThothSerializer(extra=extraJsonCoders))
    use_gzip
    webhost_config hostConfig
    use_config buildConfig // TODO: Get rid of this
    use_jwt_authentication_with_config jwtConfig
}

run app
