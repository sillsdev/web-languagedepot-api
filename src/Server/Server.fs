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

(*
API surface done:
GET /api/project/private
GET /api/project/private/{projId}
GET /api/project
GET /api/project/{projId}
GET /api/project/exists/{projId}
GET /api/users/exists/{username}
POST /api/users/{username}/projects
POST /api/users
PUT /api/users/{username}
PATCH /api/users/{username} - change password only
POST /api/users/{username}/verify-password
POST /api/project
GET /api/count/users
GET /api/count/projects
GET /api/count/non-test-projects

API surface NOT done:
patchf "/api/project/%s" (fun projId -> bindJson<PatchProjects> (fun patchData ->
DELETE /api/project/{projId}/user/{username} - remove membership
POST /api/project/{projId}/user/{username} - add membership
DELETE /api/project/{projId}

API surface rejected:
POST /api/project/{projId}/add-user/{username}
*)
let webApp = router {
    get "/api/project/private" (fun next ctx ->
        task {
            // TODO: Verify login
            let listProjects = ctx.GetService<Model.ListProjects>()
            let! projects = listProjects true |> Async.StartAsTask
            return! json projects next ctx
        }
    )

    getf "/api/project/private/%s" (fun projId next ctx ->
        task {
            // TODO: Verify login
            let getProject = ctx.GetService<Model.GetProject>()
            let! project = getProject true projId
            match project with
            | Some project -> return! json project next ctx
            | None -> return! RequestErrors.notFound (json (Error (sprintf "Project code %s not found" projId))) next ctx
        }
    )

    get "/api/project" (fun next ctx ->
        task {
            let listProjects = ctx.GetService<Model.ListProjects>()
            let! projects = listProjects false |> Async.StartAsTask
            return! json projects next ctx
        }
    )

    getf "/api/project/%s" (fun projId next ctx ->
        task {
            let getProject = ctx.GetService<Model.GetProject>()
            let! project = getProject false projId
            match project with
            | Some project -> return! json project next ctx
            | None -> return! RequestErrors.notFound (json (Error (sprintf "Project code %s not found" projId))) next ctx
        }
    )

    // TODO: Not in real API spec. Why not? Probably need to add it
    getf "/api/users/%s" (fun login next ctx ->
        task {
            let getUser = ctx.GetService<Model.GetUser>()
            let! user = getUser login
            match user with
            | Some user -> return! json { user with HashedPassword = "***" } next ctx
            | None -> return! RequestErrors.notFound (json (Error (sprintf "Username %s not found" login))) next ctx
        }
    )

    getf "/api/project/exists/%s" (fun projId next ctx ->
        // Returns true if project exists (NOTE: This is the INVERSE of what the old API did!)
        task {
            let (Model.ProjectExists projectExists) = ctx.GetService<Model.ProjectExists>()
            let result = projectExists projId
            return! json result next ctx
        }
    )

    getf "/api/users/exists/%s" (fun login next ctx ->
        // Returns true if username exists (NOTE: This is the INVERSE of what the old API did!)
        task {
            let (Model.UserExists userExists) = ctx.GetService<Model.UserExists>()
            let result = userExists login
            return! json result next ctx
        }
    )

    get "/api/users" (fun next ctx ->
        // DEMO ONLY. Enumerates all users. TODO: Remove since it's not in real API spec
        task {
            let listUsers = ctx.GetService<Model.ListUsers>()
            let! x = listUsers() |> Async.StartAsTask
            let logins = x |> List.map (fun user -> { user with HashedPassword = "***" })
            return! json logins next ctx
        }
    )

    postf "/api/users/%s/projects" (fun login ->
        bindJson<Shared.LoginInfo> (fun loginInfo next ctx ->
            task {
                let verifyLoginInfo = ctx.GetService<Model.VerifyLoginInfo>()
                let projectsAndRolesByUser = ctx.GetService<Model.ProjectsAndRolesByUser>()
                let! goodLogin = verifyLoginInfo loginInfo |> Async.StartAsTask
                if goodLogin then
                    let! projectList = projectsAndRolesByUser login |> Async.StartAsTask
                    return! json projectList next ctx
                else
                    return! RequestErrors.forbidden (json {| status = "error"; message = "Login failed" |}) next ctx
            }
        )
    )

    patchf "/api/project/%s" (fun projId -> bindJson<PatchProjects> (fun patchData next ctx -> task {
        match patchData.addUser, patchData.removeUser with
        | Some add, Some remove ->
            return! RequestErrors.badRequest (json (Error "Specify exactly one of addUser or removeUser")) next ctx
        | Some add, None ->
            // TODO: Actually do the add
            let (Model.AddMembership addMember) = ctx.GetService<Model.AddMembership>()
            let! result = addMember add.Name projId 3 |> Async.StartAsTask  // TODO: get role in here as well
            // let result = Ok (sprintf "Added %s to %s" add.Name projId)
            // TODO: Fix up the result type (should be Result<string, string>, not just bool)
            // TODO: Do the rest
            return! json result next ctx
        | None, Some remove ->
            // TODO: Actually do the remove
            let result = Ok (sprintf "Removed %s from %s" remove.Name projId)
            return! json result next ctx
        | None, None ->
            return! RequestErrors.badRequest (json (Error "Specify exactly one of addUser or removeUser")) next ctx
    }))

    // Suggested by Chris Hirt: POST to add, DELETE to remove, no JSON body needed
    postf "/api/project/%s/user/%s" (fun (projId,username) ->
        // TODO: Actually do the add
        let result = Ok (sprintf "Added %s to %s" username projId)
        json result
    )

    deletef "/api/project/%s/user/%s" (fun (projId,username) ->
        // TODO: Actually do the remove
        let result = Ok (sprintf "Removed %s from %s" username projId)
        json result
    )

    postf "/api/users/%s/projects/withRole/%i" (fun (login, roleId) ->
        bindJson<Shared.LoginInfo> (fun logininfo next ctx ->
            task {
                let verifyLoginInfo = ctx.GetService<Model.VerifyLoginInfo>()
                let projectsAndRolesByUserRole = ctx.GetService<Model.ProjectsAndRolesByUserRole>()
                let! goodLogin = verifyLoginInfo logininfo |> Async.StartAsTask
                if goodLogin then
                    let! projectList = projectsAndRolesByUserRole login roleId |> Async.StartAsTask
                    return! json projectList next ctx
                else
                    return! RequestErrors.forbidden (json {| status = "error"; message = "Login failed" |}) next ctx
            }
        )
    )

    get "/api/roles" (fun next ctx ->
        task {
            let roleNames = ctx.GetService<Model.ListRoles>()
            let! roles = roleNames()
            return! json roles next ctx
        }
    )

    post "/api/users" (bindJson<CreateUser> (fun user next ctx ->
        task {
            let (Model.UserExists userExists) = ctx.GetService<Model.UserExists>()
            let! alreadyExists = userExists user.Login
            if alreadyExists then
                return! json {| status = "error"; message = "Username already exists; pick another one" |} next ctx
            else
                let createUser = ctx.GetService<Model.CreateUser>()
                let! newId = createUser user
                return! json newId next ctx
        }
    ))

    putf "/api/users/%s" (fun login -> bindJson<UpdateUser> (fun updateData next ctx ->
        task {
            let upsertUser = ctx.GetService<Model.UpsertUser>()
            let! newId = upsertUser login updateData
            return! json newId next ctx
        }
    ))

    patchf "/api/users/%s" (fun login -> bindJson<ChangePassword> (fun updateData next ctx ->
        task {
            let changePassword = ctx.GetService<Model.ChangePassword>()
            let! success = changePassword login updateData
            return! json success next ctx
        }
    ))

    postf "/api/users/%s/verify-password" (fun login -> bindJson<LoginInfo> (fun loginInfo next ctx ->
        task {
            let verifyLoginInfo = ctx.GetService<Model.VerifyLoginInfo>()
            let! goodLogin = verifyLoginInfo loginInfo |> Async.StartAsTask
            return! json goodLogin next ctx
            // NOTE: We don't do any work behind the scenes to reconcile MySQL and Mongo passwords; that's up to Language Forge
        }
    ))

    post "/api/project" (bindJson<CreateProject> (fun proj next ctx ->
        task {
            let (Model.ProjectExists projectExists) = ctx.GetService<Model.ProjectExists>()
            let projId = match proj.Identifier with
                         | None -> "new-project-id"  // TODO: Build from project name and check whether it exists, appending numbers if needed
                         | Some projId -> projId
            let! alreadyExists = projectExists projId
            if alreadyExists then
                return! json {| status = "error"; message = "Project code already exists; pick another one" |} next ctx
            else
                let createProject = ctx.GetService<Model.CreateProject>()
                let! newId = createProject { proj with Identifier = Some projId }
                return! json newId next ctx
        }
    ))

    get "/api/count/users" (fun next ctx ->
        task {
            do! Async.Sleep 500 // Simulate server load
            let (Model.CountUsers countUsers) = ctx.GetService<Model.CountUsers>()
            let! count = countUsers ()
            return! json count next ctx
        }
    )

    get "/api/count/projects" (fun next ctx ->
        task {
            do! Async.Sleep 750 // Simulate server load
            let (Model.CountProjects countProjects) = ctx.GetService<Model.CountProjects>()
            let! count = countProjects ()
            return! json count next ctx
        }
    )

    get "/api/count/non-test-projects" (fun next ctx ->
        task {
            do! Async.Sleep 1000 // Simulate server load
            let (Model.CountRealProjects countRealProjects) = ctx.GetService<Model.CountRealProjects>()
            let! count = countRealProjects ()
            return! json count next ctx
        }
    )

    get "/api/config" (fun next ctx ->
        task {
            let cfg = ctx |> getSettings<MySqlSettings>
            return! json cfg next ctx
        }
    )

    deletef "/api/project/%s" (fun (projId) ->
        // TODO: Verify admin password before this is allowed
        (json (Error "Not implemented; would verify admin username/password first, then delete a project (really just archive it)"))
    )
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
