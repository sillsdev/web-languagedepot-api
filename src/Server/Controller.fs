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
let apiError<'a> (msg : string) = { ok = false; data = Unchecked.defaultof<'a>; message = msg }
let apiSuccess data = { ok = true; data = data; message = "" }
let apiResult (result : Result<'a, string>) =
    match result with
    | Ok data -> apiSuccess data
    | Error msg -> apiError msg

let jsonError<'a> (msg : string) : HttpHandler = json (apiError<'a> msg)
let jsonSuccess data : HttpHandler = json (apiSuccess data)
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
        return! jsonError<'result> e.Message next ctx
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
        return! jsonError<'result> e.Message next ctx
}

let withModelReturningOption isPublic (impl : Model.IModel -> Task<'a option>) (notFoundMsg : string) (next : HttpFunc) (ctx : HttpContext) = task {
    let model = ctx |> getModel isPublic
    let! opt = (impl model)
    match opt with
    | Some result -> return! jsonSuccess result next ctx
    | None -> return! RequestErrors.notFound (jsonError notFoundMsg) next ctx
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

let listUsers isPublic : HttpHandler = fun next ctx -> task {
        let limit, offset = getLimitOffset ctx
        return! withModel isPublic (fun model -> model.ListUsers limit offset) next ctx
    }

let projectExists isPublic projectCode : HttpHandler =
    withModel isPublic (fun model -> model.ProjectExists projectCode)

let userExists isPublic projectCode : HttpHandler =
    withModel isPublic (fun model -> model.UserExists projectCode)

let getAllPublicProjects isPublic : HttpHandler = fun next ctx -> task {
        let limit, offset = getLimitOffset ctx
        return! withModel isPublic (fun model -> model.ListProjects limit offset) next ctx
    }

let getAllPrivateProjects isPublic : HttpHandler = fun next ctx -> task {
        let limit, offset = getLimitOffset ctx
        return! withModel isPublic (fun model -> model.ListProjects limit offset) next ctx
    }
    // TODO: Get rid of the public/private distinction. The appropriate model will be loaded from the service collection

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

let getAllRoles isPublic : HttpHandler = fun next ctx -> task {
        let limit, offset = getLimitOffset ctx
        return! withModel isPublic (fun model -> model.ListRoles limit offset) next ctx
    }

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

let withModelAndData isPublic (impl : 'data -> Model.IModel -> Task<Result<'out,string>>) next ctx = task {
    try
        let! data = Controller.getJson<'data> ctx
        let model = ctx |> getModel isPublic
        let! result = impl data model
        return! jsonResult result next ctx
    with :? System.Text.Json.JsonException as e ->
        return! RequestErrors.badRequest (jsonError (e.ToString())) next ctx
}

let withModelOrPass isPublic (impl : 'data -> Model.IModel -> Task<Result<'out,string>>) next (ctx : HttpContext) = task {
    ctx.Request.EnableBuffering()  // So the request body can be attempted multiple times
    try
        let! data = Controller.getJson<'data> ctx
        let model = ctx |> getModel isPublic
        let! result = impl data model
        return! jsonResult result next ctx
    with :? System.Text.Json.JsonException as e ->
        ctx.Request.Body.Position <- 0L  // Rewind for the next attempt
        return None
}

let createUserManualDeserialize isPublic =
    withModelAndData isPublic (fun (user : Api.CreateUser) model -> task {
        let! alreadyExists = model.UserExists user.username
        if alreadyExists then
            return Error "Username already exists; pick another one"
        else
            let! newId = model.CreateUser user
            return Ok newId
    })

let upsertUser isPublic (login : string) (updateData : Api.CreateUser) =
    withModel isPublic (fun (model : Model.IModel) -> model.UpsertUser login updateData)

let upsertUserManual isPublic (login : string) =
    withModelAndData isPublic (fun (updateData : Api.CreateUser) (model : Model.IModel) -> task { return Ok (model.UpsertUser login updateData)})

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

let createProjectManual isPublic =
    withModelAndData isPublic (fun (proj : Api.CreateProject) model -> task {
        let! alreadyExists = model.ProjectExists proj.code
        if alreadyExists then
            return Error "Project code already exists; pick another one"
        else
            let! newId = model.CreateProject proj
            if newId < 0 then
                return Error "Something went wrong creating the project; please try again"
            else
                return Ok newId
    })

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

let mapRolesIntoDb (s : string) =
    match s.ToLowerInvariant() with
    | "contributor" -> "Contributer"  // Misspelled in database
    | _ -> s

let addUsers (model : Model.IModel) projectCode (data : Api.MembershipRecordApiCall list) =
    let rec loop (data : Api.MembershipRecordApiCall list) =
        match data with
        | [] -> task { return Ok () }
        | record :: rest ->
            task {
                let! success = model.AddMembership record.username projectCode (mapRolesIntoDb record.role)
                if success then
                    return! loop rest
                else
                    return Error rest
            }
    loop data

let removeUsers (model : Model.IModel) projectCode (data : Api.MembershipRecordApiCall list) =
    let rec loop (data : Api.MembershipRecordApiCall list) =
        match data with
        | [] -> task { return Ok () }
        | record :: rest ->
            task {
                let! success = model.RemoveMembership record.username projectCode
                if success then
                    return! loop rest
                else
                    return Error rest
            }
    loop data

let addOrRemoveUserFromProject isPublic projectCode =
    choose [
        withModelOrPass isPublic (fun (addApi : Api.AddProjectMembershipApiCall) model -> task {
            let! success = addUsers model projectCode addApi.add
            match success with
            | Ok () -> return Ok (sprintf "Added %A to %s" addApi.add projectCode)
            | Error failed -> return Error (sprintf "Something went wrong adding %A to %s" failed projectCode)
        })
        withModelOrPass isPublic (fun (removeApi : Api.RemoveProjectMembershipApiCall) model -> task {
            let! success = removeUsers model projectCode removeApi.remove
            match success with
            | Ok () -> return Ok (sprintf "Removed %A from %s" removeApi.remove projectCode)
            | Error failed -> return Error (sprintf "Something went wrong removing %A from %s" failed projectCode)
        })
        withModelOrPass isPublic (fun (removeUserApi : Api.RemoveUserProjectMembershipApiCall) model -> task {
            let! success = model.RemoveMembership removeUserApi.removeUser projectCode
            if success
            then return Ok (sprintf "Removed %A from %s" removeUserApi.removeUser projectCode)
            else return Error (sprintf "Something went wrong removing %A from %s" removeUserApi.removeUser projectCode)
        })
        RequestErrors.badRequest (jsonError "Could not parse JSON")
    ]

// withModelAndData isPublic (fun (patchData : Api.EditProjectMembershipInternalDetails) model -> task {
//     match patchData with
//     | Api.EditProjectMembershipInternalDetails.Add (login, memberships) -> return Ok (sprintf "Would add memberships %A to %s" memberships projectCode)
//     | Api.EditProjectMembershipInternalDetails.Remove (login, memberships) -> return Error (sprintf "Would remove memberships %A from %s" memberships projectCode)
//     | Api.EditProjectMembershipInternalDetails.RemoveUser (login, username) -> return Error (sprintf "Would remove user %s from %s" username projectCode)
// })

let addOrRemoveUserFromProjectSample isPublic =
    let login : Api.LoginCredentials = { username = "x"; password = "y" }
    withModel isPublic (fun model -> task {
        // let foo : Api.RemoveUserProjectMembershipApiCall = { login = login; removeUser = "x" }
        // let foo = Api.EditProjectMembershipApiCall.RemoveUser (login, "x")
        // let foo : Api.AddProjectMembershipApiCall = { login = login; add = [{
        //     username = "foo"
        //     role = "Contributor"
        // }]}
        let foo : Api.RemoveProjectMembershipApiCall = { login = login; remove = [{
            username = "foo"
            role = "Contributor"
        }]}
        return foo
    })
