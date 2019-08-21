module LoginPage

open Elmish
open Fable.React
open Fable.React.Props

open Shared

type Msg =
    | RootModelUpdated of RootPage.Model
    | LoginInputChanged of string

type Model = { RootModel : RootPage.Model; LoginInput : string }

let init rootModel = { RootModel = rootModel; LoginInput = "" }, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | RootModelUpdated newRootModel ->
        let nextModel = { currentModel with RootModel = newRootModel }
        nextModel, Cmd.none
    | LoginInputChanged username ->
        let nextModel = { currentModel with LoginInput = username }
        nextModel, Cmd.none

let LoginView = FunctionComponent.Of (fun (props : {|model : Model; dispatch : Msg -> unit|}) ->
    let currentUser, setUser = Hooks.useContext RootPage.userCtx
    div [ ]
        [ input [ OnChange (fun ev -> ev.Value |> LoginInputChanged |> props.dispatch) ]
          button [ OnClick (fun _ -> setUser (Some { Name = props.model.LoginInput; Email = "rmunn@pobox.com" })) ] [ str "Log in"]
          button [ OnClick (fun _ -> setUser None) ] [ str "Log out"] ]
)

let view (model : Model) (dispatch : Msg -> unit) =
    LoginView {| model = model; dispatch = dispatch |}
