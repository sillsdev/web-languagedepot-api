module LoginPage

open Elmish
open Fable.React
open Fable.React.Props

open Shared

type Msg =
    | RootModelUpdated of RootPage.Model
    | ConnectOnLoginHook of (SharedUser -> unit)
    | ConnectOnLogoutHook of (unit -> unit)
    | DisconnectOnLoginHook
    | DisconnectOnLogoutHook
    | LoginInputChanged of string

type Model = { RootModel : RootPage.Model; OnLogin : SharedUser -> unit; OnLogout : unit -> unit; LoginInput : string }

let init rootModel = { RootModel = rootModel; OnLogin = ignore; OnLogout = ignore; LoginInput = "" }, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | RootModelUpdated newRootModel ->
        let nextModel = { currentModel with RootModel = newRootModel }
        nextModel, Cmd.none
    | ConnectOnLoginHook onLogin ->
        let nextModel = { currentModel with OnLogin = onLogin }
        nextModel, Cmd.none
    | ConnectOnLogoutHook onLogout ->
        let nextModel = { currentModel with OnLogout = onLogout }
        nextModel, Cmd.none
    | DisconnectOnLoginHook ->
        let nextModel = { currentModel with OnLogin = ignore }
        nextModel, Cmd.none
    | DisconnectOnLogoutHook ->
        let nextModel = { currentModel with OnLogout = ignore }
        nextModel, Cmd.none
    | LoginInputChanged username ->
        let nextModel = { currentModel with LoginInput = username }
        nextModel, Cmd.none

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ]
        [ input [ OnChange (fun ev -> ev.Value |> LoginInputChanged |> dispatch) ]
          button [ OnClick (fun _ -> model.OnLogin { Name = model.LoginInput; Email = "rmunn@pobox.com" }) ] [ str "Log in"]
          button [ OnClick (fun _ -> model.OnLogout()) ] [ str "Log out"] ]
