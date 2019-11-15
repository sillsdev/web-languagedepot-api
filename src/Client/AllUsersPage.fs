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
    | FindUser of string
    | SearchUsers of string * bool
    | UsersFound of JsonResult<Dto.UserList> * int
    | SingleUserFound of JsonResult<Dto.UserDetails>
    | UserNotFound
    | LogUserResult of Dto.UserList
    | LogException of System.Exception
    | ListAllUsers
    | ListUsersLimitOffset of int*int*int
    | LogText of string
    | ToggleAdmin
    | UserCountLoaded of JsonResult<int>
    | PaginationMsg of int
    | SetCurrentPage of int

type Model = { FoundUsers : Dto.UserList; UsersPerPage : int; UserCount : int; CurrentPage : int; IsAdmin : bool }

let init() =
    { FoundUsers = []; UsersPerPage = 2; UserCount = 0; CurrentPage = 1; IsAdmin = false }, Cmd.OfPromise.perform Fetch.get "/api/count/users" UserCountLoaded

let unpackJsonResult currentModel jsonResult fn =
        match toResult jsonResult with
        | Ok newData ->
            fn newData
        | Result.Error msg ->
            currentModel, Notifications.notifyError msg

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | ToggleAdmin ->
        let nextModel = { currentModel with IsAdmin = not currentModel.IsAdmin }
        nextModel, Cmd.none
    | UserCountLoaded result ->
        unpackJsonResult currentModel result (fun n ->
            let nextModel = { currentModel with UserCount = n }
            nextModel, Cmd.none)
    | PaginationMsg newCurrentPage ->
            let nextModel = { currentModel with CurrentPage = newCurrentPage }
            // TODO: Cache results so we don't have to wait for the database to load the next page if we already had it in memory
            nextModel, Cmd.ofMsg (ListUsersLimitOffset (currentModel.UsersPerPage, (newCurrentPage - 1) * currentModel.UsersPerPage, newCurrentPage))
    | SetCurrentPage newCurrentPage ->
            let nextModel = { currentModel with CurrentPage = newCurrentPage }
            nextModel, Cmd.none
    | FindUser username ->
        let url = sprintf "/api/users/%s" username
        currentModel, Cmd.OfPromise.either Fetch.get url SingleUserFound LogException
    | SearchUsers (searchText, asAdmin) ->
        let url = sprintf "/api/searchUsers/%s" searchText
        let loggedInUser = "test"  // TODO: Once we implement the login page properly, get the currently logged in user here instead (and get rid of the IsAdmin checkbox)
        let username = if asAdmin then "admin" else loggedInUser
        let login : Api.LoginCredentials = { username = username; password = "x" }
        currentModel, Cmd.OfPromise.either (fun data -> Fetch.post(url,data)) login (fun result -> UsersFound(result, 1)) LogException
    | ListAllUsers ->
        let url = sprintf "/api/users/limit/%d" currentModel.UsersPerPage
        currentModel, Cmd.OfPromise.either Fetch.get url UsersFound LogException
    | ListUsersLimitOffset (limit,offset,newCurrentPage) ->
        let url = sprintf "/api/users/limit/%d/offset/%d" limit offset
        currentModel, Cmd.OfPromise.either Fetch.get url (fun result -> UsersFound (result,newCurrentPage)) LogException
    | LogText text ->
        printfn "%A" text
        currentModel, Cmd.none
    | UsersFound (result,newCurrentPage) ->
        unpackJsonResult currentModel result (fun users ->
            let nextModel = { currentModel with FoundUsers = users }
            nextModel, Cmd.ofMsg (LogUserResult users))
    | SingleUserFound result ->
        unpackJsonResult currentModel result (fun user ->
            let nextModel = { currentModel with FoundUsers = [user] }
            nextModel, Cmd.ofMsg (LogUserResult [user]))
    | UserNotFound ->
        currentModel, Cmd.none
    | LogUserResult users ->
        for user in users do
            printfn "Username %s, first name %s, last name %s, email(s) %A" user.username user.firstName user.lastName user.emailAddresses
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
                // TODO: Tweak this so it looks good in all scenarios
                if pageCount <= 5 then
                    [ for i = 1 to pageCount do yield pageLink i ]
                elif currentPage <= 3 then
                    [ for i = 1 to 3 do yield pageLink i
                      yield Pagination.ellipsis [ ]
                      yield pageLink pageCount ]
                elif pageCount - currentPage <= 2 then
                    [ yield pageLink 1
                      yield Pagination.ellipsis [ ]
                      for i = currentPage - 2 to pageCount do yield pageLink i ]
                else
                    [ yield pageLink 1
                      if (currentPage > 4) then yield Pagination.ellipsis [ ]
                      for i in [ currentPage - 2 .. (currentPage + 2 |> min pageCount) ] do yield pageLink i
                      if (pageCount - currentPage > 3) then yield Pagination.ellipsis [ ]
                      yield pageLink pageCount ]
            )
        ]

let userSearch (model : Model) (dispatch : Msg -> unit) =
    textInputComponent "Find user" "" (Button.button [ Button.Color IsPrimary ] [ str "Find user" ]) (fun text -> dispatch (SearchUsers(text, model.IsAdmin)))

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ] [ str "This is the users page"
              br [ ]
              Checkbox.input [ Props [ Checked model.IsAdmin; OnClick (fun evt -> dispatch ToggleAdmin ) ] ]
              Checkbox.checkbox [ Props [ Checked model.IsAdmin; OnClick (fun evt -> dispatch ToggleAdmin ) ] ]  [ str "Logged in as admin (test mode) "]
              br [ ]
              userSearch model dispatch
              br [ ]
              Button.button [ Button.Color IsPrimary; Button.OnClick (fun _ -> dispatch ListAllUsers) ] [ str "List all" ]
              pagination dispatch model.UserCount model.UsersPerPage model.CurrentPage
              ul [ ] [
                  for user in model.FoundUsers ->
                    li [ ] [ a [ Href (sprintf "#user/%s" user.username) ] [ str (user.firstName + " " + user.lastName) ] ]
              ]
            ]
