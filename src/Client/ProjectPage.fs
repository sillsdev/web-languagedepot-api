module ProjectPage

open Elmish
open Fable.React
open Fable.React.Props
open Fulma
open Thoth.Elmish
open Thoth.Elmish.FormBuilder
open Thoth.Elmish.FormBuilder.BasicFields
open Fetch.Types

type Msg =
    | RootModelUpdated of RootPage.Model
    | NewProjectPageNav of string
    | OnFormMsg of FormBuilder.Types.Msg
    | FormSubmitted
    | GotFormResult of Result<Response,System.Exception>
    | ParsedFormResult of string

type Model = { RootModel : RootPage.Model; CurrentlyViewedProject : string; FormState : FormBuilder.Types.State }

let (formState, formConfig) =
    Form<Msg>
        .Create(OnFormMsg)
        .AddField(
            BasicInput
                .Create("Name")
                .WithLabel("Project Name")
                .IsRequired()
                .WithDefaultView()
        )
        .AddField(
            BasicTextarea
                .Create("Description")
                .WithLabel("Description")
                .WithPlaceholder("Describe your project in a paragraph or two")
                .WithDefaultView()
        )
        .AddField(
            BasicInput
                .Create("Identifier")
                .WithLabel("Project Code")
                .IsRequired("You must specify a project code")
                .AddValidator(fun state ->
                    let lower = state.Value.ToLowerInvariant()
                    if state.Value <> lower then
                        Types.Invalid "Project codes must be in lowercase letters"
                    else
                        let chars = lower.ToCharArray() |> Array.distinct |> Array.filter (fun ch -> ch < 'a' || ch > 'z')
                        let hasInvalidChars = chars |> Array.filter (fun ch -> ch <> '-' && ch <> '_') |> Array.length > 0
                        if hasInvalidChars then
                            Types.Invalid "Project codes must contain only letters, hyphens, and underscores"
                        else
                            Types.Valid
                )
                .WithDefaultView()
        )
        .Build()

let init rootModel =
    let formState, formCmds = Form.init formConfig formState
    { RootModel = rootModel; CurrentlyViewedProject = ""; FormState = formState }, Cmd.map OnFormMsg formCmds

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | RootModelUpdated newRootModel ->
        let nextModel = { currentModel with RootModel = newRootModel }
        nextModel, Cmd.none
    | NewProjectPageNav projectCode ->
        let nextModel = { currentModel with CurrentlyViewedProject = projectCode }
        nextModel, Cmd.none
    | OnFormMsg msg ->
        let (formState, formCmd) = Form.update formConfig msg currentModel.FormState
        let nextModel = { currentModel with FormState = formState }
        nextModel, Cmd.map OnFormMsg formCmd
    | FormSubmitted ->
        let newFormState, isValid = Form.validate formConfig currentModel.FormState
        if isValid then
            let json = Form.toJson formConfig newFormState  // TODO: Convert form to a Shared.Project instance once https://github.com/MangelMaxime/Thoth/issues/93 is implemented
            let url = "/api/project"
            let nextModel = { currentModel with FormState = newFormState }
            let props = [
                Method HttpMethod.POST
                Body (unbox json)
            ]
            nextModel, Cmd.OfPromise.perform (Fetch.tryFetch url) props GotFormResult
        else
            currentModel, Cmd.none
    | GotFormResult result ->
        match result with
        | Ok response ->
            currentModel, Cmd.OfPromise.result (response.text() |> Promise.map ParsedFormResult)
        | Error ex ->
            printfn "Error submitting form: %s" ex.Message  // TODO: Make this pop up as a Toast notification
            currentModel, Cmd.none
    | ParsedFormResult text ->
        let n = System.Int32.Parse text
        printfn "Got ID %d from server" n
        currentModel, Cmd.none

let formActions (formState : FormBuilder.Types.State) dispatch =
    div [ ]
        [ Button.button
            [ Button.Props [ OnClick (fun _ -> dispatch FormSubmitted) ]
              Button.Color IsPrimary
            ]
            [ str "Submit" ] ]

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ] [
        str ("This is the project page" + if System.String.IsNullOrEmpty model.CurrentlyViewedProject then "" else " for " + model.CurrentlyViewedProject)
        br [ ]
        Form.render {
            Config = formConfig
            State = model.FormState
            Dispatch = dispatch
            ActionsArea = (formActions model.FormState dispatch)
            Loader = Form.DefaultLoader }
        ]
