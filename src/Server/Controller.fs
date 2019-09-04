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

let withLoggedInServiceFunc (impl : 'service -> Async<'a>) =
    bindJson<Shared.LoginInfo> (fun loginInfo (next : HttpFunc) (ctx : HttpContext) -> task {
        let verifyLoginInfo = ctx.GetService<Model.VerifyLoginInfo>()
        let! goodLogin = verifyLoginInfo loginInfo
        if goodLogin then
            return! withServiceFunc impl next ctx
        else
            return! RequestErrors.forbidden (jsonError "Login failed") next ctx
    }
)

let getAllPrivateProjects : HttpHandler =
    withServiceFunc
        (fun (listProjects : Model.ListProjects) -> listProjects false)

let getPrivateProject projId : HttpHandler =
    withServiceFuncOrNotFound
        (fun (getProject : Model.GetProject) -> getProject false projId)
        (sprintf "Project code %s not found" projId)

let getAllPublicProjects : HttpHandler =
    withServiceFunc
        (fun (listProjects : Model.ListProjects) -> listProjects true)

let getPublicProject projId : HttpHandler =
    withServiceFuncOrNotFound
        (fun (getProject : Model.GetProject) -> getProject true projId)
        (sprintf "Project code %s not found" projId)

// TODO: Not in real API spec. Why not? Probably need to add it
let getUser login = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    // Can't use withServiceFuncOrNotFound for this one since we need to tweak the return value in the success branch
    let getUser = ctx.GetService<Model.GetUser>()
    let! user = getUser login
    match user with
    | Some user -> return! json { user with HashedPassword = "***" } next ctx
    | None -> return! RequestErrors.notFound (json (Error (sprintf "Username %s not found" login))) next ctx
}

let projectExists projId : HttpHandler =
    withServiceFunc
        (fun (Model.ProjectExists projectExists) -> projectExists projId)

let userExists projId : HttpHandler =
    withServiceFunc
        (fun (Model.UserExists userExists) -> userExists projId)

let projectsAndRolesByUser login : HttpHandler =
    withLoggedInServiceFunc
        (fun (projectsAndRolesByUser : Model.ProjectsAndRolesByUser) -> projectsAndRolesByUser login)

let projectsAndRolesByUserRole (login,roleId) : HttpHandler =
    withLoggedInServiceFunc
        (fun (projectsAndRolesByUserRole : Model.ProjectsAndRolesByUserRole) -> projectsAndRolesByUserRole login roleId)

let addUserToProject (projId,username) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let (Model.AddMembership addMember) = ctx.GetService<Model.AddMembership>()
    let! success = addMember username projId 3  // TODO: get role in here as well
    let result =
        if success then
            Ok (sprintf "Added %s to %s" username projId)
        else
            Error (sprintf "Failed to add %s to %s" username projId)
    return! json result next ctx
}

let removeUserFromProject (projId,username) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    let (Model.RemoveMembership removeMember) = ctx.GetService<Model.RemoveMembership>()
    let! success = removeMember username projId -1  // TODO: Better API; it makes no sense to specify a role for the removal
    let result =
        if success then
            Ok (sprintf "Removed %s from %s" username projId)
        else
            Error (sprintf "Failed to remove %s from %s" username projId)
    return! json result next ctx
}

let addOrRemoveUserFromProject projId (patchData : PatchProjects) =
    match patchData.addUser, patchData.removeUser with
    | Some add, Some remove ->
        RequestErrors.badRequest (json (Error "Specify exactly one of addUser or removeUser, not both"))
    | Some add, None ->
        addUserToProject (projId, add.Name)
    | None, Some remove ->
        removeUserFromProject (projId, remove.Name)
    | None, None ->
        RequestErrors.badRequest (json (Error "Specify either addUser or removeUser"))

let getAllRoles : HttpHandler =
    withServiceFunc
        (fun (roleNames : Model.ListRoles) -> roleNames())

let createUser (user : CreateUser) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    // Can't use withServiceFunc for this one since we need to do extra work in the success branch
    let (Model.UserExists userExists) = ctx.GetService<Model.UserExists>()
    let! alreadyExists = userExists user.Login
    if alreadyExists then
        return! jsonError "Username already exists; pick another one" next ctx
    else
        let createUser = ctx.GetService<Model.CreateUser>()
        let! newId = createUser user
        return! json newId next ctx
}

let upsertUser (login : string) (updateData : UpdateUser) =
    withServiceFunc
        (fun (upsertUser : Model.UpsertUser) -> upsertUser login updateData)

let changePassword login (updateData : ChangePassword) =
    withServiceFunc
        (fun (changePassword : Model.ChangePassword) -> changePassword login updateData)

// NOTE: We don't do any work behind the scenes to reconcile MySQL and Mongo passwords; that's up to Language Forge
let verifyPassword login (loginInfo : LoginInfo) =
    withServiceFunc
        (fun (verifyLoginInfo : Model.VerifyLoginInfo) -> verifyLoginInfo loginInfo)

let createProject (proj : CreateProject) : HttpHandler = fun (next : HttpFunc) (ctx : HttpContext) -> task {
    // Can't use withServiceFunc for this one since we need to tweak the return value in the success branch
    let (Model.ProjectExists projectExists) = ctx.GetService<Model.ProjectExists>()
    let projId = match proj.Identifier with
                    | None -> "new-project-id"  // TODO: Build from project name and check whether it exists, appending numbers if needed
                    | Some projId -> projId
    let! alreadyExists = projectExists projId
    if alreadyExists then
        return! json {| status = "error"; message = "Project code already exists; pick another one" |} next ctx
    else
        let createProject = ctx.GetService<Model.CreateProject>()
        let! newId = createProject { proj with Identifier = Some projId }
        return! json newId next ctx
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

let archiveProject projId : HttpHandler =
    withServiceFunc
        (fun (archiveProject : Model.ArchiveProject) -> archiveProject true projId)

let archivePrivateProject projId : HttpHandler =
    withServiceFunc
        (fun (archiveProject : Model.ArchiveProject) -> archiveProject false projId)
