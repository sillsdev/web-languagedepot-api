namespace Shared

type JsonSuccess<'a> = {
    ok : bool
    data : 'a
}

type JsonError = {
    ok : bool
    message : string
}

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
    Lft : int option
    Rgt : int option
    InheritMembers : bool // default false
    DefaultVersionId : string option
    DefaultAssignedToId : string option
}
// Lft and Rgt are part of Redmine's project hierarchy, which basically uses https://en.wikipedia.org/wiki/Nested_set_model to be able to do "is X a subproject of Y's hierarchy?" queries.
// If a project has no subprojects, then its Rgt will be equal to its Lft + 1. If a situation arises where we can't set both of these to NULL, then a good value for a project with
// no subprojects (and almost all projects created through this API will have no subprojects) is for Rgt to be equal to Id * 2, and Lft = Rgt - 1.

type ProjectForListing = {
    Id : int
    Name : string
    Typ : ProjectType
    CreatedOn : System.DateTime option
    Identifier : string option // 20 chars
}

// TODO: Expand and rename to ProjectDTO, or maybe ProjectCreationDTO if we need multiple kinds of DTO
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
    Admin : bool // default false
    Status : int // default 1
    LastLoginOn : System.DateTime option
    Language : string option // 5 chars
    AuthSourceId : int option
    CreatedOn : System.DateTime option
    UpdatedOn : System.DateTime option
    Type : string option
    IdentityUrl : string option
    MailNotification : string
    Salt : string option
    MustChangePasswd : bool
    PasswdChangedOn : System.DateTime option
}

(*
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` int(11) NOT NULL,
  `address` varchar(255) NOT NULL,
  `is_default` tinyint(1) NOT NULL DEFAULT '0',
  `notify` tinyint(1) NOT NULL DEFAULT '1',
  `created_on` datetime NOT NULL,
  `updated_on` datetime NOT NULL,
*)

type MailAddress = {
    Id : int
    UserId : int
    Address : string
    IsDefault : bool
    Notify : bool
    CreatedOn : System.DateTime
    UpdatedOn : System.DateTime
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
    Permissions : string option // Long string containing newlines
    IssuesVisibility : string // 30 chars, default "default"
    UsersVisibility : string // 30 chars, default "all"
    TimeEntriesVisibility : string // 30 chars, default "all"
    AllRolesManaged : bool // default true
    Settings : string option // Long string, default NULL
}

type Membership = {
    Id : int
    UserId : int // default 0
    ProjectId : int // default 0
    CreatedOn : System.DateTime option
    MailNotification : bool // default false
}

type MembershipRole = {
    Id : int
    MembershipId : int // default 0
    RoleId : int // default 0
    InheritedFrom : int option
}

// TODO: Add a Repositories type so that our API can manage that table as well
