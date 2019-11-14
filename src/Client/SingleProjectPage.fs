module SingleProjectPage

open Browser
open Elmish
open Fable.FontAwesome
open Fable.FontAwesome.Free
open Fable.React
open Fable.React.Props
open Fulma
open Thoth.Elmish
open Thoth.Elmish.FormBuilder
open Thoth.Elmish.FormBuilder.BasicFields
open Thoth.Fetch

open JsonHelpers
open Shared

type Msg =
    | NewProjectPageNav of string
    | OnFormMsg of FormBuilder.Types.Msg
    | ListAllProjects
    | ListSingleProject of string
    | ProjectListRetrieved of JsonResult<Dto.ProjectList>
    | SingleProjectRetrieved of JsonResult<Dto.ProjectDetails>
    | ClearProjects
    | FormSubmitted
    | GotFormResult of JsonResult<int>
    | HandleFetchError of exn
    | EditMembership of string * Dto.ProjectDetails
    | RemoveMembership of string * Dto.ProjectDetails * RoleType
    | GotStringResult of JsonResult<string>

type Model = { CurrentlyViewedProject : Dto.ProjectDetails option; ProjectList : Dto.ProjectList; FormState : FormBuilder.Types.State }

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

let init() =
    let formState, formCmds = Form.init formConfig formState
    { CurrentlyViewedProject = None; ProjectList = []; FormState = formState }, Cmd.map OnFormMsg formCmds

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | HandleFetchError e ->
        currentModel, Notifications.notifyError e.Message
    | NewProjectPageNav projectCode ->
        currentModel, Cmd.ofMsg (ListSingleProject projectCode)
    | OnFormMsg msg ->
        let (formState, formCmd) = Form.update formConfig msg currentModel.FormState
        let nextModel = { currentModel with FormState = formState }
        nextModel, Cmd.map OnFormMsg formCmd
    | ListAllProjects ->
        let url = "/api/project"
        currentModel, Cmd.OfPromise.either Fetch.get url ProjectListRetrieved HandleFetchError
    | ListSingleProject projectCode ->
        let url = sprintf "/api/project/%s" projectCode
        currentModel, Cmd.OfPromise.either Fetch.get url SingleProjectRetrieved HandleFetchError
    | SingleProjectRetrieved projectResult ->
        match toResult projectResult with
        | Ok project ->
            printfn "%A" project
            { currentModel with CurrentlyViewedProject = Some project }, Cmd.none
        | Error msg ->
            currentModel, Notifications.notifyError msg
    | ProjectListRetrieved projectsResult ->
        match toResult projectsResult with
        | Ok projects ->
            { currentModel with ProjectList = projects }, Cmd.none
        | Error msg ->
            currentModel, Notifications.notifyError msg
    | ClearProjects ->
        let nextModel = { currentModel with ProjectList = [] }
        nextModel, Cmd.none
    | FormSubmitted ->
        let newFormState, isValid = Form.validate formConfig currentModel.FormState
        let nextModel = { currentModel with FormState = newFormState }
        if isValid then
            let json = Form.toJson formConfig newFormState
            match Thoth.Json.Decode.Auto.fromString<Api.CreateProject> json with
            | Ok data ->
                let url = "/api/project"
                nextModel, Cmd.OfPromise.either (fun data -> Fetch.post(url, data)) data GotFormResult HandleFetchError
            | Error err ->
                printfn "Decoding error (fix the form validation?): %s" err
                nextModel, Cmd.none
        else
            nextModel, Cmd.none  // TODO: Do something to report "invalid form not submitted"?
    | GotFormResult jsonResult ->
        match toResult jsonResult with
        | Ok n ->
            printfn "Got ID %d from server" n
        | Error e ->
            printfn "Server responded with error message: %s" e
        currentModel, [fun _ -> history.go -1]
    | GotStringResult jsonResult ->
        match toResult jsonResult with
        | Ok s ->
            printfn "Got success message %s from server" s
        | Error e ->
            printfn "Server responded with error message: %s" e
        currentModel, Cmd.none
    | EditMembership (name,project) ->
        console.log("Not yet implemented. Would edit membership of",name,"in project",project)
        // Once we implement, this should pop up a modal with four checkboxes, so you can change what role(s) someone has.
        currentModel, Cmd.none
    | RemoveMembership (name,project,role) ->
        let url = sprintf "/api/project/%s/user/%s/withRole/%s" project.code name (role.ToString())
        currentModel, Cmd.OfPromise.either (fun data -> Fetch.delete(url, data)) () GotStringResult HandleFetchError

