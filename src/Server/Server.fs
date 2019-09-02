open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2
open Giraffe
open Giraffe.HttpStatusCodeHandlers
open Saturn
open Shared
open Shared.Settings

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

let webApp = router {
    get "/api/project/private" Controller.getAllPrivateProjects
    getf "/api/project/private/%s" Controller.getPrivateProject
    get "/api/project" Controller.getAllPublicProjects
    getf "/api/project/%s" Controller.getPublicProject
    // TODO: Not in real API spec. Why not? Probably need to add it
    getf "/api/users/%s" Controller.getUser
    getf "/api/project/exists/%s" Controller.projectExists
    getf "/api/users/exists/%s" Controller.userExists
    postf "/api/users/%s/projects" Controller.projectsAndRolesByUser
    patchf "/api/project/%s" Controller.addOrRemoveUserFromProject
    // Suggested by Chris Hirt: POST to add, DELETE to remove, no JSON body needed
    postf "/api/project/%s/user/%s" Controller.addUserToProject
    deletef "/api/project/%s/user/%s" Controller.removeUserFromProject
    postf "/api/users/%s/projects/withRole/%i" Controller.projectsAndRolesByUserRole
    get "/api/roles" Controller.getAllRoles
    post "/api/users" (bindJson<CreateUser> Controller.createUser)
    putf "/api/users/%s" (fun login -> bindJson<UpdateUser> (Controller.upsertUser login))
    patchf "/api/users/%s" (fun login -> bindJson<ChangePassword> (Controller.changePassword login))
    postf "/api/users/%s/verify-password" (fun login -> bindJson<LoginInfo> (Controller.verifyPassword login))
    post "/api/project" (bindJson<CreateProject> Controller.createProject)
    get "/api/count/users" Controller.countUsers
    get "/api/count/projects" Controller.countProjects
    get "/api/count/non-test-projects" Controller.countRealProjects
    get "/api/config" Controller.getMySqlSettings
    deletef "/api/project/%s" Controller.archiveProject
    deletef "/api/project/private/%s" Controller.archivePrivateProject
    // Rejected API: POST /api/project/{projId}/add-user/{username}
}

let setupUserSecrets (context : WebHostBuilderContext) (configBuilder : IConfigurationBuilder) =
    let env = context.HostingEnvironment
    if env.IsDevelopment() || env.IsEnvironment("Testing") then
        configBuilder
            .AddJsonFile("secrets.json")
        |> ignore

let registerMySqlServices (context : WebHostBuilderContext) (svc : IServiceCollection) =
    let x = getSettingsValue<MySqlSettings> context.Configuration
    Model.ModelRegistration.registerServices svc x.ConnString

let hostConfig (builder : IWebHostBuilder) =
    builder
        .ConfigureAppConfiguration(setupUserSecrets)
        .ConfigureServices(registerMySqlServices)

let app = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router webApp
    memory_cache
    error_handler errorHandler
    use_static publicPath
    use_json_serializer(Thoth.Json.Giraffe.ThothSerializer())
    use_gzip
    host_config hostConfig
    use_config buildConfig // TODO: Get rid of this
}

run app
