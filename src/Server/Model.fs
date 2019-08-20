module Model

open System
open FSharp.Data.Sql
open Shared

[<Literal>]
let connString  = "Server=localhost;Database=testldapi;User=rmunn"
// TODO: Create constants that the build script can replace from a Config.fs file

[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__

type sql = SqlDataProvider<Common.DatabaseProviderTypes.MYSQL,
                           connString,
                           ResolutionPath = resolutionPath,
                           CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL>

// TODO: Add "is_archived" boolean to model (default false) so we can implement archiving; update queries that list or count projects to specify "where (isArchived = false)"
type Shared.Project with
    static member FromSql (sqlProject : sql.dataContext.``testldapi.projectsEntity``) = {
        Id = sqlProject.Id
        Name = sqlProject.Name
        Description = sqlProject.Description |> Option.ofObj
        Homepage = sqlProject.Homepage |> Option.ofObj
        IsPublic = sqlProject.IsPublic <> 0y
        ParentId = sqlProject.ParentId
        CreatedOn = sqlProject.CreatedOn
        UpdatedOn = sqlProject.UpdatedOn
        Identifier = sqlProject.Identifier |> Option.ofObj
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
        Language = sqlUser.Language |> Option.ofObj
        AuthSourceId = sqlUser.AuthSourceId
        CreatedOn = sqlUser.CreatedOn
        UpdatedOn = sqlUser.UpdatedOn
        Type = sqlUser.Type |> Option.ofObj
    }

type Shared.Role with
    static member FromSql (sqlRole : sql.dataContext.``testldapi.rolesEntity``) = {
        Id = sqlRole.Id
        Name = sqlRole.Name
        Position = sqlRole.Position
        Assignable = sqlRole.Assignable <> 0y
        Builtin = sqlRole.Builtin
        Permissions = sqlRole.Permissions |> Option.ofObj
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

// TODO: Register these function signatures with Giraffe so that we can build mocks for testing
type ListUsers = unit -> Async<User list>
type ListProjects = unit -> Async<Project list>
type ListRoleNamesAndIds = unit -> Async<(int * string) list>
type ProjectsByUserRole = string -> int -> Async<Project list>
type UserExists = string -> Async<bool>
type ProjectExists = string -> Async<bool>
type GetUser = string -> Async<User option>
type GetProject = string -> Async<Project option>

let usersQueryAsync =
    let ctx = sql.GetDataContext()
    query {
        for user in ctx.Testldapi.Users do
            select (User.FromSql user)
    }
    |> List.executeQueryAsync

let projectsQueryAsync =
    let ctx = sql.GetDataContext()
    query {
        for project in ctx.Testldapi.Projects do
            select (Project.FromSql project)
    }
    |> List.executeQueryAsync

let userExists username =
    let ctx = sql.GetDataContext()
    async {
        return query {
            for user in ctx.Testldapi.Users do
                select user.Login
                contains username }
    }

let projectExists projectCode =
    let ctx = sql.GetDataContext()
    async {
        return query {
            for project in ctx.Testldapi.Projects do
                select project.Identifier
                contains projectCode }
    }

let getUser username =
    let ctx = sql.GetDataContext()
    async {
        return query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some (User.FromSql user))
                exactlyOneOrDefault }
    }

let getProject projectCode =
    let ctx = sql.GetDataContext()
    async {
        return query {
            for project in ctx.Testldapi.Projects do
                where (project.Identifier = projectCode)
                select (Some (Project.FromSql project))
                exactlyOneOrDefault }
    }

let createProject (project : Project) =
    async {
        let ctx = sql.GetDataContext()
        let sqlProject = ctx.Testldapi.Projects.Create()
        // sqlProject.Id <- project.Id // int
        sqlProject.Name <- project.Name // string
        match project.Description with
        | None -> ()
        | Some desc -> sqlProject.Description <- desc // string option // Long
        match project.Homepage with
        | None -> ()
        | Some page -> sqlProject.Homepage <- page // string option // Long
        sqlProject.IsPublic <- if project.IsPublic then 1y else 0y
        sqlProject.ParentId <- project.ParentId // int // TODO: Determine what we get if the SQL value was NULL
        sqlProject.CreatedOn <- project.CreatedOn // System.DateTime // TODO: Determine what we get if the SQL value was NULL
        sqlProject.UpdatedOn <- project.UpdatedOn // System.DateTime // TODO: Determine what we get if the SQL value was NULL
        sqlProject.Identifier <- project.Identifier |> Option.defaultValue "" // string option // 20 chars
        sqlProject.Status <- project.Status // int // default 1
        do! ctx.SubmitUpdatesAsync()
        return sqlProject.Id
    }

let projectsByUserRole username (role : int) =
    let ctx = sql.GetDataContext()
    async {
        let requestedUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some user)
                exactlyOneOrDefault }
        match requestedUser with
        | None -> return []
        | Some requestedUser ->
            return! query {
                for project in ctx.Testldapi.Projects do
                    join user in ctx.Testldapi.Members
                        on (project.Id = user.ProjectId)
                    where (user.UserId = requestedUser.Id &&
                        (if role < 0 then true else user.RoleId = role))
                    select project.Identifier
            } |> List.executeQueryAsync
    }

let projectsByUser username = projectsByUserRole username -1

let projectsAndRolesByUserRole username (roleId : int) =
    let ctx = sql.GetDataContext()
    async {
        let requestedUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some user)
                exactlyOneOrDefault }
        match requestedUser with
        | None -> return []
        | Some requestedUser ->
            return! query {
                for project in ctx.Testldapi.Projects do
                    join user in ctx.Testldapi.Members
                        on (project.Id = user.ProjectId)
                    join role in ctx.Testldapi.Roles on (user.RoleId = role.Id)
                    where (user.UserId = requestedUser.Id &&
                        (if roleId < 0 then true else user.RoleId = roleId))
                    select (project.Identifier, role.Name)
            } |> List.executeQueryAsync
    }

let projectsAndRolesByUser username =
    projectsAndRolesByUserRole username -1

let roleNames() =
    let ctx = sql.GetDataContext()
    query {
        for role in ctx.Testldapi.Roles do
            select (role.Id, role.Name)
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

let verifyLoginInfo (loginInfo : Shared.LoginInfo) =
    let ctx = sql.GetDataContext()
    async {
        let! user = query { for user in ctx.Testldapi.Users do
                                where (user.Login = loginInfo.username)
                                select user } |> Seq.tryHeadAsync
        match user with
        | None -> return false
        | Some user -> return verifyPass loginInfo.password user.HashedPassword
    }
