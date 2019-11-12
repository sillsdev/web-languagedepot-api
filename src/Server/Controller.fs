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
let jsonError (msg : string) : HttpHandler = json { ok = false; message = msg }
let jsonSuccess data : HttpHandler = json { ok = true; data = data }
let jsonResult (result : Result<'a, string>) : HttpHandler =
    match result with
    | Ok data -> jsonSuccess data
    | Error msg -> jsonError msg

let withServiceFunc (impl : 'service -> Async<'a>) (next : HttpFunc) (ctx : HttpContext) = task {
    let serviceFunction = ctx.GetService<'service>()
    let! result = impl serviceFunction
    return! jsonSuccess result next ctx
}

let withServiceFuncWrappingExceptions (impl : 'service -> Async<'a>) (next : HttpFunc) (ctx : HttpContext) = task {
    try
        return! withServiceFunc impl next ctx
    with e ->
        return! jsonError e.Message next ctx
}

let withServiceFuncOrNotFound (impl : 'service -> Async<'a option>) (msg : string) (next : HttpFunc) (ctx : HttpContext) = task {
    let serviceFunction = ctx.GetService<'service>()

    let! resultOpt = impl serviceFunction
    match resultOpt with
    | Some result -> return! jsonSuccess result next ctx
    | None -> return! RequestErrors.notFound (jsonError msg) next ctx
}

let withLoggedInServiceFunc (loginCredentials : Api.LoginCredentials) (impl : 'service -> Async<'a>) =
    fun (next : HttpFunc) (ctx : HttpContext) -> task {
        let verifyLoginCredentials = ctx.GetService<Model.VerifyLoginCredentials>()
        let! goodLogin = verifyLoginCredentials loginCredentials
        if goodLogin then
            return! withServiceFunc impl next ctx
        else
            return! RequestErrors.forbidden (jsonError "Login failed") next ctx
    }

let getAllPrivateProjects : HttpHandler =
    withServiceFunc
        (fun (listProjects : Model.ListProjects) -> listProjects false)

let getPrivateProject projectCode : HttpHandler =
    withServiceFuncOrNotFound
        (fun (getProject : Model.GetProject) -> getProject false projectCode)
        (sprintf "Project code %s not found" projectCode)

let getAllPublicProjects : HttpHandler =
    withServiceFunc
        (fun (listProjects : Model.ListProjects) -> listProjects true)

let getPublicProject projectCode : HttpHandler =
    withServiceFuncOrNotFound
        (fun (getProject : Model.GetProject) -> getProject true projectCode)
        (sprintf "Project code %s not found" projectCode)

// TODO: Not in real API spec. Why not? Probably need to add it
let getUser login : HttpHandler =
    withServiceFuncOrNotFound
        (fun (getUser : Model.GetUser) -> getUser login)
        (sprintf "Username %s not found" login)

// TODO: Remove before going to production
let listUsers : HttpHandler =
    withServiceFunc
        (fun (listUsers : Model.ListUsers) -> listUsers())

let projectExists projectCode : HttpHandler =
    withServiceFunc
        (fun (Model.ProjectExists projectExists) -> projectExists projectCode)

let userExists projectCode : HttpHandler =
    withServiceFunc
        (fun (Model.UserExists userExists) -> userExists projectCode)

let projectsAndRolesByUser username (loginCredentials : Api.LoginCredentials) : HttpHandler =
    withLoggedInServiceFunc loginCredentials
        (fun (projectsAndRolesByUser : Model.ProjectsAndRolesByUser) -> projectsAndRolesByUser username)

let projectsAndRolesByUserRole username roleName (loginCredentials : Api.LoginCredentials) : HttpHandler =
    match RoleType.TryOfString roleName with
    | None -> jsonError (sprintf "Unrecognized role name %s" roleName)
    | Some roleType ->
        withLoggedInServiceFunc loginCredentials
            (fun (projectsAndRolesByUserRole : Model.ProjectsAndRolesByUserRole) -> projectsAndRolesByUserRole username roleType)

let addUserToProjectWithRoleType (projectCode, username, roleType) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let (Model.AddMembership addMember) = ctx.GetService<Model.AddMembership>()
    let! success = addMember username projectCode roleType
    let result =
        if success then
            Ok (sprintf "Added %s to %s" username projectCode)
        else
            Error (sprintf "Failed to add %s to %s" username projectCode)
    return! jsonResult result next ctx
}

