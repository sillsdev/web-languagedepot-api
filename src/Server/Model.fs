module Model

open System
open FSharp.Data.Sql

[<Literal>]
let connString  = "Server=localhost;Database=testldapi;User=rmunn"
// TODO: Create constants that the build script can replace from a Config.fs file

[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__ + "../../packages/sql/MySqlConnector/lib/netstandard2.0/MySqlConnector.dll"

type sql = SqlDataProvider<Common.DatabaseProviderTypes.MYSQL,
                           connString,
                           ResolutionPath = resolutionPath,
                           CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL>

let ctx = sql.GetDataContext()

let usersQueryAsync =
    query {
        for user in ctx.Testldapi.Users do
            select user
    }
    |> Seq.executeQueryAsync

let projectsQueryAsync =
    query {
        for project in ctx.Testldapi.Projects do
            select project
    }
    |> Seq.executeQueryAsync

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

let projectsByUser username =
    if not (userExists username) then
        async { return Seq.empty }
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
                where (user.UserId = requestedUser.Id)
                select project.Identifier
        }
        |> Seq.executeQueryAsync

let projectsByUserRole username role =
    raise (NotImplementedException("TODO"))

// TODO: Decide whether all these fields are actually needed

type Project = {
    Id : int
    Name : string
    Description : string option // Long
    Homepage : string option
    IsPublic : bool // default true
    ParentId : int option
    CreatedOn : DateTime option
    UpdatedOn : DateTime option
    Identifier : string option // 20 chars
    Status : int // default 1
} with
    static member mkProject id name now = {
        Id = id
        Name = name
        Description = None
        Homepage = None
        IsPublic = true
        ParentId = None
        CreatedOn = Some now
        UpdatedOn = Some now
        Identifier = None
        Status = 1
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
    LastLoginOn : DateTime option
    Language : string option // 5 chars
    AuthSourceId : int option
    CreatedOn : DateTime option
    UpdatedOn : DateTime option
    Type : string option
}

type Role = {
    Id : int
    Name : string
    Position : int option // Default 1
    Assignable : bool option // Default true
    Builtin : int // Default 0
    Permissions : string option // Long
}

type Membership = {
    Id : int
    UserId : int // default 0
    ProjectId : int // default 0
    RoleId : int // default 0
    CreatedOn : DateTime option
    MailNotification : bool // default false
}


let p = { Project.mkProject 5 "foo" DateTime.UtcNow with Description = Some "bar" }