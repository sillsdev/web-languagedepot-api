module Client

open Browser
open Elmish
open Elmish.Navigation
open Elmish.React
open Fable.Core.JsInterop
open Fable.FontAwesome
open Fable.FontAwesome.Free
open Fable.React
open Fable.React.Props
open Fetch.Types
open Thoth.Fetch
open Fulma
open Thoth.Json

open Shared

module Nav =
    open Elmish.UrlParser

    type Route =
        | UserPage of string
        | ProjectPage of string
        | RootPage
        | LoginPage

    let route =
        oneOf [
            s "user" </> str |> map UserPage
            s "project" </> str |> map ProjectPage
            s "login" |> map LoginPage
            top |> map RootPage
        ]

    let toRoute = function
        | UserPage username -> sprintf "#user/%s" username
        | ProjectPage projectCode -> sprintf "#project/%s" projectCode
        | LoginPage -> "#login"
        | RootPage -> "#"

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { User: Shared.SharedUser option; UserList : string list; ProjectList : string list; Page : Nav.Route; RootModel : RootPage.Model; LoginModel : LoginPage.Model; ProjectModel : ProjectPage.Model; UserModel : UserPage.Model }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| AddProject of string
| DelProject of string
| UserProjectsUpdated of Shared.SharedUser
| FindUser of string
| UserNotFound
| LogResult of Result<string,string>
| UserFound of Shared.SharedUser
| ListAllUsers
| UserListRetrieved of string list
| ListAllProjects
| ProjectListRetrieved of string list
| GetProjectsForUser of string
| ProjectsListRetrieved of string list
| RootPageMsg of RootPage.Msg
| LoginPageMsg of LoginPage.Msg
| ProjectPageMsg of ProjectPage.Msg
| UserPageMsg of UserPage.Msg

let msgsWhenRootModelUpdates = [
    LoginPage.Msg.RootModelUpdated >> LoginPageMsg
    ProjectPage.Msg.RootModelUpdated >> ProjectPageMsg
    UserPage.Msg.RootModelUpdated >> UserPageMsg
]

// defines the initial state and initial command (= side-effect) of the application
let init page : Model * Cmd<Msg> =
    let initialRootModel = RootPage.init()
    let initialModel = { Page = defaultArg page Nav.RootPage
                         User = Some { Name = "Robin"; Projects = ["ldapi"] }; UserList = []; ProjectList = []
                         RootModel = initialRootModel
                         LoginModel = LoginPage.init initialRootModel
                         ProjectModel = ProjectPage.init initialRootModel
                         UserModel = UserPage.init initialRootModel }
    initialModel, Cmd.none

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match currentModel, msg with
    | { User = Some user }, AddProject projCode ->
        let foo = { Name = user.Name; Projects = [ projCode ] }
        let bar = Remove { Remove = foo }
        let json = Encode.Auto.toString(4, bar)
        let url = sprintf "/api/projects/%s" user.Name
        let promise = Fetch.patch(url, bar) |> Promise.map LogResult
        currentModel, Cmd.OfPromise.result promise
    | { User = Some user },  DelProject projCode ->
        currentModel, Cmd.none
    | _, UserProjectsUpdated _ ->
        currentModel, Cmd.none
    | _, ListAllUsers ->
        let url = "/api/users"
        currentModel, Cmd.OfPromise.perform Fetch.get url UserListRetrieved
    | _, UserListRetrieved users ->
        let nextModel = { currentModel with UserList = users }
        nextModel, Cmd.none
    | _, ListAllProjects ->
        let url = "/api/project"
        currentModel, Cmd.OfPromise.perform Fetch.get url ProjectListRetrieved
    | _, ProjectListRetrieved projects ->
        let nextModel = { currentModel with ProjectList = projects }
        nextModel, Cmd.none
    | _, GetProjectsForUser user ->
        let url = sprintf "/api/users/%s/projects" user
        let data = { username = user; password = "s3kr3t" }
        let promise = Fetch.post(url, data) |> Promise.map ProjectsListRetrieved
        currentModel, Cmd.OfPromise.result promise
    | _, ProjectsListRetrieved projects ->
        let nextModel = { currentModel with ProjectList = projects }
        nextModel, Cmd.none
    | _, LogResult result ->
        match result with
        | Ok s -> console.log("Success: " + s)
        | Error s -> console.log("Error: " + s)
        currentModel, Cmd.none
    // Sub pages
    | { RootModel = rootModel }, RootPageMsg rootMsg ->
        let nextRootModel, nextRootCmds = rootModel |> RootPage.update rootMsg
        let nextModel = { currentModel with RootModel = nextRootModel }
        let otherPageMsgs = msgsWhenRootModelUpdates |> List.map (fun f -> f nextRootModel |> Cmd.ofMsg) |> Cmd.batch
        nextModel, Cmd.batch [Cmd.map RootPageMsg nextRootCmds; otherPageMsgs]
    | { LoginModel = loginModel }, LoginPageMsg loginMsg ->
        let nextLoginModel, nextLoginCmds = loginModel |> LoginPage.update loginMsg
        let nextModel = { currentModel with LoginModel = nextLoginModel }
        nextModel, nextLoginCmds |> Cmd.map LoginPageMsg
    | { ProjectModel = projectModel }, ProjectPageMsg projectMsg ->
        let nextProjectModel, nextProjectCmds = projectModel |> ProjectPage.update projectMsg
        let nextModel = { currentModel with ProjectModel = nextProjectModel }
        nextModel, nextProjectCmds |> Cmd.map ProjectPageMsg
    | { UserModel = userModel }, UserPageMsg userMsg ->
        let nextUserModel, nextUserCmds = userModel |> UserPage.update userMsg
        let nextModel = { currentModel with UserModel = nextUserModel }
        nextModel, nextUserCmds |> Cmd.map UserPageMsg

