module Model

open System

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