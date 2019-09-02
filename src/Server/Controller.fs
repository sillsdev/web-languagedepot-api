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

let getAllPrivateProjects = (fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
        // TODO: Verify login
        let listProjects = ctx.GetService<Model.ListProjects>()
        let! projects = listProjects false
        return! json projects next ctx
    }
)

let getPrivateProject = (fun projId (next : HttpFunc) (ctx : HttpContext) ->
    task {
        // TODO: Verify login
        let getProject = ctx.GetService<Model.GetProject>()
        let! project = getProject false projId
        match project with
        | Some project -> return! json project next ctx
        | None -> return! RequestErrors.notFound (json (Error (sprintf "Project code %s not found" projId))) next ctx
    }
)

let getAllPublicProjects = (fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
        let listProjects = ctx.GetService<Model.ListProjects>()
        let! projects = listProjects true
        return! json projects next ctx
    }
)

let getPublicProject = (fun projId (next : HttpFunc) (ctx : HttpContext) ->
    task {
        let getProject = ctx.GetService<Model.GetProject>()
        let! project = getProject true projId
        match project with
        | Some project -> return! json project next ctx
        | None -> return! RequestErrors.notFound (json (Error (sprintf "Project code %s not found" projId))) next ctx
    }
)

// TODO: Not in real API spec. Why not? Probably need to add it
let getUser = (fun login (next : HttpFunc) (ctx : HttpContext) ->
    task {
        let getUser = ctx.GetService<Model.GetUser>()
        let! user = getUser login
        match user with
        | Some user -> return! json { user with HashedPassword = "***" } next ctx
        | None -> return! RequestErrors.notFound (json (Error (sprintf "Username %s not found" login))) next ctx
    }
)

let projectExists = (fun projId (next : HttpFunc) (ctx : HttpContext) ->
    // Returns true if project exists (NOTE: This is the INVERSE of what the old API did!)
    task {
        let (Model.ProjectExists projectExists) = ctx.GetService<Model.ProjectExists>()
        let result = projectExists projId
        return! json result next ctx
    }
)

let userExists = (fun login (next : HttpFunc) (ctx : HttpContext) ->
    // Returns true if username exists (NOTE: This is the INVERSE of what the old API did!)
    task {
        let (Model.UserExists userExists) = ctx.GetService<Model.UserExists>()
        let result = userExists login
        return! json result next ctx
    }
)

let getAllUsers = (fun (next : HttpFunc) (ctx : HttpContext) ->
    // DEMO ONLY. Enumerates all users. TODO: Remove since it's not in real API spec
    task {
        let listUsers = ctx.GetService<Model.ListUsers>()
        let! x = listUsers()
        let logins = x |> List.map (fun user -> { user with HashedPassword = "***" })
        return! json logins next ctx
    }
)

let projectsAndRolesByUser = (fun login ->
    bindJson<Shared.LoginInfo> (fun loginInfo (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let verifyLoginInfo = ctx.GetService<Model.VerifyLoginInfo>()
            let projectsAndRolesByUser = ctx.GetService<Model.ProjectsAndRolesByUser>()
            let! goodLogin = verifyLoginInfo loginInfo
            if goodLogin then
                let! projectList = projectsAndRolesByUser login
                return! json projectList next ctx
            else
                return! RequestErrors.forbidden (json {| status = "error"; message = "Login failed" |}) next ctx
        }
    )
)

let addOrRemoveUserFromProject = (fun projId -> bindJson<PatchProjects> (fun patchData (next : HttpFunc) (ctx : HttpContext) -> task {
    match patchData.addUser, patchData.removeUser with
    | Some add, Some remove ->
        return! RequestErrors.badRequest (json (Error "Specify exactly one of addUser or removeUser")) next ctx
    | Some add, None ->
        let (Model.AddMembership addMember) = ctx.GetService<Model.AddMembership>()
        let! success = addMember add.Name projId 3  // TODO: get role in here as well
        let result =
            if success then
                Ok (sprintf "Added %s to %s" add.Name projId)
            else
                Error (sprintf "Failed to add %s to %s" add.Name projId)
        return! json result next ctx
    | None, Some remove ->
        let (Model.RemoveMembership removeMember) = ctx.GetService<Model.RemoveMembership>()
        let! success = removeMember remove.Name projId -1  // TODO: Better API; it makes no sense to specify a role for the removal
        let result =
            if success then
                Ok (sprintf "Removed %s from %s" remove.Name projId)
            else
                Error (sprintf "Failed to remove %s from %s" remove.Name projId)
        return! json result next ctx
    | None, None ->
        return! RequestErrors.badRequest (json (Error "Specify exactly one of addUser or removeUser")) next ctx
}))