// TODO: Look into Fetch.patch and test the JSON stuff in it

let safeComponents =
    let components =
        span [ ]
           [ a [ Href "https://github.com/SAFE-Stack/SAFE-template" ]
               [ str "SAFE  "
                 str Version.template ]
             str ", "
             a [ Href "https://saturnframework.github.io" ] [ str "Saturn" ]
             str ", "
             a [ Href "http://fable.io" ] [ str "Fable" ]
             str ", "
             a [ Href "https://elmish.github.io" ] [ str "Elmish" ]
             str ", "
             a [ Href "https://fulma.github.io/Fulma" ] [ str "Fulma" ]
             str ", "
             a [ Href "https://bulmatemplates.github.io/bulma-templates/" ] [ str "Bulma\u00A0Templates" ]

           ]

    span [ ]
        [ str "Version "
          strong [ ] [ str Version.app ]
          str " powered by: "
          components ]

let navBrand =
    Navbar.navbar [ Navbar.Color IsWhite ]
        [ Container.container [ ]
            [ Navbar.Brand.div [ ]
                [ Navbar.Item.a [ Navbar.Item.CustomClass "brand-text" ]
                      [ str "SAFE Admin" ] ]
              Navbar.menu [ ]
                  [ Navbar.Start.div [ ]
                      [ Navbar.Item.a [ ]
                            [ str "Home" ]
                        Navbar.Item.a [ ]
                            [ str "Orders" ]
                        Navbar.Item.a [ ]
                            [ str "Payments" ]
                        Navbar.Item.a [ ]
                            [ str "Exceptions" ] ] ] ] ]

