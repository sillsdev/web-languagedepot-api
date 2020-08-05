module AllUsersPage

open Browser
open Elmish
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open Fulma
open Thoth.Elmish.Toast
open Thoth.Fetch
open Thoth.Json

open TextInput
open Shared
open Shared.Api
open JsonHelpers

type Msg =
    | SearchUsers of string * bool
    | UsersFound of JsonResult<Dto.UserList> * int
    | UserSearchResults of JsonResult<Dto.UserList>
    | UserNotFound
    | LogUserResult of Dto.UserList
    | LogException of System.Exception
    | ListAllUsers
    | ListUsersSinglePage of int
    | LogText of string
    | ToggleAdmin
    | UserCountLoaded of JsonResult<int>
    | PaginationMsg of int
    | SetCurrentPage of int
    | ChangeUsersPerPage of int

type Model = { FoundUsers : Dto.UserList; SearchResults : Dto.UserList; UsersPerPage : int; UserCount : int; CurrentPage : int; IsAdmin : bool }

let init() =
    { FoundUsers = [||]; SearchResults = [||]; UsersPerPage = 25; UserCount = 0; CurrentPage = 1; IsAdmin = false },
    Cmd.OfPromise.perform Fetch.get "/api/count/users" UserCountLoaded

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    // TODO: Pull pagination stuff out into a component; we'll want to reuse it between AllUsers and AllProjects
    | ChangeUsersPerPage newUsersPerPage ->
        let oldOffset = (currentModel.CurrentPage - 1) * currentModel.UsersPerPage
        let newCurrentPage = (oldOffset / newUsersPerPage) + 1
        let nextModel = { currentModel with UsersPerPage = newUsersPerPage }
        printfn "Old offset %d, new current page %d (old users per page %d and new %d)" oldOffset newCurrentPage currentModel.UsersPerPage newUsersPerPage
        nextModel, Cmd.ofMsg (ListUsersSinglePage newCurrentPage)
    | ToggleAdmin ->
        let nextModel = { currentModel with IsAdmin = not currentModel.IsAdmin }
        nextModel, Cmd.none
    | UserCountLoaded result ->
        unpackJsonResult currentModel result (fun n ->
            let nextModel = { currentModel with UserCount = n }
            nextModel, Cmd.none)
    | PaginationMsg newCurrentPage ->
        // We won't update the current page number in the model until the results come back
        currentModel, Cmd.ofMsg (ListUsersSinglePage newCurrentPage)
    | SetCurrentPage newCurrentPage ->
        let nextModel = { currentModel with CurrentPage = newCurrentPage }
        nextModel, Cmd.none
    | SearchUsers (searchText, asAdmin) ->
        let url = sprintf "/api/searchUsers/%s" searchText
        let loggedInUser = "test"  // TODO: Once we implement the login page properly, get the currently logged in user here instead (and get rid of the IsAdmin checkbox)
        let username = if asAdmin then "admin" else loggedInUser
        let login : Api.LoginCredentials = { username = username; password = "x" }
        currentModel, Cmd.OfPromise.either (fun data -> Fetch.post(url,data)) login UserSearchResults LogException
    | UserSearchResults result ->
        unpackJsonResult currentModel result (fun users ->
            let nextModel = { currentModel with SearchResults = users }
            nextModel, Cmd.ofMsg (LogUserResult users))
    | ListAllUsers ->
        currentModel, Cmd.ofMsg (ListUsersSinglePage 1)
    | ListUsersSinglePage (newCurrentPage) ->
        let limit = currentModel.UsersPerPage
        let offset = (newCurrentPage - 1) * limit
        let url = sprintf "/api/users/limit/%d/offset/%d" limit offset
        // We won't update the current page number in the model until the results come back
        currentModel, Cmd.OfPromise.either Fetch.get url (fun result -> UsersFound (result,newCurrentPage)) LogException
    | LogText text ->
        printfn "%A" text
        currentModel, Cmd.none
    | UsersFound (result,newCurrentPage) ->
        unpackJsonResult currentModel result (fun users ->
            let nextModel = { currentModel with FoundUsers = users; CurrentPage = newCurrentPage }
            nextModel, Cmd.ofMsg (LogUserResult users))  // TODO: Get rid of this logging now that the user search is working well
    | UserNotFound ->
        currentModel, Cmd.none
    | LogUserResult users ->
        for user in users do
            printfn "Username %s, first name %s, last name %s, email(s) %A" user.username user.firstName user.lastName user.email
        currentModel, Cmd.none
    | LogException exn ->
        let cmd = Notifications.notifyError exn.Message
        currentModel, cmd

