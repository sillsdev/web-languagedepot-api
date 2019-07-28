open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Shared


let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let webApp = router {
    get "/api/project/private" (fun next ctx ->
        json "Would get all private projects" next ctx
    )

    getf "/api/project/private/%s" (fun projId next ctx ->
        json (sprintf "Would get private project with ID %s" projId) next ctx
    )

    get "/api/project" (fun next ctx ->
        json "Would get all public projects" next ctx
    )

    getf "/api/project/%s" (fun projId next ctx ->
        json (sprintf "Would get public project with ID %s" projId) next ctx
    )

    getf "/api/project/exists/%s" (fun projId next ctx ->
        // Would return true if project exists (NOTE: This is the INVERSE of what the old API did!)
        json true next ctx
    )

    getf "/api/users/exists/%s" (fun login next ctx ->
        // Would return true if username exists (NOTE: This is the INVERSE of what the old API did!)
        json true next ctx
    )

    postf "/api/users/%s/projects" (fun login next ctx ->
        json "Would verify password, then return list of projects user is member of (optionally filtered by role in project)" next ctx
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
    use_static publicPath
    use_json_serializer(Thoth.Json.Giraffe.ThothSerializer())
    use_gzip
}

run app
