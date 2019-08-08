module UserPage

open Elmish
open Fable.React

type Msg = Msg

type Model = { nothing : unit }

let init() = { nothing = () }

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    currentModel, Cmd.none

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ] [ str "This is the user page" ]