let pagination (dispatch : Msg -> unit) itemCount itemsPerPage currentPage =
    let pageCount = itemCount / itemsPerPage + (if itemCount % itemsPerPage = 0 then 0 else 1)
    printfn "Paginating: %d pages" pageCount
    if pageCount <= 1
    then nav [] []
    else
        let pageLink n =
            let props = if n = currentPage
                        then Pagination.Link.Current (n = currentPage)
                        else Pagination.Link.Props [ OnClick (fun _ -> dispatch (PaginationMsg n))]
            Pagination.link [ props ] [ str (n.ToString()) ]
        let nextProps = if currentPage >= pageCount then [] else [ Props [ OnClick (fun _ -> dispatch (PaginationMsg (currentPage + 1))) ] ]
        let prevProps = if currentPage <= 1 then [] else [ Props [ OnClick (fun _ -> dispatch (PaginationMsg (currentPage - 1))) ] ]
        Pagination.pagination [ Pagination.IsCentered ] [
            Pagination.previous prevProps [ str "«" ]
            Pagination.next nextProps [ str "»" ]
            Pagination.list [ ] (
                if pageCount <= 5 then
                    [ for i = 1 to pageCount do yield pageLink i ]
                elif currentPage <= 3 then
                    [ for i = 1 to 5 do yield pageLink i
                      if pageCount > 6 then yield Pagination.ellipsis [ ]
                      yield pageLink pageCount ]
                elif pageCount - currentPage <= 2 then
                    [ yield pageLink 1
                      if pageCount > 6 then yield Pagination.ellipsis [ ]
                      for i = pageCount - 4 to pageCount do yield pageLink i ]
                else
                    [ yield pageLink 1
                      if (currentPage > 4) then yield Pagination.ellipsis [ ]
                      for i in [ currentPage - 2 .. (currentPage + 2 |> min pageCount) ] do yield pageLink i
                      if (pageCount - currentPage > 3) then yield Pagination.ellipsis [ ]
                      yield pageLink pageCount ]
            )
        ]

let userSearch (model : Model) (dispatch : Msg -> unit) =
    textInputComponent "Find user" "" (str "Find user") (fun text -> dispatch (SearchUsers(text, model.IsAdmin)))

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ] [ str "This is the users page"
              br [ ]
              Checkbox.input [ Props [ Checked model.IsAdmin; OnClick (fun evt -> dispatch ToggleAdmin ) ] ]
              Checkbox.checkbox [ Props [ Checked model.IsAdmin; OnClick (fun evt -> dispatch ToggleAdmin ) ] ]  [ str "Logged in as admin (test mode) "]
              br [ ]
              userSearch model dispatch
              br [ ]
              ul [ ] [
                  for user in model.SearchResults ->
                    li [ ] [ a [ Href (sprintf "#user/%s" user.username) ] [ str (user.firstName + " " + user.lastName) ] ]
              ]
              br [ ]
              if (model.IsAdmin) then
                  Button.button [ Button.Color IsPrimary; Button.OnClick (fun _ -> dispatch ListAllUsers) ] [ str "List all" ]
                  ul [ ] [
                      for user in model.FoundUsers ->
                        li [ ] [ a [ Href (sprintf "#user/%s" user.username) ] [ str (user.firstName + " " + user.lastName) ] ]
                  ]
                  pagination dispatch model.UserCount model.UsersPerPage model.CurrentPage
                  br [ ]
                  str "Items per page: "
                  Select.select [ ] [
                      select
                        [ Value model.UsersPerPage
                          OnChange (fun evt ->
                              match System.Int32.TryParse evt.Value with
                              | false, _ -> ()
                              | true, value -> dispatch (ChangeUsersPerPage value) ) ]
                        [ option [ Value "1"; Selected (model.UsersPerPage = 1) ] [ str "1" ]
                          option [ Value "2"; Selected (model.UsersPerPage = 2) ] [ str "2" ]
                          option [ Value "3"; Selected (model.UsersPerPage = 3) ] [ str "3" ]
                          option [ Value "5"; Selected (model.UsersPerPage = 5) ] [ str "5" ]
                          option [ Value "10"; Selected (model.UsersPerPage = 10) ] [ str "10" ]
                          option [ Value "25"; Selected (model.UsersPerPage = 25) ] [ str "25" ]
                          option [ Value "50"; Selected (model.UsersPerPage = 50) ] [ str "50" ]
                          option [ Value "100"; Selected (model.UsersPerPage = 100) ] [ str "100" ]
                      ]
                  ]
            ]
