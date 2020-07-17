module Controller

open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2
open Giraffe
open Giraffe.HttpStatusCodeHandlers
open Saturn
open Shared
open Shared.Settings

// TODO: Define these two return types in Shared.fs
// TODO: Then switch all client code to expect the success/error return type
let jsonError<'a> (msg : string) : HttpHandler = json { ok = false; data = Unchecked.defaultof<'a>; message = msg }
let jsonSuccess data : HttpHandler = json { ok = true; data = data; message = "" }
let jsonResult (result : Result<'a, string>) : HttpHandler =
    match result with
    | Ok data -> jsonSuccess data
    | Error msg -> jsonError msg

let withServiceFunc (isPublic : bool) (impl : string -> 'service -> Task<'a>) (next : HttpFunc) (ctx : HttpContext) = task {
    let serviceFunction = ctx.GetService<'service>()
    let cfg = ctx |> getSettings<MySqlSettings>
    let connString = if isPublic then cfg.ConnString else cfg.ConnStringPrivate
    let! result = impl connString serviceFunction
    return! jsonSuccess result next ctx
}

let withServiceFuncWrappingExceptions (isPublic : bool) (impl : string -> 'service -> Task<'a>) (next : HttpFunc) (ctx : HttpContext) = task {
    try
        return! withServiceFunc isPublic impl next ctx
    with e ->
        return! jsonError e.Message next ctx
}

let withServiceFuncOrNotFound (isPublic : bool) (impl : string -> 'service -> Task<'a option>) (msg : string) (next : HttpFunc) (ctx : HttpContext) = task {
    let serviceFunction = ctx.GetService<'service>()
    let cfg = ctx |> getSettings<MySqlSettings>
    let connString = if isPublic then cfg.ConnString else cfg.ConnStringPrivate

    let! resultOpt = impl connString serviceFunction
    match resultOpt with
    | Some result -> return! jsonSuccess result next ctx
    | None -> return! RequestErrors.notFound (jsonError msg) next ctx
}

let withLoggedInServiceFunc (isPublic : bool) (loginCredentials : Api.LoginCredentials) (impl : string -> 'service -> Task<'a>) =
    fun (next : HttpFunc) (ctx : HttpContext) -> task {
        let verifyLoginCredentials = ctx.GetService<Model.VerifyLoginCredentials>()
        let cfg = ctx |> getSettings<MySqlSettings>
        let connString = if isPublic then cfg.ConnString else cfg.ConnStringPrivate
        let! goodLogin = verifyLoginCredentials connString loginCredentials
        if goodLogin then
            return! withServiceFunc isPublic impl next ctx
        else
            return! RequestErrors.forbidden (jsonError "Login failed") next ctx
    }

let getAllPrivateProjects : HttpHandler =
    withServiceFunc false
        (fun connString (listProjects : Model.ListProjects) -> listProjects connString)

let getPrivateProject projectCode : HttpHandler =
    withServiceFuncOrNotFound false
        (fun connString (getProject : Model.GetProject) -> getProject connString projectCode)
        (sprintf "Project code %s not found" projectCode)

let getAllPublicProjects : HttpHandler =
    withServiceFunc true
        (fun connString (listProjects : Model.ListProjects) -> listProjects connString)

let getPublicProject projectCode : HttpHandler =
    withServiceFuncOrNotFound true
        (fun connString (getProject : Model.GetProject) -> getProject connString projectCode)
        (sprintf "Project code %s not found" projectCode)

// TODO: Not in real API spec. Why not? Probably need to add it
let getUser login : HttpHandler =
    withServiceFuncOrNotFound true
        (fun connString (getUser : Model.GetUser) -> getUser connString login)
        (sprintf "Username %s not found" login)

let searchUsers searchText (loginCredentials : Api.LoginCredentials) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> task {
        let verifyLoginCredentials = ctx.GetService<Model.VerifyLoginCredentials>()
        let cfg = ctx |> getSettings<MySqlSettings>
        // let connString = if isPublic then cfg.ConnString else cfg.ConnStringPrivate  // TODO: Decide whether we want to allow searching in private LD instance as well; if so, must be admin
        let connString = cfg.ConnString
        let! goodLogin = verifyLoginCredentials connString loginCredentials
        if goodLogin then
            let (Model.IsAdmin userIsAdmin) = ctx.GetService<Model.IsAdmin>()
            let! isAdmin = userIsAdmin connString loginCredentials.username
            if isAdmin then
                return! withServiceFunc true
                    (fun connString (Model.SearchUsersLoose searchUsersLoose) -> searchUsersLoose connString searchText) next ctx
            else
                return! withServiceFunc true
                    (fun connString (Model.SearchUsersExact searchUsersExact) -> searchUsersExact connString searchText) next ctx
        else
            return! RequestErrors.forbidden (jsonError "Login failed") next ctx
    }

// TODO: Remove before going to production
let listUsers : HttpHandler =
    withServiceFunc true
        (fun connString (listUsers : Model.ListUsers) -> listUsers connString None None)

let listUsersPrivate : HttpHandler =
    withServiceFunc false
        (fun connString (listUsers : Model.ListUsers) -> listUsers connString None None)

let listUsersLimit limit : HttpHandler =
    withServiceFunc true
        (fun connString (listUsers : Model.ListUsers) -> listUsers connString (Some limit) None)

let listUsersOffset offset : HttpHandler =
    withServiceFunc true
        (fun connString (listUsers : Model.ListUsers) -> listUsers connString None (Some offset))

let listUsersLimitOffset (limit,offset) : HttpHandler =
    withServiceFunc true
        (fun connString (listUsers : Model.ListUsers) -> listUsers connString (Some limit) (Some offset))

let projectExists projectCode : HttpHandler =
    withServiceFunc true
        (fun connString (Model.ProjectExists projectExists) -> projectExists connString projectCode)

let userExists projectCode : HttpHandler =
    withServiceFunc true
        (fun connString (Model.UserExists userExists) -> userExists connString projectCode)

let projectsAndRolesByUser username (loginCredentials : Api.LoginCredentials) : HttpHandler =
    withLoggedInServiceFunc true loginCredentials
        (fun connString (projectsAndRolesByUser : Model.ProjectsAndRolesByUser) -> projectsAndRolesByUser connString username)

let legacyProjectsAndRolesByUser username (legacyLoginCredentials : Api.LegacyLoginCredentials) : HttpHandler =
    let loginCredentials : Api.LoginCredentials =
        { username = username
          password = legacyLoginCredentials.password }
    withLoggedInServiceFunc true loginCredentials
        (fun connString (legacyProjectsAndRolesByUser : Model.LegacyProjectsAndRolesByUser) -> legacyProjectsAndRolesByUser connString username)

let projectsAndRolesByUserRole username roleName (loginCredentials : Api.LoginCredentials) : HttpHandler =
    withLoggedInServiceFunc true loginCredentials
        (fun connString (projectsAndRolesByUserRole : Model.ProjectsAndRolesByUserRole) -> projectsAndRolesByUserRole connString username roleName)

let addUserToProjectWithRole (projectCode, username, roleName) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let (Model.AddMembership addMember) = ctx.GetService<Model.AddMembership>()
    let cfg = ctx |> getSettings<MySqlSettings>
    // let connString = if isPublic then cfg.ConnString else cfg.ConnStringPrivate  // TODO: Add a private version of this to the server routing list
    let connString = cfg.ConnString
    let! success = addMember connString username projectCode roleName
    let result =
        if success then
            Ok (sprintf "Added %s to %s" username projectCode)
        else
            Error (sprintf "Failed to add %s to %s" username projectCode)
    return! jsonResult result next ctx
}

