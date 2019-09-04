module RootPage

open Elmish
open Fable.FontAwesome
open Fable.FontAwesome.Free
open Fable.React
open Fable.React.Props
open Fulma
open Thoth.Fetch

open Shared
open JsonHelpers

type Msg =
    | RefreshCounts
    | ProjectCountLoaded of JsonResult<int>
    | RealProjectCountLoaded of JsonResult<int>
    | UserCountLoaded of JsonResult<int>

type Model = { ProjectCount : int option
               RealProjectCount : int option
               UserCount : int option }

let init() =
    let initialModel = { ProjectCount = None
                         RealProjectCount = None
                         UserCount = None }
    initialModel, Cmd.ofMsg RefreshCounts

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | RefreshCounts ->
        let cmds = Cmd.batch [
            Cmd.OfPromise.perform Fetch.get "/api/count/projects" ProjectCountLoaded
            Cmd.OfPromise.perform Fetch.get "/api/count/non-test-projects" RealProjectCountLoaded
            Cmd.OfPromise.perform Fetch.get "/api/count/users" UserCountLoaded
        ]
        currentModel, cmds
    | ProjectCountLoaded jsonResult ->
        match toResult jsonResult with
        | Ok n ->
            { currentModel with ProjectCount = Some n}, Cmd.none
        | Error msg ->
            printfn "Error loading project count: %s" msg  // Just log to console, don't notify user
            currentModel, Cmd.none
    | RealProjectCountLoaded jsonResult ->
        match toResult jsonResult with
        | Ok n ->
            { currentModel with RealProjectCount = Some n}, Cmd.none
        | Error msg ->
            printfn "Error loading real project count: %s" msg  // Just log to console, don't notify user
            currentModel, Cmd.none
    | UserCountLoaded jsonResult ->
        match toResult jsonResult with
        | Ok n ->
            { currentModel with UserCount = Some n}, Cmd.none
        | Error msg ->
            printfn "Error loading user project count: %s" msg  // Just log to console, don't notify user
            currentModel, Cmd.none

let userCtx : IContext<SharedUser option * (SharedUser option -> unit)> = createContext (None, ignore)