let formActions (formState : FormBuilder.Types.State) dispatch =
    div [ ]
        [ Button.button
            [ Button.Props [ OnClick (fun _ -> dispatch FormSubmitted) ]
              Button.Color IsPrimary
            ]
            [ str "Submit" ] ]

let membershipViewInline (membership : Dto.MemberList option) =
    match membership with
    | None -> str " (member list not provided)"
    | Some members ->
        if Seq.isEmpty members then
            str (sprintf " (no members in %A)" members)
        else
            let toStr title (role : RoleType) (lst : Dto.MemberList) =
                let filteredList = lst |> Seq.filter (snd >> (fun itemRole -> itemRole.ToString() = role.ToString()))
                if Seq.isEmpty filteredList then "" else title + ": " + String.concat "," (filteredList |> Seq.map fst) + ";"
            // TODO: Investigate why List.isEmpty and List.filter both seem to be returning *incorrect* results!
            str ((toStr "Managers" Manager members +
                  toStr "Contributors" Contributor members +
                  toStr "Observers" Observer members +
                  toStr "Programmers" Programmer members).TrimEnd(';'))

let membershipViewBlock (dispatch : Msg -> unit) (project : Dto.ProjectDetails) =
    match project.membership with
    | None -> []
    | Some members ->
        let section title (role : RoleType) (lst : Dto.MemberList) =
            [
                h2 [ ] [ str title ]
                for name, itemRole in lst do
                    console.log("Matching", (name, itemRole), "with string repr", itemRole.ToString(), "against", role, "with string repr", role.ToString(), (sprintf "which %s" (if itemRole.ToString() = role.ToString() then "matches" else "doesn't match")))
                    // TODO: Investigate why the itemRole.ToNumericId() in all these pairs is *always* coming out as 3 (manager) no matter what the role actually is.
                    // For now, we'll use ToString instead of ToNumericId; it's not meaningfully slower since the strings are short, and it works.
                    if itemRole.ToString() = role.ToString() then yield! [
                        str name
                        str "\u00a0"
                        a [ Style [Color "inherit"]; OnClick (fun _ -> dispatch (EditMembership (name, project)))] [ Fa.span [ Fa.Solid.Edit ] [ ] ]
                        str "\u00a0"
                        a [ Style [Color "red"]; OnClick (fun _ -> dispatch (RemoveMembership (name, project, itemRole)))] [ Fa.span [ Fa.Solid.Times ] [ ] ]
                        br []
                    ]
                    // Can't just compare itemRole to role because in Javascript they end up being different types, for reasons I don't yet understand
            ]
        [
            yield! section "Managers" Manager members
            yield! section "Contributors" Contributor members
            yield! section "Observers" Observer members
            yield! section "Programmers" Programmer members
        ]

let projectDetailsView (dispatch : Msg -> unit) (project : Dto.ProjectDetails option) =
    div [ ] [
        match project with
        | None -> str "(no project loaded)"
        | Some project ->
            h2 [ ] [ str project.name; str (sprintf " (%s)" project.code); membershipViewInline project.membership ]
            p [ ] [ str project.description ]
            yield! membershipViewBlock dispatch project
    ]

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ] [
        projectDetailsView dispatch model.CurrentlyViewedProject
        br [ ]
        Form.render {
            Config = formConfig
            State = model.FormState
            Dispatch = dispatch
            ActionsArea = (formActions model.FormState dispatch)
            Loader = Form.DefaultLoader }
        br [ ]
        (if model.ProjectList |> List.isEmpty then
            Button.button
                [ Button.Props [ OnClick (fun _ -> dispatch ListAllProjects) ]
                  Button.Color IsPrimary
                ]
                [ str "List Projects" ]
        else
            Button.button
                [ Button.Props [ OnClick (fun _ -> dispatch ClearProjects) ]
                  Button.Color IsPrimary
                ]
                [ str "Clear Project list" ])
        ul [ ]
           [ for project in model.ProjectList ->
                li [ ] [ a [ OnClick (fun _ -> dispatch (ListSingleProject project.code)) ] [ str (sprintf "%s:" project.name); membershipViewInline project.membership ] ] ]
        ]