let addUserToProject (projectCode, username) = addUserToProjectWithRole (projectCode,username,"Contributer")

let removeUserFromProject (projectCode, username) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let (Model.RemoveMembership removeUserFromProject) = ctx.GetService<Model.RemoveMembership>()
    let cfg = ctx |> getSettings<MySqlSettings>
    // let connString = if isPublic then cfg.ConnString else cfg.ConnStringPrivate  // TODO: Add a private version of this to the server routing list
    let connString = cfg.ConnString
    let! success = removeUserFromProject connString username projectCode
    let result =
        if success then
            Ok (sprintf "Removed %s from %s" username projectCode)
        else
            Error (sprintf "Failed to remove %s from %s" username projectCode)
    return! jsonResult result next ctx
}
let addOrRemoveUserFromProjectInternal projectCode (patchData : Api.EditProjectMembershipInternalDetails) =
    match patchData with
    | Api.EditProjectMembershipInternalDetails.AddUserRoles memberships -> RequestErrors.badRequest (json (Error "Add not yet implemented"))
    | Api.EditProjectMembershipInternalDetails.RemoveUserRoles memberships -> RequestErrors.badRequest (json (Error "Remove not yet implemented"))
    | Api.EditProjectMembershipInternalDetails.RemoveUserEntirely username -> RequestErrors.badRequest (json (Error "RemoveUser not yet implemented"))

let mapRoles (apiRoles : Api.MembershipRecordApiCall list) : Result<Api.MembershipRecordInternal list, string> =
    let roleTypes = apiRoles |> List.map (fun apiRole -> apiRole, RoleType.TryOfString apiRole.role)
    let errors = roleTypes |> List.choose (fun (apiRole, roleType) -> if roleType.IsNone then Some apiRole else None)
    if List.isEmpty errors then
        roleTypes |> List.map (fun (apiRole, roleType) -> { username = apiRole.username; role = roleType.Value } : Api.MembershipRecordInternal) |> Ok
    else
        errors |> List.map (fun apiRole -> apiRole.role) |> String.concat ", " |> sprintf "Unrecognized role names: %s" |> Error