let addUserToProjectWithRole (projectCode, username, roleName) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    match RoleType.TryOfString roleName with
    | None -> return! jsonError (sprintf "Unrecognized role name %s" roleName) next ctx
    | Some roleType ->
        return! addUserToProjectWithRoleType (projectCode,username,roleType) next ctx
}

let addUserToProject (projectCode, username) = addUserToProjectWithRoleType (projectCode,username,Contributor)

let removeUserFromOneRoleInProject (projectCode, username, roleName) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let (Model.RemoveMembership removeMember) = ctx.GetService<Model.RemoveMembership>()
    match RoleType.TryOfString roleName with
    | None -> return! jsonError (sprintf "Unrecognized role name %s" roleName) next ctx
    | Some roleType ->
        let! success = removeMember username projectCode roleType  // TODO: Add the "removeUserFromAllRolesInProject" function (or whatever I want to call it) mentioned in a TODO in Model.fs
        let result =
            if success then
                Ok (sprintf "Removed %s from role %s in %s" username roleName projectCode)
            else
                Error (sprintf "Failed to remove %s from role %s in %s" username roleName projectCode)
        return! jsonResult result next ctx
}

let removeUserFromAllRolesInProject (projectCode, username) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let (Model.RemoveUserFromAllRolesInProject removeUserFromAllRolesInProject) = ctx.GetService<Model.RemoveUserFromAllRolesInProject>()
    let! success = removeUserFromAllRolesInProject username projectCode
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
    withServiceFunc
        (fun (roleNames : Model.ListRoles) -> roleNames())

let createUser (user : Api.CreateUser) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    // Can't use withServiceFunc for this one since we need to do extra work in the success branch
    let (Model.UserExists userExists) = ctx.GetService<Model.UserExists>()
    let! alreadyExists = userExists user.username
    if alreadyExists then
        return! jsonError "Username already exists; pick another one" next ctx
    else
        let createUser = ctx.GetService<Model.CreateUser>()
        let! newId = createUser user
        return! json newId next ctx
}

let upsertUser (login : string) (updateData : Api.CreateUser) =
    withServiceFunc
        (fun (upsertUser : Model.UpsertUser) -> upsertUser login updateData)

let changePassword login (updateData : Api.ChangePassword) =
    withServiceFunc
        (fun (changePassword : Model.ChangePassword) -> changePassword login updateData)

// NOTE: We don't do any work behind the scenes to reconcile MySQL and Mongo passwords; that's up to Language Forge
let verifyPassword username (loginCredentials : Api.LoginCredentials) =
    withServiceFunc
        (fun (verifyLoginInfo : Model.VerifyLoginCredentials) -> verifyLoginInfo loginCredentials)

let createProject (proj : Api.CreateProject) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    // Can't use withServiceFunc for this one since we need to tweak the return value in the success branch
    let (Model.ProjectExists projectExists) = ctx.GetService<Model.ProjectExists>()
    let! alreadyExists = projectExists proj.code
    if alreadyExists then
        return! jsonError "Project code already exists; pick another one" next ctx
    else
        return! withServiceFunc (fun (createProject : Model.CreateProject) -> createProject proj) next ctx
}

let countUsers : HttpHandler =
    withServiceFunc
        (fun (Model.CountUsers countUsers) -> async {
                do! Async.Sleep 500 // Simulate server load
                return! countUsers()
        })

let countProjects : HttpHandler =
    withServiceFunc
        (fun (Model.CountProjects countProjects) -> async {
                do! Async.Sleep 750 // Simulate server load
                return! countProjects()
        })

let countRealProjects : HttpHandler =
    withServiceFunc
        (fun (Model.CountRealProjects countRealProjects) -> async {
                do! Async.Sleep 1000 // Simulate server load
                return! countRealProjects()
        })

let getMySqlSettings : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let cfg = ctx |> getSettings<MySqlSettings>
    return! json cfg next ctx
}

let archiveProject projectCode : HttpHandler =
    withServiceFunc
        (fun (archiveProject : Model.ArchiveProject) -> archiveProject true projectCode)

let archivePrivateProject projectCode : HttpHandler =
    withServiceFunc
        (fun (archiveProject : Model.ArchiveProject) -> archiveProject false projectCode)
