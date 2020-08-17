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

// let withServiceFunc (isPublic : bool) (impl : string -> 'service -> Task<'a>) (next : HttpFunc) (ctx : HttpContext) = task {
//     let serviceFunction = ctx.GetService<'service>()
//     let cfg = ctx |> getSettings<MySqlSettings>
//     let connString = if isPublic then cfg.ConnString else cfg.ConnStringPrivate
//     let! result = impl connString serviceFunction
//     return! jsonSuccess result next ctx
// }

let withSimpleFunc (impl : 'a -> Task<'result>) (param : 'a) (next : HttpFunc) (ctx : HttpContext) = task {
    try
        let! result = impl param
        return! jsonSuccess result next ctx
    with e ->
        return! jsonError<'result> (e.ToString()) next ctx  // TODO: More sophisticated error type that carries a message *and* a stacktrace, so the message can be displayed and the stacktrace can be logged
}

let getModel isPublic (ctx : HttpContext) =
    if isPublic
        then ctx.GetService<Model.MySqlPublicModel>() :> Model.IModel
        else ctx.GetService<Model.MySqlPrivateModel>() :> Model.IModel

// Exact same thing as withServiceFunc, really, except that we're retrieving an entire model implementation
let withModel isPublic (impl : Model.IModel -> Task<'result>) (next : HttpFunc) (ctx : HttpContext) = task {
    let model = ctx |> getModel isPublic
    try
        let! result = impl model
        return! jsonSuccess result next ctx
    with e ->
        return! jsonError<'result> (e.ToString()) next ctx  // TODO: More sophisticated error type that carries a message *and* a stacktrace, so the message can be displayed and the stacktrace can be logged
}

let withModelReturningOption isPublic (impl : Model.IModel -> Task<'a option>) (msg : string) (next : HttpFunc) (ctx : HttpContext) = task {
    let model = ctx |> getModel isPublic
    let! opt = (impl model)
    match opt with
    | Some result -> return! jsonSuccess result next ctx
    | None -> return! RequestErrors.notFound (jsonError msg) next ctx
}

let tryParseSingleInt (strs : Microsoft.Extensions.Primitives.StringValues) =
    if strs.Count > 0 then
        match System.Int32.TryParse strs.[0] with
        | true, n -> Some n
        | false, _ -> None
    else None

let getLimitOffset (ctx : HttpContext) =
    let q = ctx.Request.Query
    tryParseSingleInt q.["limit"], tryParseSingleInt q.["offset"]

let withServiceFuncNoSuccessWrap (isPublic : bool) (impl : string -> 'service -> Task<'a>) (next : HttpFunc) (ctx : HttpContext) = task {
    let serviceFunction = ctx.GetService<'service>()
    let cfg = ctx |> getSettings<MySqlSettings>
    let connString = if isPublic then cfg.ConnString else cfg.ConnStringPrivate
    let! result = impl connString serviceFunction
    return! json result next ctx
}

let withLoggedInModel isPublic (loginCredentials : Api.LoginCredentials) (impl : Model.IModel -> Task<'a>) (next : HttpFunc) (ctx : HttpContext) = task {
    let model = ctx |> getModel isPublic
    let! goodLogin = model.VerifyLoginInfo loginCredentials
    if goodLogin then
        return! withModel isPublic impl next ctx
    else
        return! RequestErrors.forbidden (jsonError "Login failed") next ctx
}

let withLoggedInServiceFuncNoSuccessWrap isPublic (loginCredentials : Api.LoginCredentials) (impl : Model.IModel -> Task<'a>) (next : HttpFunc) (ctx : HttpContext) = task {
    let model = ctx |> getModel isPublic
    let! goodLogin = model.VerifyLoginInfo loginCredentials
    if goodLogin then
        let! result = impl model
        return! json result next ctx
    else
        return! RequestErrors.forbidden (json "Login failed") next ctx
}

let getUser isPublic login : HttpHandler =
    withModelReturningOption isPublic
        (fun model -> model.GetUser login)
        (sprintf "Username %s not found" login)

let getPublicProject isPublic projectCode : HttpHandler =
    withModelReturningOption isPublic
        (fun model -> model.GetProject projectCode)
        (sprintf "Project code %s not found" projectCode)

let getPrivateProject isPublic projectCode : HttpHandler =
    // TODO: Get rid of the public/private distinction. The appropriate model will be loaded from the service collection
    withModelReturningOption isPublic
        (fun model -> model.GetProject projectCode)
        (sprintf "Project code %s not found" projectCode)