let addOrRemoveUserFromProject projectCode (patchData : Api.EditProjectMembershipApiCall) : HttpHandler =
    match patchData.add, patchData.remove, patchData.removeUser with
    | Some _, Some _, Some _
    | Some _, Some _, None
    | Some _, None,   Some _
    | None,   Some _, Some _ ->
        RequestErrors.badRequest (json (Error "Specify exactly one of add, remove, or removeUser"))
    | Some add, None, None ->
        match mapRoles add with
        | Ok addInternal ->
            addOrRemoveUserFromProjectInternal projectCode (Api.EditProjectMembershipInternalDetails.AddUserRoles addInternal)
        | Error msg ->
            jsonError msg
    | None, Some remove, None ->
        match mapRoles remove with
        | Ok removeInternal ->
            addOrRemoveUserFromProjectInternal projectCode (Api.EditProjectMembershipInternalDetails.RemoveUserRoles removeInternal)
        | Error msg ->
            jsonError msg
    | None, None, Some removeUser ->
        addOrRemoveUserFromProjectInternal projectCode (Api.EditProjectMembershipInternalDetails.RemoveUserEntirely removeUser)
    | None, None, None ->
        RequestErrors.badRequest (json (Error "No command included in JSON: should be one of add, remove, or removeUser"))

let getAllRoles : HttpHandler =
    withServiceFunc true
        (fun connString (roleNames : Model.ListRoles) -> roleNames connString)

let createUser (user : Api.CreateUser) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    // Can't use withServiceFunc for this one since we need to do extra work in the success branch
    let (Model.UserExists userExists) = ctx.GetService<Model.UserExists>()
    let cfg = ctx |> getSettings<MySqlSettings>
    // let connString = if isPublic then cfg.ConnString else cfg.ConnStringPrivate  // TODO: Implement private version of this API endpoint in server routing list
    let connString = cfg.ConnString
    let! alreadyExists = userExists connString user.username
    if alreadyExists then
        return! jsonError "Username already exists; pick another one" next ctx
    else
        let createUser = ctx.GetService<Model.CreateUser>()
        let! newId = createUser connString user
        return! json newId next ctx
}

let upsertUser (login : string) (updateData : Api.CreateUser) =
    withServiceFunc true
        (fun connString (upsertUser : Model.UpsertUser) -> upsertUser connString login updateData)

let changePassword login (updateData : Api.ChangePassword) =
    withServiceFunc true
        (fun connString (changePassword : Model.ChangePassword) -> changePassword connString login updateData)

// NOTE: We don't do any work behind the scenes to reconcile MySQL and Mongo passwords; that's up to Language Forge
let verifyPassword (loginCredentials : Api.LoginCredentials) =
    withServiceFunc true
        (fun connString (verifyLoginInfo : Model.VerifyLoginCredentials) -> verifyLoginInfo connString loginCredentials)

let createProject (proj : Api.CreateProject) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    // Can't use withServiceFunc for this one since we need to tweak the return value in the success branch
    let (Model.ProjectExists projectExists) = ctx.GetService<Model.ProjectExists>()
    let cfg = ctx |> getSettings<MySqlSettings>
    // let connString = if isPublic then cfg.ConnString else cfg.ConnStringPrivate  // TODO: Implement private version of this API endpoint in server routing list
    let connString = cfg.ConnString
    let! alreadyExists = projectExists connString proj.code
    if alreadyExists then
        return! jsonError "Project code already exists; pick another one" next ctx
    else
        return! withServiceFunc true (fun connString (createProject : Model.CreateProject) -> createProject connString proj) next ctx
}

let countUsers : HttpHandler =
    withServiceFunc true
        (fun connString (Model.CountUsers countUsers) -> task {
                do! Async.Sleep 500 // Simulate server load
                return! countUsers connString
        })

let countProjects : HttpHandler =
    withServiceFunc true
        (fun connString (Model.CountProjects countProjects) -> task {
                do! Async.Sleep 750 // Simulate server load
                return! countProjects connString
        })

let countRealProjects : HttpHandler =
    withServiceFunc true
        (fun connString (Model.CountRealProjects countRealProjects) -> task {
                do! Async.Sleep 1000 // Simulate server load
                return! countRealProjects connString
        })

let archiveProject projectCode : HttpHandler =
    withServiceFunc true
        (fun connString (archiveProject : Model.ArchiveProject) -> archiveProject connString projectCode)

let archivePrivateProject projectCode : HttpHandler =
    withServiceFunc false
        (fun connString (archiveProject : Model.ArchiveProject) -> archiveProject connString projectCode)
