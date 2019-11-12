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
    | UsersFound of JsonResult<Dto.UserList>
    | SingleUserFound of JsonResult<Dto.UserDetails>
    | UserNotFound
    | LogUserResult of Dto.UserList
    | LogException of System.Exception
    | ListAllUsers
    | LogText of string

type Model = { FoundUsers : Dto.UserList; }

let init() =
    { FoundUsers = [] }, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | FindUser username ->
        let url = sprintf "/api/users/%s" username
        currentModel, Cmd.OfPromise.either Fetch.get url SingleUserFound LogException
    | ListAllUsers ->
        let url = sprintf "/api/users"
        currentModel, Cmd.OfPromise.either Fetch.get url UsersFound LogException
        // let url = sprintf "/api/healthCheck"
        // currentModel, Cmd.OfPromise.either Fetch.get<string> url LogText LogException
    | LogText text ->
        printfn "%A" text
        currentModel, Cmd.none
    | UsersFound result ->
        match toResult result with
        | Ok users ->
            let nextModel = { currentModel with FoundUsers = users }
            nextModel, Cmd.ofMsg (LogUserResult users)
        | Result.Error msg ->
            printfn "Error getting user list: %s" msg
            currentModel, Cmd.none
    | SingleUserFound result ->
        match toResult result with
        | Ok user ->
            let nextModel = { currentModel with FoundUsers = [user] }
            nextModel, Cmd.ofMsg (LogUserResult [user])
        | Result.Error msg ->
            printfn "Error finding single user: %s" msg
            currentModel, Cmd.none
    | UserNotFound ->
        currentModel, Cmd.none
    | LogUserResult users ->
        for user in users do
            printfn "Username %s, first name %s, last name %s, email(s) %A" user.username user.firstName user.lastName user.emailAddresses
        currentModel, Cmd.none
    | LogException exn ->
        let cmd = Notifications.notifyError exn.Message
        currentModel, cmd

let view (model : Model) (dispatch : Msg -> unit) =
    div [ ] [ str "This is the users page"
              br [ ]
              textInputComponent "Find user" "" (Button.button [ Button.Color IsPrimary ] [ str "Find user" ]) (dispatch << FindUser)
              br [ ]
              Button.button [ Button.Color IsPrimary; Button.OnClick (fun _ -> dispatch ListAllUsers) ] [ str "List all" ]
              ul [ ] [
                  for user in model.FoundUsers ->
                    li [ ] [ a [ Href (sprintf "#user/%s" user.username) ] [ str (user.firstName + " " + user.lastName) ] ]
              ]
            ]