let searchUsers isPublic searchText (loginCredentials : Api.LoginCredentials) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) -> task {
        let model = ctx |> getModel isPublic
        let! goodLogin = model.VerifyLoginInfo loginCredentials
        if goodLogin then
            let! isAdmin = model.IsAdmin loginCredentials.username
            if isAdmin then
                return! withSimpleFunc model.SearchUsersLoose searchText next ctx
            else
                return! withSimpleFunc model.SearchUsersExact searchText next ctx
        else
            return! RequestErrors.forbidden (jsonError "Login failed") next ctx
    }

let listUsers isPublic : HttpHandler =
    withModel isPublic (fun model -> model.ListUsers None None)

let listUsersLimit isPublic limit : HttpHandler =
    withModel isPublic (fun model -> model.ListUsers (Some limit) None)

let listUsersOffset isPublic offset : HttpHandler =
    withModel isPublic (fun model -> model.ListUsers None (Some offset))

let listUsersLimitOffset isPublic (limit,offset) : HttpHandler =
    withModel isPublic (fun model -> model.ListUsers (Some limit) (Some offset))

let projectExists isPublic projectCode : HttpHandler =
    withModel isPublic (fun model -> model.ProjectExists projectCode)

let userExists isPublic projectCode : HttpHandler =
    withModel isPublic (fun model -> model.UserExists projectCode)

let getAllPublicProjects isPublic : HttpHandler =
    withModel isPublic (fun model -> model.ListProjects())

let getAllPrivateProjects isPublic : HttpHandler =
    // TODO: Get rid of the public/private distinction. The appropriate model will be loaded from the service collection
    withModel isPublic (fun model -> model.ListProjects())

let projectsAndRolesByUser isPublic username (loginCredentials : Api.LoginCredentials) : HttpHandler =
    withLoggedInModel isPublic loginCredentials (fun model -> model.ProjectsAndRolesByUser username)

let legacyProjectsAndRolesByUser isPublic username (legacyLoginCredentials : Api.LegacyLoginCredentials) : HttpHandler =
    let loginCredentials : Api.LoginCredentials =
        { username = username
          password = legacyLoginCredentials.password }
    withLoggedInServiceFuncNoSuccessWrap isPublic loginCredentials (fun model -> model.LegacyProjectsAndRolesByUser username)

let projectsAndRolesByUserRole isPublic username roleName (loginCredentials : Api.LoginCredentials) : HttpHandler =
    withLoggedInModel isPublic loginCredentials (fun model -> model.ProjectsAndRolesByUserRole username roleName)

let addUserToProjectWithRole isPublic (projectCode, username, roleName) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let model = ctx |> getModel isPublic
    let! success = model.AddMembership username projectCode roleName
    let result =
        if success then
            Ok (sprintf "Added %s to %s" username projectCode)
        else
            Error (sprintf "Failed to add %s to %s" username projectCode)
    return! jsonResult result next ctx
}

let addUserToProject isPublic (projectCode, username) = addUserToProjectWithRole isPublic (projectCode,username,"Contributer")

let removeUserFromProject isPublic (projectCode, username) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let model = ctx |> getModel isPublic
    let! success = model.RemoveMembership username projectCode
    let result =
        if success then
            Ok (sprintf "Removed %s from %s" username projectCode)
        else
            Error (sprintf "Failed to remove %s from %s" username projectCode)
    return! jsonResult result next ctx
}
let addOrRemoveUserFromProjectInternal isPublic projectCode (patchData : Api.EditProjectMembershipInternalDetails) =
    match patchData with
    | Api.EditProjectMembershipInternalDetails.AddUserRoles memberships -> RequestErrors.badRequest (jsonError "Add not yet implemented")
    | Api.EditProjectMembershipInternalDetails.RemoveUserRoles memberships -> RequestErrors.badRequest (jsonError "Remove not yet implemented")
    | Api.EditProjectMembershipInternalDetails.RemoveUserEntirely username -> RequestErrors.badRequest (jsonError "RemoveUser not yet implemented")

