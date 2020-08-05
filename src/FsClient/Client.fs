module Client

open Browser
open Elmish
open Feliz.Router
open Elmish.React
open Fable.Core.JsInterop
open Fable.FontAwesome
open Fable.FontAwesome.Free
open Fable.React
open Fable.React.Props
open Fetch.Types
open Thoth.Elmish
open Thoth.Fetch
open Fulma
open Thoth.Json

open TextInput
open Shared

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| UserProjectsUpdated of Shared.SharedUser
| LogResult of Result<string,string>
| RootPageMsg of RootPage.Msg
| UserLoggedIn of Shared.SharedUser option
| LoginPageMsg of LoginPage.Msg
| ProjectPageMsg of SingleProjectPage.Msg
| UserPageMsg of SingleUserPage.Msg
| AllUsersPageMsg of AllUsersPage.Msg
| AllProjectsPageMsg of AllProjectsPage.Msg
| NotImplementedPageMsg of NotImplementedPage.Msg
| UrlChanged of string list

module Nav =
    type Route =
        | UserPage of string
        | ProjectPage of string
        | AllUsersPage
        | AllProjectsPage
        | RootPage
        | LoginPage
        | NotImplementedPage

    let parseUrl = function
    // For pages with internal models based on the URL, pass on a message so they can update their internal model
    | [] -> RootPage, Cmd.none
    | ["user"; login] -> UserPage login, SingleUserPage.Msg.NewUserPageNav login |> UserPageMsg |> Cmd.ofMsg
    | ["users"] -> AllUsersPage, Cmd.none
    | ["project"; code] -> ProjectPage code, SingleProjectPage.Msg.NewProjectPageNav code |> ProjectPageMsg |> Cmd.ofMsg
    | ["projects"] -> AllProjectsPage, Cmd.none
    | ["login"] -> LoginPage, Cmd.none
    | ["logout"] -> RootPage, Cmd.ofMsg (UserLoggedIn None)
    | ["not-implemented"] -> NotImplementedPage, Cmd.none
    | _ -> RootPage, Cmd.none

    let toRoute = function
        | UserPage username -> sprintf "#user/%s" username
        | ProjectPage projectCode -> sprintf "#project/%s" projectCode
        | AllUsersPage -> "#users"
        | AllProjectsPage -> "#projects"
        | LoginPage -> "#login"
        | RootPage -> "#"
        | NotImplementedPage -> "#not-implemented"

    let jump (n:int):Cmd<_> =
        [fun _ -> history.go n]

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { UserList : Dto.UserDetails list; CurrentUser : SharedUser option; Page : Nav.Route; RootModel : RootPage.Model; LoginModel : LoginPage.Model; ProjectModel : SingleProjectPage.Model; UserModel : SingleUserPage.Model; AllUsersModel : AllUsersPage.Model; AllProjectsModel : AllProjectsPage.Model; NotImplementedModel : NotImplementedPage.Model }

