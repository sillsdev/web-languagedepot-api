module RootPage

open Elmish
open Fable.React
open Fable.React.Props

open Shared

type Msg = Msg

type Model = { nothing : unit }

let init() = { nothing = () }

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    currentModel, Cmd.none

let userCtx : IContext<SharedUser option> = createContext None

let ShowUser = FunctionComponent.Of (fun (props : {|dispatch : Msg -> unit|}) ->
    let userCtx = Hooks.useContext userCtx
    match userCtx with
    | None -> str "Please "; a [ Href "#login"] [ str "log in" ]
    | Some user -> str ("Hello, " + user.Name)
)

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ]
        [ str "This is the root page"
          br [ ]
          ShowUser {|dispatch = dispatch|} ]
