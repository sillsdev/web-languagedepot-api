module UserPage

open Browser
open Elmish
open Fable.Core.JsInterop
open Fable.React
// open Thoth.Elmish.Toast
open Thoth.Fetch
open Thoth.Json

open TextInput
open Shared

type Msg =
    | RootModelUpdated of RootPage.Model
    | NewUserPageNav of string
    | AddProject of string
    | DelProject of string
    | LogResult of Result<string,string>

type Model = { RootModel : RootPage.Model; CurrentlyViewedUser : SharedUser option; }

let init rootModel = { RootModel = rootModel; CurrentlyViewedUser = None }

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | RootModelUpdated newRootModel ->
        let nextModel = { currentModel with RootModel = newRootModel }
        nextModel, Cmd.none
    | NewUserPageNav username ->
        let nextModel = { currentModel with CurrentlyViewedUser = Some { Name = username } }
        nextModel, Cmd.none
    | AddProject projCode ->
        match currentModel.CurrentlyViewedUser with
        | None ->
            currentModel, Cmd.none
        | Some user ->
            let data = Add { Add = user }
            let url = sprintf "/api/projects/%s" projCode
            let promise = Fetch.patch(url, data) |> Promise.map LogResult
            currentModel, Cmd.OfPromise.result promise
    | DelProject projCode ->
        match currentModel.CurrentlyViewedUser with
        | None ->
            currentModel, Cmd.none
        | Some user ->
            let data = Remove { Remove = user }
            let url = sprintf "/api/projects/%s" projCode
            let promise = Fetch.patch(url, data) |> Promise.map LogResult
            currentModel, Cmd.OfPromise.result promise
    | LogResult result ->
        let cmd = result |> Notifications.notifyStrResult
        currentModel, cmd

let view (model : Model) (dispatch : Msg -> unit) =
    let name = match model.CurrentlyViewedUser with None -> "" | Some user -> user.Name
    div [ ] [ str ("This is the user page" + if System.String.IsNullOrEmpty name then "" else " for " + name)
              br [ ]
              textInputComponent "Project code" "" (str "+") (dispatch << AddProject)
              br [ ]
              textInputComponent "Project code" "" (str "-") (dispatch << DelProject) ]