let menu =
    Menu.menu [ ]
        [ Menu.label [ ]
              [ str "General" ]
          Menu.list [ ]
              [ Menu.Item.a [ ]
                    [ str "Dashboard" ]
                Menu.Item.a [ ]
                    [ str "Customers" ] ]
          Menu.label [ ]
              [ str "Administration" ]
          Menu.list [ ]
              [ Menu.Item.a [ ]
                  [ str "Team Settings" ]
                li [ ]
                    [ a [ ]
                        [ str "Manage Your Team" ]
                      Menu.list [ ]
                          [ Menu.Item.a [ ]
                                [ str "Members" ]
                            Menu.Item.a [ ]
                                [ str "Plugins" ]
                            Menu.Item.a [ ]
                                [ str "Add a member" ] ] ]
                Menu.Item.a [ ]
                    [ str "Invitations" ]
                Menu.Item.a [ ]
                    [ str "Cloud Storage Environment Settings" ]
                Menu.Item.a [ ]
                    [ str "Authentication" ] ]
          Menu.label [ ]
              [ str "Transactions" ]
          Menu.list [ ]
              [ Menu.Item.a [ ]
                    [ str "Payments" ]
                Menu.Item.a [ ]
                    [ str "Transfers" ]
                Menu.Item.a [ ]
                    [ str "Balance" ] ] ]

let breadcrump =
    Breadcrumb.breadcrumb [ ]
        [ Breadcrumb.item [ ]
              [ a [ ] [ str "Bulma" ] ]
          Breadcrumb.item [ ]
              [ a [ ] [ str "Templates" ] ]
          Breadcrumb.item [ ]
              [ a [ ] [ str "Examples" ] ]
          Breadcrumb.item [ Breadcrumb.Item.IsActive true ]
              [ a [ ] [ str "Admin" ] ] ]

let hero =
    Hero.hero [ Hero.Color IsInfo
                Hero.CustomClass "welcome" ]
        [ Hero.body [ ]
            [ Container.container [ ]
                [ Heading.h1 [ ]
                      [ str "Hello, Admin." ]
                  safeComponents ] ] ]

