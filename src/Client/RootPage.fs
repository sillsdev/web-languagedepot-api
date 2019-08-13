module RootPage

open Elmish
open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Fulma

open Shared

type Msg = Msg

type Model = { nothing : unit }

let init() = { nothing = () }

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    currentModel, Cmd.none

let userCtx : IContext<SharedUser option> = createContext None

let md5 (s : string) : string = importDefault "md5"

let Avatar = FunctionComponent.Of (fun (props : {| user : SharedUser |}) ->
    let name = if isNull props.user.Name then "" else props.user.Name
    let email = if isNull props.user.Email then "" else props.user.Email
    let emailForGravatar = email.Trim().ToLowerInvariant()
    let hash = md5 emailForGravatar
    let url = "https://www.gravatar.com/avatar/" + hash + "?d=mp"
    img [ Src url ]
)
let avatar user = Avatar {| user = user |}

let UserCard = FunctionComponent.Of (fun (props : {| user : SharedUser |}) ->
    Media.media [ ] [
        Media.left [ ] [
            Image.image [ Image.Is48x48 ] [
                avatar props.user
            ]
        ]
        Media.content [ ] [
            strong [ ] [ str props.user.Name ]
            br [ ]
            str props.user.Email
        ]
    ]
)
let userCard user = UserCard {| user = user |}

let ShowUser = FunctionComponent.Of (fun (props : {|dispatch : Msg -> unit|}) ->
    let userCtx = Hooks.useContext userCtx
    match userCtx with
    | None -> p [ ] [ str "Please "; a [ Href "#login"] [ str "log in" ] ]
    | Some user -> p [ ] [ str ("Hello, " + user.Name); br [ ]; userCard user ]
)

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ]
        [ str "This is the root page"
          br [ ]
          ShowUser {|dispatch = dispatch|} ]
