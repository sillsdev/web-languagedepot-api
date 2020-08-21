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
open Giraffe.Serialization.Json
open Saturn
open Shared
open Shared.Settings
open Thoth.Json.Net
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Http

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
        | claim -> Controller.emailIsAdminImpl true claim.Value ctx
    if isAdmin then
        return! next ctx
    else
        return! (setStatusCode 403 >=> Controller.jsonError "Unauthorized") next ctx
        // TODO: Decide whether to return a more detailed explanation for unauthorized API requests
        // E.g., if it said "Only admins are allowed to do that", would that be an information leak?
}

let head (handler : string -> HttpHandler) (next : HttpFunc) (ctx : HttpContext) =
    if HttpMethods.IsHead ctx.Request.Method then
        routef "/%s" handler next ctx
    else
        next ctx

let projectRouter isPublic = router {
    pipe_through (head (Controller.projectExists isPublic))  // Ugly, but there's no `headf` operation so we have to do it manually
    get     "/" (Controller.listProjects isPublic)
    post    "/" (Controller.createProject isPublic)
    getf    "/%s" (Controller.getProject isPublic)
    patchf  "/%s" (Controller.addOrRemoveUserFromProject isPublic)
    deletef "/%s" (Controller.archiveProject isPublic)
    postf   "/%s/user/%s/withRole/%s" (Controller.addUserToProjectWithRole isPublic)
    postf   "/%s/user/%s" (Controller.addUserToProject isPublic)  // Default role is "Contributer", yes, spelled with "er"
    deletef "/%s/user/%s" (Controller.removeUserFromProject isPublic)
    // getf    "/exists/%s" (Controller.projectExists isPublic)  // No need, can just send HEAD request to /%s instead of GET request
}

let usersRouter isPublic = router {
    pipe_through (head (Controller.userExists isPublic))
    get    "/" (Controller.listUsers isPublic)
    post   "/" (Controller.createUser isPublic)
    getf   "/%s" (Controller.getUser isPublic)
    putf   "/%s"  (Controller.upsertUser isPublic)
    patchf "/%s" (Controller.changePassword isPublic) // TODO: Allow more operations than just changing password
    // deletef "/%s" (Controller.deleteUser isPublic) // TODO: Implement
    // getf   "/exists/%s" (Controller.userExists true)  // No need, can just send HEAD request to /%s instead of GET request

    postf  "/%s/projects" (Controller.projectsAndRolesByUser isPublic)
    postf  "/%s/projects/withRole/%s" (Controller.projectsAndRolesByUserRole isPublic)
}

let securedApp = router {
    pipe_through requireAdmin  // TODO: Only do this on a subset of the API endpoints, not all of them

    forward "/api/projects" (projectRouter true)
    forward "/api/privateProjects" (projectRouter false)
    forward "/api/users" (usersRouter true)
    forward "/api/privateUsers" (usersRouter false)

    postf "/api/searchUsers/%s" (fun searchText -> bindJson<Api.LoginCredentials> (Controller.searchUsers true searchText))
    postf "/api/searchPrivateUsers/%s" (fun searchText -> bindJson<Api.LoginCredentials> (Controller.searchUsers false searchText))
    post  "/api/verify-password" (bindJson<Api.LoginCredentials> (Controller.verifyPassword true))
    post  "/api/verify-private-password" (bindJson<Api.LoginCredentials> (Controller.verifyPassword false))

    // Remove this once we're done experimenting
    postf  "/api/experimental/addRemoveUsers/%s" (Controller.addOrRemoveUserFromProject true)
    get    "/api/experimental/addRemoveUsersSample" (Controller.addOrRemoveUserFromProjectSample true)
}

let publicWebApp = router {
    // Backwards compatibility (old API used /api/user/{username}/projects with just the password in JSON)
    get "/api/count/users" (Controller.countUsers true)
    get "/api/count/projects" (Controller.countProjects true)
    get "/api/count/non-test-projects" (Controller.countRealProjects true)
    postf "/api/user/%s/projects" (fun username -> bindJson<Api.LegacyLoginCredentials> (Controller.legacyProjectsAndRolesByUser true username))
    get "/api/roles" (Controller.getAllRoles true)
    // Rejected API: POST /api/project/{projId}/add-user/{username}
    getf "/api/isAdmin/%s" (Controller.emailIsAdmin true)
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

let jsonSerializer =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))
    SystemTextJsonSerializer(options)

let app = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router webApp
    memory_cache
    disable_diagnostics  // Don't create site.map file
    error_handler errorHandler
    use_json_serializer jsonSerializer
    use_gzip
    webhost_config hostConfig
    use_config buildConfig // TODO: Get rid of this
    use_jwt_authentication_with_config jwtConfig
}

run app
