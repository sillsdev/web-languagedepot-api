module Model

open System
open BCrypt.Net
open FSharp.Data.Sql
open Shared

[<Literal>]
let sampleConnString = "Server=localhost;Database=testldapi;User=rmunn"

[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__

type sql = SqlDataProvider<Common.DatabaseProviderTypes.MYSQL,
                           sampleConnString,
                           ResolutionPath = resolutionPath,
                           CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL,
                           UseOptionTypes = true>

// TODO: Add "is_archived" boolean to model (default false) so we can implement archiving; update queries that list or count projects to specify "where (isArchived = false)"
type Shared.Project with
    static member FromSql (sqlProject : sql.dataContext.``testldapi.projectsEntity``) = {
        Id = sqlProject.Id
        Name = sqlProject.Name
        Description = sqlProject.Description
        Homepage = sqlProject.Homepage
        IsPublic = sqlProject.IsPublic <> 0y
        ParentId = sqlProject.ParentId
        CreatedOn = sqlProject.CreatedOn
        UpdatedOn = sqlProject.UpdatedOn
        Identifier = sqlProject.Identifier
        Status = sqlProject.Status
    }

type Shared.ProjectForListing with
    static member FromSql ((id,identifier,createdOn,name,description) : int * Option<string> * Option<DateTime> * string * Option<string>) = {
        Id = id
        Name = name
        CreatedOn = createdOn
        Identifier = identifier
        Typ = GuessProjectType.guessType identifier name description
    }

type Shared.User with
    static member FromSql (sqlUser : sql.dataContext.``testldapi.usersEntity``) = {
        Id = sqlUser.Id
        Login = sqlUser.Login
        HashedPassword = sqlUser.HashedPassword
        FirstName = sqlUser.Firstname
        LastName = sqlUser.Lastname
        Mail = sqlUser.Mail
        MailNotification = sqlUser.MailNotification <> 0y
        Admin = sqlUser.Admin <> 0y
        Status = sqlUser.Status
        LastLoginOn = sqlUser.LastLoginOn
        Language = sqlUser.Language
        AuthSourceId = sqlUser.AuthSourceId
        CreatedOn = sqlUser.CreatedOn
        UpdatedOn = sqlUser.UpdatedOn
        Type = sqlUser.Type
    }

type Shared.Role with
    static member FromSql (sqlRole : sql.dataContext.``testldapi.rolesEntity``) = {
        Id = sqlRole.Id
        Name = sqlRole.Name
        Position = sqlRole.Position
        Assignable = (sqlRole.Assignable |> Option.defaultValue 0y) <> 0y
        Builtin = sqlRole.Builtin
        Permissions = sqlRole.Permissions
    }

type Shared.Membership with
    static member FromSql (sqlMember : sql.dataContext.``testldapi.membersEntity``) = {
        Id = sqlMember.Id
        UserId = sqlMember.UserId
        ProjectId = sqlMember.ProjectId
        RoleId = sqlMember.RoleId
        CreatedOn = sqlMember.CreatedOn
        MailNotification = sqlMember.MailNotification <> 0y
    }

type ListUsers = unit -> Async<User list>
type ListProjects = bool -> Async<ProjectForListing list>
// These three CountFoo types all look the same, so we have to use a single-case DU to distinguish them
type CountUsers = CountUsers of (unit -> Async<int>)
type CountProjects = CountProjects of (unit -> Async<int>)
type CountRealProjects = CountRealProjects of (unit -> Async<int>)
type ListRoles = unit -> Async<Role list>
type ProjectsByUser = string -> Async<Project list>
type ProjectsByUserRole = string -> int -> Async<Project list>
type ProjectsAndRolesByUser = string -> Async<(Project * Role) list>
type ProjectsAndRolesByUserRole = string -> int -> Async<(Project * Role) list>
// Ditto for these two FooExists types: need a DU
type UserExists = UserExists of (string -> Async<bool>)
type ProjectExists = ProjectExists of (string -> Async<bool>)
type GetUser = string -> Async<User option>
type GetProject = bool -> string -> Async<Project option>
type CreateProject = Shared.CreateProject -> Async<int>
type CreateUser = Shared.CreateUser -> Async<int>
type UpsertUser = string -> Shared.UpdateUser -> Async<int>
type ChangePassword = string -> Shared.ChangePassword -> Async<bool>
type VerifyLoginInfo = LoginInfo -> Async<bool>
type AddMembership = AddMembership of (string -> string -> int -> Async<bool>)
type RemoveMembership = RemoveMembership of (string -> string -> int -> Async<bool>)
type ArchiveProject = bool -> string -> Async<bool>

let usersQueryAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        let usersQuery = query {
            for user in ctx.Testldapi.Users do
                select (User.FromSql user)
        }
        return! usersQuery |> List.executeQueryAsync
    }

