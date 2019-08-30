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

type ProjectType =
    | Unknown
    | Lift
    | Flex
    | OurWord
    | OneStory
    | Test
    | AdaptIt
    | School

module ProjectStatus =
    // Values copied from Redmine
    let [<Literal>] Active = 1
    let [<Literal>] Closed = 5
    let [<Literal>] Archived = 9

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

type ProjectForListing = {
    Id : int
    Name : string
    Typ : ProjectType
    CreatedOn : System.DateTime option
    Identifier : string option // 20 chars
}

type CreateProject = { // Just a subset of fields
    Name : string
    Description : string option // Long
    Identifier : string option // 20 chars
}

module UserStatus =
    // Values copied from Redmine
    let [<Literal>] Anonymous = 0
    let [<Literal>] Active = 1
    let [<Literal>] Registered = 2
    let [<Literal>] Locked = 3

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

type CreateUser = { // Just a subset of fields
    Login : string
    CleartextPassword : string
    FirstName : string
    LastName : string
    Mail : string
}

type UpdateUser = {
    User : User
    NewPassword : string option
}

type ChangePassword = {
    OldPassword : string
    NewPassword : string
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
