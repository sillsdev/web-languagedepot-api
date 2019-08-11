module LoginPage

open Elmish
open Fable.React

type Msg = RootModelUpdated of RootPage.Model

type Model = { RootModel : RootPage.Model }

let init rootModel = { RootModel = rootModel }, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | RootModelUpdated newRootModel ->
        let nextModel = { currentModel with RootModel = newRootModel }
        nextModel, Cmd.none

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ] [ str "This is the login page" ]
