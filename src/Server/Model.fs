module Model

open System
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
type ListProjects = unit -> Async<Project list>
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
type GetProject = string -> Async<Project option>
type CreateProject = Shared.CreateProject -> Async<int>
// type CreateUser = Shared.CreateUser -> Async<int>
type VerifyLoginInfo = LoginInfo -> Async<bool>

let usersQueryAsync (connString : string) () =
    let ctx = sql.GetDataContext connString
    query {
        for user in ctx.Testldapi.Users do
            select (User.FromSql user)
    }
    |> List.executeQueryAsync

let projectsQueryAsync (connString : string) () =
    let ctx = sql.GetDataContext connString
    printfn "About to query %s for projects" connString
    async {
        let sqlProjects = query {
            for project in ctx.Testldapi.Projects do
                where (project.IsPublic > 0y)
                select project
            }
        let! results = List.executeQueryAsync sqlProjects
        return List.map Project.FromSql results
    }

let projectsCountAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Testldapi.Projects do
                where (project.IsPublic > 0y)
                count
        }
    }

let realProjectsCountAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Testldapi.Projects do
                where (project.IsPublic > 0y &&
                       not (project.Name.ToLowerInvariant().Contains("test"))) // TODO: Figure out a better rule for what's a test project
                count
        }
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
    let ctx = sql.GetDataContext connString
    async {
        return query {
            for user in ctx.Testldapi.Users do
                select user.Login
                contains username }
    }

let projectExists (connString : string) projectCode =
    let ctx = sql.GetDataContext connString
    async {
        return query {
            for project in ctx.Testldapi.Projects do
                where (project.IsPublic > 0y)
                where (project.Identifier.IsSome)
                select project.Identifier.Value
                contains projectCode }
    }

let getUser (connString : string) username =
    let ctx = sql.GetDataContext connString
    async {
        return query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some (User.FromSql user))
                exactlyOneOrDefault }
    }

let getProject (connString : string) projectCode =
    let ctx = sql.GetDataContext connString
    async {
        return query {
            for project in ctx.Testldapi.Projects do
                where (not project.Identifier.IsNone)
                where (project.Identifier.Value = projectCode)
                select (Some (Project.FromSql project))
                exactlyOneOrDefault }
    }

let createProject (connString : string) (project : Shared.CreateProject) =
    async {
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
        // sqlProject.Status <- project.Status // int // default 1
        do! ctx.SubmitUpdatesAsync()
        return sqlProject.Id
    }

let projectsByUserRole (connString : string) username roleId =
    let ctx = sql.GetDataContext connString
    async {
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
                    where (user.UserId = requestedUser.Id &&
                        (if roleId < 0 then true else user.RoleId = roleId))
                    select project
                }
            let! projects = projectsQuery |> List.executeQueryAsync
            return projects |> List.map Project.FromSql
    }

let projectsByUser username connString = projectsByUserRole connString username -1

let projectsAndRolesByUserRole (connString : string) username (roleId : int) =
    let ctx = sql.GetDataContext connString
    async {
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
                    where (user.UserId = requestedUser.Id &&
                        (if roleId < 0 then true else user.RoleId = roleId))
                    select (project, role)
                }
            let! projects = projectsQuery |> List.executeQueryAsync
            return projects |> List.map (fun (project, role) -> Project.FromSql project, Role.FromSql role)
    }

let projectsAndRolesByUser (connString : string) username =
    projectsAndRolesByUserRole connString username -1

let roleNames (connString : string) () =
    let ctx = sql.GetDataContext connString
    query {
        for role in ctx.Testldapi.Roles do
            select (Role.FromSql role)  // TODO: Does this work? Or does it crash like the projects query did?
    }
    |> List.executeQueryAsync

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
    let ctx = sql.GetDataContext connString
    async {
        let! user = query { for user in ctx.Testldapi.Users do
                                where (user.Login = loginInfo.username)
                                select user } |> Seq.tryHeadAsync
        match user with
        | None -> return false
        | Some user -> return verifyPass loginInfo.password user.HashedPassword
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
            .AddSingleton<CreateProject>(createProject connString)
            .AddSingleton<ProjectsByUser>(projectsByUser connString)
            .AddSingleton<ProjectsByUserRole>(projectsByUserRole connString)
            .AddSingleton<ProjectsAndRolesByUser>(projectsAndRolesByUser connString)
            .AddSingleton<ProjectsAndRolesByUserRole>(projectsAndRolesByUserRole connString)
            .AddSingleton<VerifyLoginInfo>(verifyLoginInfo connString)
        |> ignore