let info =
    section [ Class "info-tiles" ]
        [ Tile.ancestor [ Tile.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
            [ Tile.parent [ ]
                  [ Tile.child [ ]
                      [ Box.box' [ ]
                          [ Heading.p [ ]
                                [ str "439k" ]
                            Heading.p [ Heading.IsSubtitle ]
                                [ str "Users" ] ] ] ]
              Tile.parent [ ]
                  [ Tile.child [ ]
                      [ Box.box' [ ]
                          [ Heading.p [ ]
                                [ str "59k" ]
                            Heading.p [ Heading.IsSubtitle ]
                                [ str "Products" ] ] ] ]
              Tile.parent [ ]
                  [ Tile.child [ ]
                      [ Box.box' [ ]
                          [ Heading.p [ ]
                                [ str "3.4k" ]
                            Heading.p [ Heading.IsSubtitle ]
                                [ str "Open Orders" ] ] ] ]
              Tile.parent [ ]
                  [ Tile.child [ ]
                      [ Box.box' [ ]
                          [ Heading.p [ ]
                                [ str "19" ]
                            Heading.p [ Heading.IsSubtitle ]
                                [ str "Exceptions" ] ] ] ] ] ]

type TextInputProps = { placeholder : string; value : string; btn : ReactElement; dispatch : Dispatch<string> }
let TextInputComponent =
    FunctionComponent.Of (fun (props : TextInputProps) ->
        let state = Hooks.useState(props.value)
        Control.div
           [ Control.HasIconRight ]
           [ Field.div
               [ Field.HasAddons ]
               [ Input.text
                   [ Input.Placeholder props.placeholder
                     Input.Value state.current
                     Input.OnChange (fun ev -> state.update (!!ev.target?value : string)) ]
                 Button.a
                   [ Button.Color IsInfo
                     Button.OnClick (fun _ -> props.dispatch state.current) ]
                   [ props.btn ] ] ]
    )

let textInputComponent placeholder initialValue btn dispatch =
    TextInputComponent { placeholder = placeholder; value = initialValue; btn = btn; dispatch = dispatch; }

let columns (model : Model) (dispatch : Msg -> unit) =
    Columns.columns [ ]
        [ Column.column [ Column.Width (Screen.All, Column.Is6) ]
              [ Card.card [ ]
                  [ Card.header [ ]
                      [ Card.Header.title [ ]
                          [ str "Username/Email Search" ]
                        Card.Header.icon [ ]
                            [ Icon.icon [ ]
                                [ Fa.i [Fa.Solid.AngleDown] [] ] ] ]
                    Card.content [ ]
                        [ Content.content [ ]
                            [ Control.div
                                [ Control.HasIconLeft
                                  Control.HasIconRight ]
                                [ Input.text
                                      [ Input.Size IsLarge ]
                                  Icon.icon
                                      [ Icon.Size IsMedium
                                        Icon.IsLeft ]
                                      [ Fa.i [Fa.Solid.Search] [] ]
                                  Icon.icon
                                      [ Icon.Size IsMedium
                                        Icon.IsRight ]
                                      [ Fa.i [Fa.Solid.Check] [] ] ] ] ]
                    Card.footer [ ]
                        [ Button.button
                            [ Button.Color IsInfo
                              Button.OnClick (fun _ -> dispatch ListAllUsers) ]
                            [ str "All users" ]
                          Button.button
                            [ Button.Color IsInfo
                              Button.OnClick (fun _ -> dispatch ListAllProjects) ]
                            [ str "All projects" ] ] ]
                Card.card [ ]
                  [ Card.header [ ]
                      [ Card.Header.title [ ]
                          [ str "Projects" ]
                        Card.Header.icon [ ]
                            [ Icon.icon [ ]
                                [ Fa.i [Fa.Solid.AngleDown] [] ] ] ]
                    Card.content [ ]
                      [ Content.content [ ]
                          [ ul [ ]
                               [ for project in model.ProjectList ->
                                    li [ ] [ str project ] ] ] ] ] ]
          Column.column [ Column.Width (Screen.All, Column.Is6) ]
            [ Card.card [ CustomClass "events-card" ]
                [ Card.header [ ]
                    [ Card.Header.title [ ]
                        [ str "Users" ]
                      Card.Header.icon [ ]
                          [ Icon.icon [ ]
                              [ Fa.i [ Fa.Solid.AngleDown ] [] ] ] ]
                  div [ Class "card-table" ]
                      [ Content.content [ ]
                          [ Table.table
                              [ Table.IsFullWidth
                                Table.IsStriped ]
                              [ tbody [ ]
                                  [ for user in model.UserList ->
                                      tr [ ]
                                          [ td [ Style [ Width "5%" ] ]
                                              [ Icon.icon
                                                  [ ]
                                                  [ Fa.i [ Fa.Solid.User ] [] ] ]
                                            td [ ]
                                                [ str user ]
                                            td [ ]
                                                [ Button.a
                                                    [ Button.Size IsSmall
                                                      Button.Color IsPrimary
                                                      Button.OnClick (fun _ -> dispatch (GetProjectsForUser user)) ]
                                                    [ str "Projects" ] ] ] ] ] ] ]
                  Card.footer [ ]
                      [ Card.Footer.div [ ]
                          [ textInputComponent "Project code" "" (str "+") (dispatch << AddProject) ] ] ] ] ]

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ]
        [ navBrand
          Container.container [ ]
              [ Columns.columns [ ]
                  [ Column.column [ Column.Width (Screen.All, Column.Is3) ]
                      [ menu ]
                    Column.column [ Column.Width (Screen.All, Column.Is9) ]
                      [ breadcrump
                        hero
                        info
                        columns model dispatch ] ] ] ]

let urlUpdate (result : Nav.Route option) model =
  match result with
  | Some page ->
      { model with Page = page }, Cmd.none

  | None ->
      model, Navigation.modifyUrl (Nav.toRoute Nav.RootPage)

let routingView (model : Model) (dispatch : Msg -> unit) =
    match model.Page with
    | Nav.RootPage -> RootPage.view model.RootModel (RootPageMsg >> dispatch)
    | Nav.LoginPage -> LoginPage.view model.LoginModel (LoginPageMsg >> dispatch)
    // TODO: Do something with `code`
    | Nav.ProjectPage code -> ProjectPage.view model.ProjectModel (ProjectPageMsg >> dispatch)
    // TODO: Do something with `username`
    | Nav.UserPage username -> UserPage.view model.UserModel (UserPageMsg >> dispatch)

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update routingView
|> Program.toNavigable (UrlParser.parseHash Nav.route) urlUpdate
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