let projectsQueryAsync (connString : string) (isPublic : bool) =
    async {
        let ctx = sql.GetDataContext connString
        let projectsQuery = query {
            for project in ctx.Testldapi.Projects do
                where (if isPublic then project.IsPublic > 0y else project.IsPublic = 0y)
                where (project.Status = ProjectStatus.Active)
                select (project.Id, project.Identifier, project.CreatedOn, project.Name, project.Description)
        }
        let! projects = projectsQuery |> List.executeQueryAsync
        return projects |> List.map ProjectForListing.FromSql
    }

let projectsCountAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Testldapi.Projects do
                where (project.IsPublic > 0y)
                where (project.Status = ProjectStatus.Active)
                count
        }
    }

let realProjectsCountAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        let projectsQuery = query {
            for project in ctx.Testldapi.Projects do
                where (project.IsPublic > 0y)
                where (project.Status = ProjectStatus.Active)
                select (project.Id, project.Identifier, project.CreatedOn, project.Name, project.Description)
            }
        let! projects = projectsQuery |> Seq.executeQueryAsync
        return
            projects
            |> Seq.map ProjectForListing.FromSql
            |> Seq.filter (fun project -> project.Typ <> Test && not ((defaultArg project.Identifier "").StartsWith "test"))
            |> Seq.length
    }

let usersCountAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for _ in ctx.Testldapi.Users do
            count
        }
    }

let userExists (connString : string) username =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for user in ctx.Testldapi.Users do
                select user.Login
                contains username }
    }

let projectExists (connString : string) projectCode =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Testldapi.Projects do
                where (project.IsPublic > 0y)
                // We do NOT check where (project.Status = ProjectStatus.Active) here because we want to forbid re-using project codes even of inactive projects
                where (project.Identifier.IsSome)
                select project.Identifier.Value
                contains projectCode }
    }

let getUser (connString : string) username =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some (User.FromSql user))
                exactlyOneOrDefault }
    }

let getProject (connString : string) (isPublic : bool) projectCode =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Testldapi.Projects do
                where (if isPublic then project.IsPublic > 0y else project.IsPublic = 0y)
                where (not project.Identifier.IsNone)
                where (project.Identifier.Value = projectCode)
                select (Some (Project.FromSql project))
                exactlyOneOrDefault }
    }

let createProject (connString : string) (project : Shared.CreateProject) =
    async {
        // TODO: Handle case where project already exists, and reject if it does. Also, API shape needs to become Async<int option> instead of Async<int>
        let ctx = sql.GetDataContext connString
        let sqlProject = ctx.Testldapi.Projects.Create()
        // sqlProject.Id <- project.Id // int
        sqlProject.Name <- project.Name // string
        sqlProject.Description <- project.Description // string option // Long
        // sqlProject.Homepage <- project.Homepage // string option // Long
        // sqlProject.IsPublic <- if project.IsPublic then 1y else 0y
        // sqlProject.ParentId <- project.ParentId // int option
        // sqlProject.CreatedOn <- project.CreatedOn // System.DateTime option
        // sqlProject.UpdatedOn <- project.UpdatedOn // System.DateTime option
        sqlProject.Identifier <- project.Identifier // string option // 20 chars
        sqlProject.Status <- ProjectStatus.Active
        do! ctx.SubmitUpdatesAsync()
        return sqlProject.Id
    }

let createUser (connString : string) (user : Shared.CreateUser) =
    async {
        let ctx = sql.GetDataContext connString
        let hashedPassword = BCrypt.HashPassword user.CleartextPassword  // TODO: Password hashing doesn't belong in the model
        let sqlUser = ctx.Testldapi.Users.Create()
        sqlUser.Firstname <- user.FirstName
        sqlUser.Lastname <- user.LastName
        sqlUser.HashedPassword <- hashedPassword
        sqlUser.Login <- user.Login
        sqlUser.Mail <- user.Mail
        do! ctx.SubmitUpdatesAsync()
        return sqlUser.Id
    }

