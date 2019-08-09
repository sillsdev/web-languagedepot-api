open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2
open Giraffe
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
        json [(sprintf "Would get public project with ID %s" projId)] next ctx
    )

    getf "/api/project/exists/%s" (fun projId next ctx ->
        // Returns true if project exists (NOTE: This is the INVERSE of what the old API did!)
        json (Model.projectExists projId) next ctx
    )

    getf "/api/users/exists/%s" (fun login next ctx ->
        // Returns true if username exists (NOTE: This is the INVERSE of what the old API did!)
        json (Model.userExists login) next ctx
    )

    get "/api/users" (fun next ctx ->
        // DEMO ONLY. Enumerates all users
        task {
            let! x = Model.usersQueryAsync |> Async.StartAsTask
            let logins = x |> List.map (fun user -> user.Login)
            return! json logins next ctx
        }
    )

    patchf "/api/projects/%s" (fun projId -> bindJson<PatchProjects> (function
        | Add input ->
            let result = Ok <| sprintf "Added %s to %s" input.Add.Name projId
            json result
        | Remove input ->
            let result = Ok <| sprintf "Removed %s from %s" input.Remove.Name projId
            json result
    ))

    postf "/api/users/%s/projects" (fun login ->
        bindJson<Shared.LoginInfo> (fun logininfo next ctx ->
            eprintfn "Got username %s and password %s" logininfo.username logininfo.password
            task {
                let! goodLogin =  Model.verifyLoginInfo logininfo |> Async.StartAsTask
                if goodLogin then
                    let! projectList = Model.projectsByUser login |> Async.StartAsTask
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
                    let! projectList = Model.projectsByUserRole login roleId |> Async.StartAsTask
                    return! json projectList next ctx
                else
                    return! RequestErrors.forbidden (json {| status = "error"; message = "Login failed" |}) next ctx
            }
        )
    )

    post "/api/users" (fun next ctx ->
        json "Would create new user account" next ctx
    )

    put "/api/users" (fun next ctx ->
        json "Would update existing user account" next ctx
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
