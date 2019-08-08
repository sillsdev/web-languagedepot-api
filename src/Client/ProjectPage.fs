module ProjectPage

open Elmish
open Fable.React

type Msg =
    | RootModelUpdated of RootPage.Model
    | NewProjectPageNav of string

type Model = { RootModel : RootPage.Model; CurrentlyViewedProject : string }

let init rootModel = { RootModel = rootModel; CurrentlyViewedProject = "" }

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | RootModelUpdated newRootModel ->
        let nextModel = { currentModel with RootModel = newRootModel }
        nextModel, Cmd.none
    | NewProjectPageNav projectCode ->
        let nextModel = { currentModel with CurrentlyViewedProject = projectCode }
        nextModel, Cmd.none

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ] [ str ("This is the project page" + if System.String.IsNullOrEmpty model.CurrentlyViewedProject then "" else " for " + model.CurrentlyViewedProject) ]