let mapRoles (apiRoles : Api.MembershipRecordApiCall list) : Result<Api.MembershipRecordInternal list, string> =
    let roleTypes = apiRoles |> List.map (fun apiRole -> apiRole, RoleType.TryOfString apiRole.role)
    let errors = roleTypes |> List.choose (fun (apiRole, roleType) -> if roleType.IsNone then Some apiRole else None)
    if List.isEmpty errors then
        roleTypes |> List.map (fun (apiRole, roleType) -> { username = apiRole.username; role = roleType.Value } : Api.MembershipRecordInternal) |> Ok
    else
        errors |> List.map (fun apiRole -> apiRole.role) |> String.concat ", " |> sprintf "Unrecognized role names: %s" |> Error

let addOrRemoveUserFromProject isPublic projectCode (patchData : Api.EditProjectMembershipApiCall) : HttpHandler =
    match patchData.add, patchData.remove, patchData.removeUser with
    | Some _, Some _, Some _
    | Some _, Some _, None
    | Some _, None,   Some _
    | None,   Some _, Some _ ->
        RequestErrors.badRequest (jsonError "Specify exactly one of add, remove, or removeUser")
    | Some add, None, None ->
        match mapRoles add with
        | Ok addInternal ->
            addOrRemoveUserFromProjectInternal isPublic projectCode (Api.EditProjectMembershipInternalDetails.AddUserRoles addInternal)
        | Error msg ->
            jsonError msg
    | None, Some remove, None ->
        match mapRoles remove with
        | Ok removeInternal ->
            addOrRemoveUserFromProjectInternal isPublic projectCode (Api.EditProjectMembershipInternalDetails.RemoveUserRoles removeInternal)
        | Error msg ->
            jsonError msg
    | None, None, Some removeUser ->
        addOrRemoveUserFromProjectInternal isPublic projectCode (Api.EditProjectMembershipInternalDetails.RemoveUserEntirely removeUser)
    | None, None, None ->
        RequestErrors.badRequest (jsonError "No command included in JSON: should be one of add, remove, or removeUser")

let getAllRoles isPublic : HttpHandler =
    withModel isPublic (fun model -> model.ListRoles())

let createUser isPublic (user : Api.CreateUser) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    // Can't use withServiceFunc for this one since we need to do extra work in the success branch
    let model = ctx |> getModel isPublic
    let! alreadyExists = model.UserExists user.username
    if alreadyExists then
        return! jsonError "Username already exists; pick another one" next ctx
    else
        let! newId = model.CreateUser user
        return! jsonSuccess newId next ctx
}

let upsertUser isPublic (login : string) (updateData : Api.CreateUser) =
    withModel isPublic (fun (model : Model.IModel) -> model.UpsertUser login updateData)

let changePassword isPublic login (updateData : Api.ChangePassword) =
    withModel isPublic (fun (model : Model.IModel) -> model.ChangePassword login updateData)

// NOTE: We don't do any work behind the scenes to reconcile MySQL and Mongo passwords; that's up to Language Forge
let verifyPassword isPublic (loginCredentials : Api.LoginCredentials) =
    withModel isPublic (fun model -> model.VerifyLoginInfo loginCredentials)

let createProject isPublic (proj : Api.CreateProject) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    // Can't use withServiceFunc for this one since we need to tweak the return value in the success branch
    let model = ctx |> getModel isPublic
    let! alreadyExists = model.ProjectExists proj.code
    if alreadyExists then
        return! jsonError "Project code already exists; pick another one" next ctx
    else
        let! newId = model.CreateProject proj
        if newId < 0 then
            return! jsonError "Something went wrong creating the project; please try again" next ctx
        else
            return! jsonSuccess newId next ctx
}

let countUsers isPublic : HttpHandler =
    withModel isPublic (fun model -> model.CountUsers())

let countProjects isPublic : HttpHandler =
    withModel isPublic (fun model -> model.CountProjects())

let countRealProjects isPublic : HttpHandler =
    withModel isPublic (fun model -> model.CountRealProjects())

let archiveProject isPublic projectCode : HttpHandler =
    withModel isPublic (fun model -> model.ArchiveProject projectCode)

let archivePrivateProject isPublic projectCode : HttpHandler =
    withModel isPublic (fun model -> model.ArchiveProject projectCode)  // TODO: Get rid of the public/private distinction. The appropriate model will be loaded from the service collection

let emailIsAdminImpl isPublic email (ctx : HttpContext) = task {
    let model = ctx |> getModel isPublic
    return! model.EmailIsAdmin email
}

let emailIsAdmin isPublic email : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let! isAdmin = emailIsAdminImpl isPublic email ctx
    return! jsonSuccess isAdmin next ctx
}
