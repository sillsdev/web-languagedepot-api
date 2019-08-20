module Model

open System
open FSharp.Data.Sql

[<Literal>]
let connString  = "Server=localhost;Database=testldapi;User=rmunn"
// TODO: Create constants that the build script can replace from a Config.fs file

[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__

type sql = SqlDataProvider<Common.DatabaseProviderTypes.MYSQL,
                           connString,
                           ResolutionPath = resolutionPath,
                           CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL>

let ctx = sql.GetDataContext()

// TODO: Add "is_archived" boolean to model (default false) so we can implement archiving; update queries that list or count projects to specify "where (isArchived = false)"
type Project = {
    Id : int
    Name : string
    Description : string option // Long
    Homepage : string option
    IsPublic : bool // default true
    ParentId : int // TODO: Determine what we get if the SQL value was NULL
    CreatedOn : DateTime // TODO: Determine what we get if the SQL value was NULL
    UpdatedOn : DateTime // TODO: Determine what we get if the SQL value was NULL
    Identifier : string option // 20 chars
    Status : int // default 1
} with
    static member mkProject id name now = {
        Id = id
        Name = name
        Description = None
        Homepage = None
        IsPublic = true
        ParentId = 0
        CreatedOn = now
        UpdatedOn = now
        Identifier = None
        Status = 1
    }
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

type User = {
    Id : int
    Login : string
    HashedPassword : string
    FirstName : string
    LastName : string
    Mail : string
    MailNotification : bool // default true
    Admin : bool // default false
    Status : int // default 1
    LastLoginOn : DateTime // TODO: Determine what we get if the SQL value was NULL
    Language : string option // 5 chars
    AuthSourceId : int // TODO: Determine what we get if the SQL value was NULL
    CreatedOn : DateTime // TODO: Determine what we get if the SQL value was NULL
    UpdatedOn : DateTime // TODO: Determine what we get if the SQL value was NULL
    Type : string option
} with
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

type Role = {
    Id : int
    Name : string
    Position : int // Default 1
    Assignable : bool
    Builtin : int // Default 0
    Permissions : string option // Long
} with
    static member FromSql (sqlRole : sql.dataContext.``testldapi.rolesEntity``) = {
        Id = sqlRole.Id
        Name = sqlRole.Name
        Position = sqlRole.Position
        Assignable = sqlRole.Assignable <> 0y
        Builtin = sqlRole.Builtin
        Permissions = sqlRole.Permissions |> Option.ofObj
    }

type Membership = {
    Id : int
    UserId : int // default 0
    ProjectId : int // default 0
    RoleId : int // default 0
    CreatedOn : DateTime // TODO: Determine what we get if the SQL value was NULL
    MailNotification : bool // default false
} with
    static member FromSql (sqlMember : sql.dataContext.``testldapi.membersEntity``) = {
        Id = sqlMember.Id
        UserId = sqlMember.UserId
        ProjectId = sqlMember.ProjectId
        RoleId = sqlMember.RoleId
        CreatedOn = sqlMember.CreatedOn
        MailNotification = sqlMember.MailNotification <> 0y
    }
// TODO: Decide whether all these fields in the Redmine SQL schema will actually be needed in our use case

type ListUsers = unit -> Async<User list>
type ListProjects = unit -> Async<Project list>
type ListRoleNamesAndIds = unit -> Async<(int * string) list>
type ProjectsByUserRole = string -> int -> Async<Project list>

let usersQueryAsync =
    query {
        for user in ctx.Testldapi.Users do
            select (User.FromSql user)
    }
    |> List.executeQueryAsync

let projectsQueryAsync =
    query {
        for project in ctx.Testldapi.Projects do
            select (Project.FromSql project)
    }
    |> List.executeQueryAsync

let userExists username =
    query {
        for user in ctx.Testldapi.Users do
        select user.Login
        contains username
    }

let projectExists projectCode =
    query {
        for project in ctx.Testldapi.Projects do
        select project.Identifier
        contains projectCode
    }

let projectsByUserRole username (role : int) =
    if not (userExists username) then
        async { return [] }
    else
        let requestedUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select user
                exactlyOneOrDefault
        }
        query {
            for project in ctx.Testldapi.Projects do
                join user in ctx.Testldapi.Members
                    on (project.Id = user.ProjectId)
                where (user.UserId = requestedUser.Id &&
                    (if role < 0 then true else user.RoleId = role))
                select project.Identifier
        }
        |> List.executeQueryAsync

let projectsByUser username = projectsByUserRole username -1

let projectsAndRolesByUserRole username (roleId : int) =
    if not (userExists username) then
        async { return [] }
    else
        let requestedUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select user
                exactlyOneOrDefault
        }
        query {
            for project in ctx.Testldapi.Projects do
                join user in ctx.Testldapi.Members
                    on (project.Id = user.ProjectId)
                join role in ctx.Testldapi.Roles on (user.RoleId = role.Id)
                where (user.UserId = requestedUser.Id &&
                    (if roleId < 0 then true else user.RoleId = roleId))
                select (project.Identifier, role.Name)
        }
        |> List.executeQueryAsync

let projectsAndRolesByUser username =
    projectsAndRolesByUserRole username -1

let roleNames() =
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
    async {
        let! user = query { for user in ctx.Testldapi.Users do
                                where (user.Login = loginInfo.username)
                                select user } |> Seq.tryHeadAsync
        match user with
        | None -> return false
        | Some user -> return verifyPass loginInfo.password user.HashedPassword
    }