// defines the initial state and initial command (= side-effect) of the application
let init() : Model * Cmd<Msg> =
    let initialRootModel, rootCmds = RootPage.init()
    let notImplementedModel, notImplementedCmds = NotImplementedPage.init()
    let loginModel, loginCmds = LoginPage.init()
    let projectModel, projectCmds = SingleProjectPage.init()
    let userModel, userCmds = SingleUserPage.init()
    let allUsersModel, allUsersCmds = AllUsersPage.init()
    let allProjectsModel, allProjectsCmds = AllProjectsPage.init()
    let initialModel = { Page = Nav.RootPage
                         CurrentUser = None
                         UserList = []
                         RootModel = initialRootModel
                         NotImplementedModel = notImplementedModel
                         LoginModel = loginModel
                         ProjectModel = projectModel
                         AllUsersModel = allUsersModel
                         AllProjectsModel = allProjectsModel
                         UserModel = userModel }
    initialModel, Cmd.batch [
        notImplementedCmds |> Cmd.map NotImplementedPageMsg
        loginCmds |> Cmd.map LoginPageMsg
        projectCmds |> Cmd.map ProjectPageMsg
        userCmds |> Cmd.map UserPageMsg
        allUsersCmds |> Cmd.map AllUsersPageMsg
        allProjectsCmds |> Cmd.map AllProjectsPageMsg
        rootCmds |> Cmd.map RootPageMsg
    ]

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match currentModel, msg with
    | _, UserProjectsUpdated _ ->
        currentModel, Cmd.none
    | _, UserLoggedIn user ->
        let nextModel = { currentModel with CurrentUser = user }
        nextModel, Router.navigate ""
    | _, LogResult result ->
        let cmd = result |> Notifications.notifyStrResult
        currentModel, cmd
    | _, UrlChanged parts ->
        let page, cmd = Nav.parseUrl parts
        let nextModel = { currentModel with Page = page }
        nextModel, cmd
    // Sub pages
    | { RootModel = rootModel }, RootPageMsg rootMsg ->
        let nextRootModel, nextRootCmds = rootModel |> RootPage.update rootMsg
        let nextModel = { currentModel with RootModel = nextRootModel }
        nextModel, nextRootCmds |> Cmd.map RootPageMsg
    | { LoginModel = loginModel }, LoginPageMsg loginMsg ->
        let nextLoginModel, nextLoginCmds = loginModel |> LoginPage.update loginMsg
        let nextModel = { currentModel with LoginModel = nextLoginModel }
        nextModel, nextLoginCmds |> Cmd.map LoginPageMsg
    | { ProjectModel = projectModel }, ProjectPageMsg projectMsg ->
        let nextProjectModel, nextProjectCmds = projectModel |> SingleProjectPage.update projectMsg
        let nextModel = { currentModel with ProjectModel = nextProjectModel }
        nextModel, nextProjectCmds |> Cmd.map ProjectPageMsg
    | { UserModel = userModel }, UserPageMsg userMsg ->
        let nextUserModel, nextUserCmds = userModel |> SingleUserPage.update userMsg
        let nextModel = { currentModel with UserModel = nextUserModel }
        nextModel, nextUserCmds |> Cmd.map UserPageMsg
    | { AllUsersModel = allUsersModel }, AllUsersPageMsg allUsersMsg ->
        let nextAllUsersModel, nextAllUsersCmds = allUsersModel |> AllUsersPage.update allUsersMsg
        let nextModel = { currentModel with AllUsersModel = nextAllUsersModel }
        nextModel, nextAllUsersCmds |> Cmd.map AllUsersPageMsg
    | { AllProjectsModel = allProjectsModel }, AllProjectsPageMsg allProjectsMsg ->
        let nextAllProjectsModel, nextAllProjectsCmds = allProjectsModel |> AllProjectsPage.update allProjectsMsg
        let nextModel = { currentModel with AllProjectsModel = nextAllProjectsModel }
        nextModel, nextAllProjectsCmds |> Cmd.map AllProjectsPageMsg
    | { NotImplementedModel = pageModel }, NotImplementedPageMsg pageMsg ->
        let nextpageModel, nextpageCmds = pageModel |> NotImplementedPage.update pageMsg
        let nextModel = { currentModel with NotImplementedModel = nextpageModel }
        nextModel, nextpageCmds |> Cmd.map NotImplementedPageMsg

let routingView (model : Model) (dispatch : Msg -> unit) =
    let pageView =
        match model.Page with
        | Nav.RootPage -> RootPage.view model.RootModel (RootPageMsg >> dispatch)
        | Nav.LoginPage -> LoginPage.view model.LoginModel (LoginPageMsg >> dispatch)
        | Nav.ProjectPage _ -> SingleProjectPage.view model.ProjectModel (ProjectPageMsg >> dispatch)
        | Nav.UserPage _ -> SingleUserPage.view model.UserModel (UserPageMsg >> dispatch)
        | Nav.AllUsersPage _ -> AllUsersPage.view model.AllUsersModel (AllUsersPageMsg >> dispatch)
        | Nav.AllProjectsPage _ -> AllProjectsPage.view model.AllProjectsModel (AllProjectsPageMsg >> dispatch)
        | Nav.NotImplementedPage -> NotImplementedPage.view model.NotImplementedModel (NotImplementedPageMsg >> dispatch)
    let root = contextProvider RootPage.userCtx (model.CurrentUser, UserLoggedIn >> dispatch) [ pageView ]
    Router.router [
        Router.onUrlChanged (dispatch << UrlChanged)
        Router.application root
    ]

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update routingView
// #if DEBUG
// |> Program.withConsoleTrace
// #endif
|> Toast.Program.withToast Notifications.renderToastWithFulma
|> Program.withReactBatched "elmish-app"
// #if DEBUG
// |> Program.withDebugger
// #endif
|> Program.run
