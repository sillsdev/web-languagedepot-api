open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2
open Giraffe
open Giraffe.HttpStatusCodeHandlers
open Saturn
open Shared


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
    get "/api/project/private" (fun next ctx ->
        json ["Would get all private projects"] next ctx
    )

    getf "/api/project/private/%s" (fun projId next ctx ->
        json [(sprintf "Would get private project with ID %s" projId)] next ctx
    )

    get "/api/project" (fun next ctx ->
        task {
            let! x = Model.projectsQueryAsync |> Async.StartAsTask
            let logins = x |> List.map (fun project -> project.Identifier)
            return! json logins next ctx
        }
    )

    get "/api/collections" (fun next ctx ->
        task {
            let! names = Mongo.getCollectionNames("live-sf")
            return! json (names |> List.ofSeq) next ctx
        })

    getf "/api/project/%s" (fun projId next ctx ->
        task {
            let! project = Model.getProject projId
            match project with
            | Some project -> return! json project next ctx
            | None -> return! RequestErrors.notFound (json (Error (sprintf "Project code %s not found" projId))) next ctx
        }
    )

    getf "/api/users/%s" (fun login next ctx ->
        task {
            let! user = Model.getUser login
            match user with
            | Some user -> return! json { user with HashedPassword = "***" } next ctx
            | None -> return! RequestErrors.notFound (json (Error (sprintf "Username %s not found" login))) next ctx
        }
    )

    getf "/api/project/exists/%s" (fun projId next ctx ->
        // Returns true if project exists (NOTE: This is the INVERSE of what the old API did!)
        task {
            let result = Model.projectExists projId
            return! json result next ctx
        }
    )

    getf "/api/users/exists/%s" (fun login next ctx ->
        // Returns true if username exists (NOTE: This is the INVERSE of what the old API did!)
        task {
            let result = Model.userExists login
            return! json result next ctx
        }
    )

    get "/api/users" (fun next ctx ->
        // DEMO ONLY. Enumerates all users
        task {
            let! x = Model.usersQueryAsync |> Async.StartAsTask
            let logins = x |> List.map (fun user -> user.Login)
            return! json logins next ctx
        }
    )

    patchf "/api/project/%s" (fun projId -> bindJson<PatchProjects> (fun patchData ->
        match patchData.addUser, patchData.removeUser with
        | Some add, Some remove ->
            RequestErrors.badRequest (json (Error "Specify exactly one of addUser or removeUser"))
        | Some add, None ->
            // TODO: Actually do the add
            let result = Ok (sprintf "Added %s to %s" add.Name projId)
            json result
        | None, Some remove ->
            // TODO: Actually do the remove
            let result = Ok (sprintf "Removed %s from %s" remove.Name projId)
            json result
        | None, None ->
            RequestErrors.badRequest (json (Error "Specify exactly one of addUser or removeUser"))
    ))

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

    postf "/api/users/%s/projects" (fun login ->
        bindJson<Shared.LoginInfo> (fun logininfo next ctx ->
            eprintfn "Got username %s and password %s" logininfo.username logininfo.password
            task {
                let! goodLogin =  Model.verifyLoginInfo logininfo |> Async.StartAsTask
                if goodLogin then
                    let! projectList = Model.projectsAndRolesByUser login |> Async.StartAsTask
                    return! json projectList next ctx
                else
                    return! RequestErrors.forbidden (json {| status = "error"; message = "Login failed" |}) next ctx
            }
        )
    )

    postf "/api/users/%s/projects/withRole/%i" (fun (login, roleId) ->
        bindJson<Shared.LoginInfo> (fun logininfo next ctx ->
            eprintfn "Got username %s and password %s" logininfo.username logininfo.password
            task {
                let! goodLogin =  Model.verifyLoginInfo logininfo |> Async.StartAsTask
                if goodLogin then
                    let! projectList = Model.projectsAndRolesByUserRole login roleId |> Async.StartAsTask
                    return! json projectList next ctx
                else
                    return! RequestErrors.forbidden (json {| status = "error"; message = "Login failed" |}) next ctx
            }
        )
    )

    get "/api/roles" (fun next ctx ->
        task {
            let! roles = Model.roleNames()
            return! json roles next ctx
        }
    )

    post "/api/users" (json (Error "Not implemented; would create user based on data from POST body (as JSON)"))

    putf "/api/users/%s" (fun login ->
        json (Error "Not implemented; would update existing user based on data from POST body (as JSON)")
    )

    patchf "/api/users/%s" (fun login ->
        json (Error "Not implemented; would update individual fields of user (e.g., password) based on data from POST body (as JSON)")
    )

    postf "/api/users/%s/verify-password" (fun login ->
        json (Error "Would return true or false, and do some work behind the scenes to reconcile MySQL and Mongo passwords")
    )

    post "/api/project" (bindJson<CreateProject> (fun proj next ctx ->
        task {
            let! newId = Model.createProject proj
            return! json newId next ctx
        }
    ))

    get "/api/count/users" (json (Error "Not implemented; would count total # of users"))

    get "/api/count/projects" (json (Error "Not implemented; would count total # of projects"))

    get "/api/count/non-test-projects" (json (Error "Not implemented; would count total # of projects, excluding test projects (definition of test TBD)"))
    // TODO: Determine a definition of a "test project"

    deletef "/api/project/%s" (fun (projId) ->
        // TODO: Verify admin password before this is allowed
        (json (Error "Not implemented; would verify admin username/password first, then delete a project (really just archive it)"))
    )
}

let app = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router webApp
    memory_cache
    error_handler errorHandler
    use_static publicPath
    use_json_serializer(Thoth.Json.Giraffe.ThothSerializer())
    use_gzip
}

run app
