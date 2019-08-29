namespace Shared

type SharedUser = {
    Name : string
    Email : string
}

type SharedProjects = {
    Projects : string list
}

type LoginInfo = {
    username : string
    password : string
}

type PatchProjects = {
    addUser : SharedUser option
    removeUser : SharedUser option
}

// TODO: Decide whether all these fields in the Redmine SQL schema will actually be needed in our use case
type Project = {
    Id : int
    Name : string
    Description : string option // Long
    Homepage : string option
    IsPublic : bool // default true
    ParentId : int option
    CreatedOn : System.DateTime option
    UpdatedOn : System.DateTime option
    Identifier : string option // 20 chars
    Status : int // default 1
}

type CreateProject = { // Just a subset of fields
    Name : string
    Description : string option // Long
    Identifier : string option // 20 chars
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
    LastLoginOn : System.DateTime option
    Language : string option // 5 chars
    AuthSourceId : int option
    CreatedOn : System.DateTime option
    UpdatedOn : System.DateTime option
    Type : string option
}

type Role = {
    Id : int
    Name : string
    Position : int option // Default 1
    Assignable : bool
    Builtin : int // Default 0
    Permissions : string option // Long
}

type Membership = {
    Id : int
    UserId : int // default 0
    ProjectId : int // default 0
    RoleId : int // default 0
    CreatedOn : System.DateTime option
    MailNotification : bool // default false
}
