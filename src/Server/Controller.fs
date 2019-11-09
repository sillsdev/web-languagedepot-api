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

let withServiceFuncOrNotFound (impl : 'service -> Async<'a option>) (msg : string) (next : HttpFunc) (ctx : HttpContext) = task {
    let serviceFunction = ctx.GetService<'service>()
    let! resultOpt = impl serviceFunction
    match resultOpt with
    | Some result -> return! json result next ctx
    | None -> return! RequestErrors.notFound (jsonError msg) next ctx
}

let withLoggedInServiceFunc (loginCredentials : Api.LoginCredentials) (impl : 'service -> Async<'a>) =
    // TODO: Rewrite functinos that call this to pass login credentials from their API data
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

let projectExists projectCode : HttpHandler =
    withServiceFunc
        (fun (Model.ProjectExists projectExists) -> projectExists projectCode)

let userExists projectCode : HttpHandler =
    withServiceFunc
        (fun (Model.UserExists userExists) -> userExists projectCode)

let projectsAndRolesByUser login : HttpHandler =
    withLoggedInServiceFunc
        (fun (projectsAndRolesByUser : Model.ProjectsAndRolesByUser) -> projectsAndRolesByUser login)

let projectsAndRolesByUserRole (login, roleTypeStr) : HttpHandler =
    let roleType = RoleType.TryOfString
    withLoggedInServiceFunc
        (fun (projectsAndRolesByUserRole : Model.ProjectsAndRolesByUserRole) -> projectsAndRolesByUserRole login roleType)

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
    | None -> return! jsonError (sprintf "Unrecognized role name %s" roleName)
    | Some roleType ->
        return! addUserToProjectWithRoleType (projectCode,username,roleType) next ctx
}

let addUserToProject (projectCode, username) = addUserToProjectWithRoleType (projectCode,username,Contributor)

let removeUserFromProject (projectCode, username) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let (Model.RemoveMembership removeMember) = ctx.GetService<Model.RemoveMembership>()
    let! success = removeMember username projectCode -1  // TODO: Better API; it makes no sense to specify a role for the removal
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

let addOrRemoveUserFromProject projectCode (patchData : Api.EditProjectMembershipApiCall) =
    match patchData.add, patchData.remove, patchData.removeUser with
    | Some _, Some _, Some _
    | Some _, Some _, None
    | Some _, None,   Some _
    | None,   Some _, Some _ ->
        RequestErrors.badRequest (json (Error "Specify exactly one of add, remove, or removeUser"))
    | Some add, None, None ->
        addOrRemoveUserFromProjectInternal projectCode (Api.EditProjectMembershipInternalDetails.AddUserRoles add)
    | None, Some remove, None ->
        addOrRemoveUserFromProjectInternal projectCode (Api.EditProjectMembershipInternalDetails.RemoveUserRoles remove)
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
let verifyPassword login (loginCredentials : Api.LoginCredentials) =
    withServiceFunc
        (fun (verifyLoginInfo : Model.VerifyLoginCredentials) -> verifyLoginInfo loginCredentials)

let createProject (proj : Api.CreateProject) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    // Can't use withServiceFunc for this one since we need to tweak the return value in the success branch
    let (Model.ProjectExists projectExists) = ctx.GetService<Model.ProjectExists>()
    let! alreadyExists = projectExists proj.code
    if alreadyExists then
        return! jsonError "Project code already exists; pick another one" next ctx
    else
        return! withServiceFunc (fun (createProject : Model.CreateProject) -> createProject proj)
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
