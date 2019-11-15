module AllProjectsPage

open Browser
open Elmish
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open Fulma
open Thoth.Elmish.Toast
open Thoth.Fetch
open Thoth.Json

open TextInput
open Shared
open Shared.Api
open JsonHelpers

type Msg =
    | FindProject of string
    | ProjectsFound of JsonResult<Dto.ProjectList>
    | ProjectsAndRolesFound of JsonResult<(Dto.ProjectDetails * string list) list>
    | SingleProjectFound of JsonResult<Dto.ProjectDetails>
    | ProjectNotFound
    | LogProjectResult of Dto.ProjectList
    | LogException of System.Exception
    | ListAllProjects
    | ListOwnedProjects of string
    | ToggleAdmin
    | LogText of string

type Model = { FoundProjects : Dto.ProjectList; IsAdmin : bool }  // TODO: Handle case where we also have roles

let init() =
    { FoundProjects = []; IsAdmin = false }, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | ToggleAdmin ->
        let nextModel = { currentModel with IsAdmin = not currentModel.IsAdmin }
        let loggedInUser = "rhood"  // TODO: Once we implement the login page properly, get the currently logged in user here instead (and get rid of the IsAdmin checkbox)
        let msg = if nextModel.IsAdmin then ListAllProjects else ListOwnedProjects loggedInUser
        nextModel, Cmd.ofMsg msg
    | FindProject projectCode ->
        let url = sprintf "/api/project/%s" projectCode
        currentModel, Cmd.OfPromise.either Fetch.get url SingleProjectFound LogException
    | ListAllProjects ->
        let url = sprintf "/api/project"
        currentModel, Cmd.OfPromise.either Fetch.get url ProjectsFound LogException
    | ListOwnedProjects username ->
        let url = sprintf "/api/users/%s/projects" username
        let data : Api.LoginCredentials = { username = username; password = "x" }
        currentModel, Cmd.OfPromise.either (fun data -> Fetch.post(url, data)) data ProjectsAndRolesFound LogException
    | LogText text ->
        printfn "%A" text
        currentModel, Cmd.none
    | ProjectsFound result ->
        unpackJsonResult currentModel result (fun projects ->
            let nextModel = { currentModel with FoundProjects = projects }
            nextModel, Cmd.ofMsg (LogProjectResult projects))
    | ProjectsAndRolesFound result ->
        unpackJsonResult currentModel result (fun projects ->
            // For some reason, List.map isn't working right so we use Seq.map here
            let nextModel = { currentModel with FoundProjects = projects |> Seq.map (fun (project, roles) -> project) |> List.ofSeq }
            nextModel, Cmd.ofMsg (LogProjectResult (projects |> Seq.map (fun (project, roles) -> project) |> List.ofSeq)))
    | SingleProjectFound result ->
        unpackJsonResult currentModel result (fun project ->
            let nextModel = { currentModel with FoundProjects = [project] }
            nextModel, Cmd.ofMsg (LogProjectResult [project]))
    | ProjectNotFound ->
        currentModel, Cmd.none
    | LogProjectResult projects ->
        for project in projects do
            printfn "Project %s, name %s, description %s, member(s) %A" project.code project.name project.description project.membership
        currentModel, Cmd.none
    | LogException exn ->
        let cmd = Notifications.notifyError exn.Message
        currentModel, cmd

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ] [ str "This is the projects page"
              br [ ]
              Checkbox.input [ Props [ Checked model.IsAdmin; OnClick (fun evt -> dispatch ToggleAdmin ) ] ]
              Checkbox.checkbox [ Props [ Checked model.IsAdmin; OnClick (fun evt -> dispatch ToggleAdmin ) ] ]  [ str "Logged in as admin (test mode) "]
              br [ ]
              textInputComponent "Find project" "" (Button.button [ Button.Color IsPrimary ] [ str "Find project" ]) (dispatch << FindProject)
              br [ ]
              Button.button [ Button.Color IsPrimary; Button.OnClick (fun _ -> dispatch ListAllProjects) ] [ str "List all" ]
              ul [ ] [
                  for project in model.FoundProjects ->
                    li [ ] [ a [ Href (sprintf "#project/%s" project.code) ] [ str (project.name) ] ]
              ]
            ]