let upsertUser (connString : string) (login : string) (updatedUser : Shared.UpdateUser) =
    async {
        let ctx = sql.GetDataContext connString
        let maybeUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = login)
                select (Some user)
                exactlyOneOrDefault
        }
        let sqlUser = match maybeUser with
                      | None -> ctx.Testldapi.Users.Create()
                      | Some user -> user
        sqlUser.Firstname <- updatedUser.User.FirstName
        sqlUser.Lastname <- updatedUser.User.LastName
        match maybeUser, updatedUser.NewPassword with
        | None, None -> sqlUser.HashedPassword <- ""  // New user and no password specified: blank password so they can't log in yet
        | Some user, None -> ()  // Existing user: not updating the password
        | _, Some password -> sqlUser.HashedPassword <- BCrypt.HashPassword password
        sqlUser.Login <- updatedUser.User.Login
        sqlUser.Mail <- updatedUser.User.Mail
        sqlUser.MailNotification <- if updatedUser.User.MailNotification then 1y else 0y
        sqlUser.Admin <- if updatedUser.User.Admin then 1y else 0y
        sqlUser.Status <- updatedUser.User.Status
        sqlUser.LastLoginOn <- updatedUser.User.LastLoginOn
        sqlUser.Language <- updatedUser.User.Language
        sqlUser.AuthSourceId <- updatedUser.User.AuthSourceId
        sqlUser.CreatedOn <- updatedUser.User.CreatedOn
        sqlUser.UpdatedOn <- updatedUser.User.UpdatedOn
        sqlUser.Type <- updatedUser.User.Type
        do! ctx.SubmitUpdatesAsync()
        return sqlUser.Id
    }

let changePassword (connString : string) (login : string) (changeRequest : Shared.ChangePassword) =
    async {
        let ctx = sql.GetDataContext connString
        let maybeUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = login)
                select (Some user)
                exactlyOneOrDefault
        }
        match maybeUser with
        | None -> return false
        | Some sqlUser ->
            let verified = BCrypt.Verify(changeRequest.OldPassword, sqlUser.HashedPassword)
            if verified then
                sqlUser.HashedPassword <- BCrypt.HashPassword changeRequest.NewPassword
                do! ctx.SubmitUpdatesAsync()
                return true
            else
                return false
    }

let projectsByUserRole (connString : string) username roleId =
    async {
        let ctx = sql.GetDataContext connString
        let requestedUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some user)
                exactlyOneOrDefault }
        match requestedUser with
        | None -> return []
        | Some requestedUser ->
            let projectsQuery = query {
                for project in ctx.Testldapi.Projects do
                    join user in ctx.Testldapi.Members
                        on (project.Id = user.ProjectId)
                    where (project.Status = ProjectStatus.Active)
                    where (user.UserId = requestedUser.Id &&
                        (if roleId < 0 then true else user.RoleId = roleId))
                    select (Project.FromSql project)
                }
            return! projectsQuery |> List.executeQueryAsync
    }

let projectsByUser username connString = projectsByUserRole connString username -1

let projectsAndRolesByUserRole (connString : string) username (roleId : int) =
    async {
        let ctx = sql.GetDataContext connString
        let requestedUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some user)
                exactlyOneOrDefault }
        match requestedUser with
        | None -> return []
        | Some requestedUser ->
            let projectsQuery = query {
                for project in ctx.Testldapi.Projects do
                    join user in ctx.Testldapi.Members
                        on (project.Id = user.ProjectId)
                    join role in ctx.Testldapi.Roles on (user.RoleId = role.Id)
                    where (project.Status = ProjectStatus.Active)
                    where (user.UserId = requestedUser.Id &&
                        (if roleId < 0 then true else user.RoleId = roleId))
                    select (Project.FromSql project, Role.FromSql role)
                }
            return! projectsQuery |> List.executeQueryAsync
    }

let projectsAndRolesByUser (connString : string) username =
    projectsAndRolesByUserRole connString username -1

let roleNames (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        let roleQuery = query {
            for role in ctx.Testldapi.Roles do
                select (Role.FromSql role)
        }
        return! roleQuery |> List.executeQueryAsync
    }

let hexStrToBytes (hexStr : string) =
    let len = hexStr.Length
    if len % 2 <> 0 then
        raise (ArgumentException("hexStr", "Hex-encoded byte strings must have an even length"))
    let result = Array.zeroCreate (len / 2)
    for i in 0..2..len - 1 do
        result.[i/2] <- System.Convert.ToByte(hexStr.[i..i+1], 16)
    result

let verifyPass (clearPass : string) (hashPass : string) =
    if hashPass.StartsWith("$2") then
        // Bcrypt
        false  // TODO: Implement
    elif hashPass.Length = 32 then
        // MD5
        false  // TODO: Implement? Or just reject that one bit of test data?
    elif hashPass.Length = 40 then
        // SHA1
        let utf8 = System.Text.UTF8Encoding(false)
        let clearBytes = utf8.GetBytes(clearPass)
        let hashBytes = hexStrToBytes hashPass
        use sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider()
        sha1.ComputeHash(clearBytes) = hashBytes
    else
        false

let verifyLoginInfo (connString : string) (loginInfo : Shared.LoginInfo) =
    async {
        let ctx = sql.GetDataContext connString
        let! user = query { for user in ctx.Testldapi.Users do
                                where (user.Login = loginInfo.username)
                                select user } |> Seq.tryHeadAsync
        match user with
        | None -> return false
        | Some user -> return verifyPass loginInfo.password user.HashedPassword
    }


