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

let usersQueryAsync =
    query {
        for user in ctx.Testldapi.Users do
            select user
    }
    |> List.executeQueryAsync

let projectsQueryAsync =
    query {
        for project in ctx.Testldapi.Projects do
            select project
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

let projectsByUser username =
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
                where (user.UserId = requestedUser.Id)
                select project.Identifier
        }
        |> List.executeQueryAsync

let projectsByUserRole username role =
    raise (NotImplementedException("TODO"))

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