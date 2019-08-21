module RootPage

open Elmish
open Fable.FontAwesome
open Fable.FontAwesome.Free
open Fable.React
open Fable.React.Props
open Fulma

open Shared

type Msg =
    | UserLoggedOut

type Model = { nothing : unit }

let init() = { nothing = () }

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | UserLoggedOut ->
        currentModel, Cmd.none  // Handled by Client.fs  // TODO: Better design

let userCtx : IContext<SharedUser option> = createContext None

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
    let currentUser = Hooks.useContext userCtx
    match currentUser with
    | None -> p [ ] [ a [ Href "#login"; Style [ Color "white" ] ] [ str "Please log in" ] ]
    | Some user ->
    Navbar.Item.div
        [ Navbar.Item.HasDropdown
          Navbar.Item.IsHoverable ]
        [ Navbar.Link.a [ ] [ userCard user ]
          Navbar.Dropdown.div [ ]
            [ Navbar.Item.a [ Navbar.Item.Props [ OnClick (fun _ -> props.dispatch UserLoggedOut) ] ] [ str "Log out" ] ] ]
)

let ShowUsername = FunctionComponent.Of (fun (props : {|dispatch : Msg -> unit|}) ->
    let userCtx = Hooks.useContext userCtx
    match userCtx with
    | None -> p [ ] [ str "Hello, Admin" ]
    | Some user -> p [ ] [ str ("Hello, " + user.Name) ]
)

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

let navBrand dispatch =
    Navbar.navbar [ Navbar.Color IsInfo ]
        [ Container.container [ ]
            [ Navbar.Brand.div [ ]
                [ Navbar.Item.a [ Navbar.Item.CustomClass "brand-text" ]
                      [ str "SAFE Admin" ] ]
              Navbar.menu [ ]
                  [ Navbar.Start.div [ ]
                      [ Navbar.Item.a [ ]
                            [ str "Projects" ]
                        Navbar.Item.a [ ]
                            [ str "Users" ] ] ]
              NavbarUserCard {|dispatch=dispatch|} ] ]

let menu =
    Menu.menu [ ]
        [ Menu.label [ ]
              [ str "General" ]
          Menu.list [ ]
              [ Menu.Item.a [ ]
                    [ str "Projects" ]
                Menu.Item.a [ ]
                    [ str "Users" ] ]
          Menu.label [ ]
              [ str "Administration" ]
          Menu.list [ ]
              [ Menu.Item.a [ ]
                  [ str "Create project" ]
                li [ ]
                    [ a [ ]
                        [ str "Manage Project" ]
                      Menu.list [ ]
                          [ Menu.Item.a [ ]
                                [ str "Members" ]
                            Menu.Item.a [ ]
                                [ str "Project Profile" ]
                            Menu.Item.a [ ]
                                [ str "Add a member" ]
                            Menu.Item.a [ ]
                                [ str "Remove a member" ] ] ]
                Menu.Item.a [ ]
                    [ str "Invitations (TODO)" ] ] ]

let breadcrump =
    Breadcrumb.breadcrumb [ ]
        [ Breadcrumb.item [ ]
              [ a [ ] [ str "Language Depot" ] ]
          Breadcrumb.item [ ]
              [ a [ ] [ str "Admin" ] ]
          Breadcrumb.item [ Breadcrumb.Item.IsActive true ]
              [ a [ ] [ str "Root page" ] ] ]

let hero dispatch =
    Hero.hero [ Hero.Color IsInfo
                Hero.CustomClass "welcome" ]
        [ Hero.body [ ]
            [ Container.container [ ]
                [ Heading.h1 [ ]
                      [ ShowUsername {|dispatch=dispatch|} ]
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
                              Button.OnClick ignore ] // Was: (fun _ -> dispatch ListAllUsers)
                            [ str "All users" ]
                          Button.button
                            [ Button.Color IsInfo
                              Button.OnClick ignore ] // Was: (fun _ -> dispatch ListAllProjects)
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
                      [ breadcrump
                        hero dispatch
                        info
                        columns model dispatch ] ] ] ]