let addOrRemoveMembershipById (connString : string) (isAdd : bool) (userId : int) (projectId : int) (roleId : int) =
    async {
        let ctx = sql.GetDataContext connString
        let membershipQuery = query {
            for membership in ctx.Testldapi.Members do
                where (membership.ProjectId = projectId && membership.UserId = userId)
                select membership }
        if isAdd then
            let withRole = query {
                for membership in membershipQuery do
                    where (membership.RoleId = roleId)
                    select (Some membership)
                    headOrDefault
            }
            match withRole with
            | None -> // add
                let sqlMembership = ctx.Testldapi.Members.Create()
                sqlMembership.MailNotification <- 0y
                sqlMembership.CreatedOn <- Some (System.DateTime.UtcNow)
                sqlMembership.ProjectId <- projectId
                sqlMembership.UserId <- userId
                sqlMembership.RoleId <- roleId
                do! ctx.SubmitUpdatesAsync()
            | Some sqlMembership ->
                // Already exists; nothing to do
                ()
        else
            let! rowsToDelete = membershipQuery |> List.executeQueryAsync
            rowsToDelete |> List.iter (fun sqlMembership -> sqlMembership.Delete())
            do! ctx.SubmitUpdatesAsync()
    }

let addOrRemoveMembership (connString : string) (isAdd : bool) (username : string) (projectCode : string) (roleId : int) =
    async {
        let ctx = sql.GetDataContext connString
        let maybeUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some user)
                exactlyOneOrDefault }
        let maybeProject = query {
            for project in ctx.Testldapi.Projects do
                where (project.Status = ProjectStatus.Active)
                where (project.Identifier.IsSome)
                where (project.Identifier.Value = projectCode)
                select (Some project)
                exactlyOneOrDefault }
        let validRole = query {
            for role in ctx.Testldapi.Roles do
                select role.Id
                contains roleId }
        match maybeUser, maybeProject, validRole with
        | None, _, _
        | _, None, _
        | _, _, false ->
            return false
        | Some sqlUser, Some sqlProject, true ->
            do! addOrRemoveMembershipById connString isAdd sqlUser.Id sqlProject.Id roleId
            return true
    }

let archiveProject (connString : string) (isPublic : bool) (projectCode : string) =
    async {
        let ctx = sql.GetDataContext connString
        let maybeProject = query {
            for project in ctx.Testldapi.Projects do
                where (if isPublic then project.IsPublic > 0y else project.IsPublic = 0y)
                where (not project.Identifier.IsNone)
                where (project.Identifier.Value = projectCode)
                select (Some project)
                exactlyOneOrDefault }
        match maybeProject with
        | None -> return false
        | Some project ->
            project.Status <- ProjectStatus.Archived
            do! ctx.SubmitUpdatesAsync()
            return true
    }

module ModelRegistration =
    open Microsoft.Extensions.DependencyInjection

    let registerServices (builder : IServiceCollection) (connString : string) =
        builder
            .AddSingleton<ListUsers>(usersQueryAsync connString)
            .AddSingleton<ListProjects>(projectsQueryAsync connString)
            .AddSingleton<CountUsers>(CountUsers (usersCountAsync connString))
            .AddSingleton<CountProjects>(CountProjects (projectsCountAsync connString))
            .AddSingleton<CountRealProjects>(CountRealProjects (realProjectsCountAsync connString))
            .AddSingleton<ListRoles>(roleNames connString)
            .AddSingleton<UserExists>(UserExists (userExists connString))
            .AddSingleton<ProjectExists>(ProjectExists (projectExists connString))
            .AddSingleton<GetUser>(getUser connString)
            .AddSingleton<GetProject>(getProject connString)
            .AddSingleton<CreateProject>(createProject connString)
            .AddSingleton<CreateUser>(createUser connString)
            .AddSingleton<UpsertUser>(upsertUser connString)
            .AddSingleton<ChangePassword>(changePassword connString)
            .AddSingleton<ProjectsByUser>(projectsByUser connString)
            .AddSingleton<ProjectsByUserRole>(projectsByUserRole connString)
            .AddSingleton<ProjectsAndRolesByUser>(projectsAndRolesByUser connString)
            .AddSingleton<ProjectsAndRolesByUserRole>(projectsAndRolesByUserRole connString)
            .AddSingleton<VerifyLoginInfo>(verifyLoginInfo connString)
            .AddSingleton<AddMembership>(AddMembership (addOrRemoveMembership connString true))
            .AddSingleton<RemoveMembership>(RemoveMembership (addOrRemoveMembership connString false))
            .AddSingleton<ArchiveProject>(archiveProject connString)
        |> ignore
