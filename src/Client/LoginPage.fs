module LoginPage

open Elmish
open Fable.React
open Fable.React.Props

type Msg =
    | RootModelUpdated of RootPage.Model
    | LoggedInAs of string option

type Model = { RootModel : RootPage.Model }

let init rootModel = { RootModel = rootModel }, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | RootModelUpdated newRootModel ->
        let nextModel = { currentModel with RootModel = newRootModel }
        nextModel, Cmd.none
    | LoggedInAs _ ->
        currentModel, Cmd.none

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ]
        [ str "This is the login page"
          a [ OnClick (fun _ -> dispatch (LoggedInAs (Some "rmunn")) ) ] [ str "Login as rmunn" ]
          br [ ]
          a [ OnClick (fun _ -> dispatch (LoggedInAs None) ) ] [ str "Log out" ]
        ]