let Avatar = FunctionComponent.Of (fun (props : {| user : SharedUser |}) ->
    let name = if isNull props.user.Name then "" else props.user.Name
    let email = if isNull props.user.Email then "" else props.user.Email
    let emailForGravatar = email.Trim().ToLowerInvariant()
    let hash = Hashes.md5 emailForGravatar
    let url = "https://www.gravatar.com/avatar/" + hash + "?d=mp"
    img [ Src url; Style [ MaxHeight "none"] ]
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

let NavbarUserCard = FunctionComponent.Of (fun (props : {|dispatch : Msg -> unit|}) ->
    let currentUser, setUser = Hooks.useContext userCtx
    match currentUser with
    | None -> p [ ] [ a [ Href "#login"; Style [ Color "white" ] ] [ str "Please log in" ] ]
    | Some user ->
    Navbar.Item.div
        [ Navbar.Item.HasDropdown
          Navbar.Item.IsHoverable ]
        [ Navbar.Link.a [ ] [ userCard user ]
          Navbar.Dropdown.div [ ]
            [ Navbar.Item.a [ Navbar.Item.Props [ OnClick (fun _ -> setUser None) ] ] [ str "Log out" ] ] ]
)

let ShowUsername = FunctionComponent.Of (fun () ->
    let userCtx, _ = Hooks.useContext userCtx
    match userCtx with
    | None -> p [ ] [ str "Hello, Admin" ]
    | Some user -> p [ ] [ str ("Hello, " + user.Name) ]
)

let showInt = function
    | None -> Fa.i [ Fa.Solid.Spinner; Fa.Spin ] []
    | Some n -> str (n.ToString())

let navBrand dispatch =
    Navbar.navbar [ Navbar.Color IsInfo ]
        [ Container.container [ ]
            [ Navbar.Brand.div [ ]
                [ Navbar.Item.a [ Navbar.Item.Props [ Href "#" ]
                                  Navbar.Item.CustomClass "brand-text" ]
                      [ str "Language Depot Admin" ] ]
              Navbar.menu [ ]
                  [ Navbar.Start.div [ ]
                      [ Navbar.Item.a [ Navbar.Item.Props [ Href "#not-implemented" ] ]
                            [ str "Projects" ]
                        Navbar.Item.a [ Navbar.Item.Props [ Href "#not-implemented" ] ]
                            [ str "Users" ] ] ]
              NavbarUserCard {|dispatch=dispatch|} ] ]

let menu =
    Menu.menu [ ]
        [ Menu.label [ ]
              [ str "General" ]
          Menu.list [ ]
              [ Menu.Item.a [ Menu.Item.Props [ Href "#not-implemented" ] ]
                    [ str "Projects" ]
                Menu.Item.a [ Menu.Item.Props [ Href "#not-implemented" ] ]
                    [ str "Users" ] ]
          Menu.label [ ]
              [ str "Administration" ]
          Menu.list [ ]
              [ Menu.Item.a [ Menu.Item.Props [ Href "#project/create" ] ]
                  [ str "Create project" ]
                li [ ]
                    [ a [ ]
                        [ str "Manage Project" ]
                      Menu.list [ ]
                          [ Menu.Item.a [ Menu.Item.Props [ Href "#not-implemented" ] ]
                                [ str "Members" ]
                            Menu.Item.a [ Menu.Item.Props [ Href "#not-implemented" ] ]
                                [ str "Project Profile" ]
                            Menu.Item.a [ Menu.Item.Props [ Href "#not-implemented" ] ]
                                [ str "Add a member" ]
                            Menu.Item.a [ Menu.Item.Props [ Href "#not-implemented" ] ]
                                [ str "Remove a member" ] ] ]
                Menu.Item.a [ Menu.Item.Props [ Href "#not-implemented" ] ]
                    [ str "Invitations (TODO)" ] ] ]

let breadcrumb =
    Breadcrumb.breadcrumb [ ]
        [ Breadcrumb.item [ ]
              [ a [ Href "#not-implemented" ] [ str "Language Depot" ] ]
          Breadcrumb.item [ ]
              [ a [ Href "#not-implemented" ] [ str "Admin" ] ]
          Breadcrumb.item [ Breadcrumb.Item.IsActive true ]
              [ a [ Href "#not-implemented" ] [ str "Root page" ] ] ]

let hero dispatch =
    Hero.hero [ Hero.Color IsInfo
                Hero.CustomClass "welcome" ]
        [ Hero.body [ ]
            [ Container.container [ ]
                [ Heading.h1 [ ]
                      [ ShowUsername() ] ] ] ]

let info (model : Model) =
    section [ Class "info-tiles" ]
        [ Tile.ancestor [ Tile.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
            [ Tile.parent [ ]
                  [ Tile.child [ ]
                      [ Box.box' [ ]
                          [ Heading.p [ ]
                                [ showInt model.UserCount ]
                            Heading.p [ Heading.IsSubtitle ]
                                [ str "Users" ] ] ] ]
              Tile.parent [ ]
                  [ Tile.child [ ]
                      [ Box.box' [ ]
                          [ Heading.p [ ]
                                [ showInt model.ProjectCount ]
                            Heading.p [ Heading.IsSubtitle ]
                                [ str "Projects" ] ] ] ]
              Tile.parent [ ]
                  [ Tile.child [ ]
                      [ Box.box' [ ]
                          [ Heading.p [ ]
                                [ showInt model.RealProjectCount ]
                            Heading.p [ Heading.IsSubtitle ]
                                [ str "Real projects" ] ] ] ] ] ]

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
                                      [ Fa.i [Fa.Solid.Search] [ ] ]
                                  Icon.icon
                                      [ Icon.Size IsMedium
                                        Icon.IsRight ]
                                      [ Fa.i [Fa.Solid.Check] [ ] ] ] ] ] ]
                Card.card [ ]
                  [ Card.header [ ]
                      [ Card.Header.title [ ]
                          [ str "Projects" ]
                        Card.Header.icon [ ]
                            [ Icon.icon [ ]
                                [ Fa.i [Fa.Solid.AngleDown] [] ] ] ]
                    Card.content [ ]
                      [ Content.content [ ]
                          [ ] ] ] ]
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
                                  [ for user in [] ->  // Was: for user in model.UserList
                                      tr [ ]
                                          [ td [ Style [ Width "5%" ] ]
                                              [ Icon.icon
                                                  [ ]
                                                  [ Fa.i [ Fa.Solid.User ] [] ] ]
                                            td [ ]
                                                [ str user ]
                                            td [ ]
                                                [ ] ] ] ] ] ]
                  Card.footer [ ]
                      [ Card.Footer.div [ ]
                          [ ] ] ] ] ]

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ]
        [ navBrand dispatch
          Container.container [ ]
              [ Columns.columns [ ]
                  [ Column.column [ Column.Width (Screen.All, Column.Is3) ]
                      [ menu ]
                    Column.column [ Column.Width (Screen.All, Column.Is9) ]
                      [ breadcrumb
                        hero dispatch
                        info model
                        columns model dispatch ] ] ] ]