// Suggested by Chris Hirt: POST to add, DELETE to remove, no JSON body needed
let addUserToProject = (fun (projId,username) (next : HttpFunc) (ctx : HttpContext) -> task {
    let (Model.AddMembership addMember) = ctx.GetService<Model.AddMembership>()
    let! success = addMember username projId 3  // TODO: get role in here as well
    let result =
        if success then
            Ok (sprintf "Added %s to %s" username projId)
        else
            Error (sprintf "Failed to add %s to %s" username projId)
    return! json result next ctx
})

let removeUserFromProject = (fun (projId,username) (next : HttpFunc) (ctx : HttpContext) -> task {
    let (Model.RemoveMembership removeMember) = ctx.GetService<Model.RemoveMembership>()
    let! success = removeMember username projId -1  // TODO: Better API; it makes no sense to specify a role for the removal
    let result =
        if success then
            Ok (sprintf "Removed %s from %s" username projId)
        else
            Error (sprintf "Failed to remove %s from %s" username projId)
    return! json result next ctx
})

let projectsAndRolesByUserRole = (fun (login, roleId) ->
    bindJson<Shared.LoginInfo> (fun logininfo (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let verifyLoginInfo = ctx.GetService<Model.VerifyLoginInfo>()
            let projectsAndRolesByUserRole = ctx.GetService<Model.ProjectsAndRolesByUserRole>()
            let! goodLogin = verifyLoginInfo logininfo
            if goodLogin then
                let! projectList = projectsAndRolesByUserRole login roleId
                return! json projectList next ctx
            else
                return! RequestErrors.forbidden (json {| status = "error"; message = "Login failed" |}) next ctx
        }
    )
)

let getAllRoles = (fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
        let roleNames = ctx.GetService<Model.ListRoles>()
        let! roles = roleNames()
        return! json roles next ctx
    }
)

let createUser = (fun (user : CreateUser) (next : HttpFunc) (ctx : HttpContext) ->
    task {
        let (Model.UserExists userExists) = ctx.GetService<Model.UserExists>()
        let! alreadyExists = userExists user.Login
        if alreadyExists then
            return! json {| status = "error"; message = "Username already exists; pick another one" |} next ctx
        else
            let createUser = ctx.GetService<Model.CreateUser>()
            let! newId = createUser user
            return! json newId next ctx
    }
)

let upsertUser = (fun login -> bindJson<UpdateUser> (fun updateData (next : HttpFunc) (ctx : HttpContext) ->
    task {
        let upsertUser = ctx.GetService<Model.UpsertUser>()
        let! newId = upsertUser login updateData
        return! json newId next ctx
    }
))

let changePassword = (fun login -> bindJson<ChangePassword> (fun updateData (next : HttpFunc) (ctx : HttpContext) ->
    task {
        let changePassword = ctx.GetService<Model.ChangePassword>()
        let! success = changePassword login updateData
        return! json success next ctx
    }
))

let verifyPassword = (fun login -> bindJson<LoginInfo> (fun loginInfo (next : HttpFunc) (ctx : HttpContext) ->
    task {
        let verifyLoginInfo = ctx.GetService<Model.VerifyLoginInfo>()
        let! goodLogin = verifyLoginInfo loginInfo
        return! json goodLogin next ctx
        // NOTE: We don't do any work behind the scenes to reconcile MySQL and Mongo passwords; that's up to Language Forge
    }
))

let createProject = (fun (proj : CreateProject) (next : HttpFunc) (ctx : HttpContext) ->
    task {
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
)

let countUsers = (fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
        do! Async.Sleep 500 // Simulate server load
        let (Model.CountUsers countUsers) = ctx.GetService<Model.CountUsers>()
        let! count = countUsers ()
        return! json count next ctx
    }
)

let countProjects = (fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
        do! Async.Sleep 750 // Simulate server load
        let (Model.CountProjects countProjects) = ctx.GetService<Model.CountProjects>()
        let! count = countProjects ()
        return! json count next ctx
    }
)

let countRealProjects = (fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
        do! Async.Sleep 1000 // Simulate server load
        let (Model.CountRealProjects countRealProjects) = ctx.GetService<Model.CountRealProjects>()
        let! count = countRealProjects ()
        return! json count next ctx
    }
)

let getMySqlSettings = (fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
        let cfg = ctx |> getSettings<MySqlSettings>
        return! json cfg next ctx
    }
)

let archiveProject = (fun projId (next : HttpFunc) (ctx : HttpContext) -> task {
    // TODO: Verify admin password before this is allowed
    let archiveProject = ctx.GetService<Model.ArchiveProject>()
    let! success = archiveProject true projId
    return! json success next ctx
})

let archivePrivateProject = (fun projId (next : HttpFunc) (ctx : HttpContext) -> task {
    // TODO: Verify admin password before this is allowed
    let archiveProject = ctx.GetService<Model.ArchiveProject>()
    let! success = archiveProject false projId
    return! json success next ctx
})

