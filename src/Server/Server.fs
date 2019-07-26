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
    patchf "/api/projects/%s" (fun projId -> bindJson<PatchProjects> (function
        | Add input ->
            let result = Ok <| sprintf "Added %s to %s" input.Add.Name input.Add.Projects.Head
            json result
        | Remove input ->
            let result = Ok <| sprintf "Removed %s from %s" input.Remove.Name input.Remove.Projects.Head
            json result
    ))
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
