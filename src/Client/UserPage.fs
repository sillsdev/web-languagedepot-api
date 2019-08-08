module UserPage

open Elmish
open Fable.React

type Msg =
    | RootModelUpdated of RootPage.Model
    | NewUserPageNav of string

type Model = { RootModel : RootPage.Model; CurrentlyViewedUser : string }

let init rootModel = { RootModel = rootModel; CurrentlyViewedUser = "" }

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | RootModelUpdated newRootModel ->
        let nextModel = { currentModel with RootModel = newRootModel }
        nextModel, Cmd.none
    | NewUserPageNav username ->
        let nextModel = { currentModel with CurrentlyViewedUser = username }
        nextModel, Cmd.none

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ] [ str ("This is the user page" + if System.String.IsNullOrEmpty model.CurrentlyViewedUser then "" else " for " + model.CurrentlyViewedUser) ]
