module Model

open System.Threading.Tasks
open Shared

type IModel =
    abstract ListUsers : int option -> int option -> Task<Dto.UserDetails []>
    abstract ListProjects : int option -> int option -> Task<Dto.ProjectList>
    abstract ListRoles : int option -> int option -> Task<(int * string)[]>
    abstract ListProjectsAndRoles : int option -> int option -> Task<Dto.ProjectDetails []>

    abstract SearchUsersExact : string -> Task<Dto.UserList>
    abstract SearchUsersLoose : string -> Task<Dto.UserList>

    abstract SearchProjectsExact : string -> Task<Dto.ProjectDetails []>
    abstract SearchProjectsLoose : string -> Task<Dto.ProjectDetails []>

    abstract GetUser : string -> Task<Dto.UserDetails option>
    abstract GetProject : string -> Task<Dto.ProjectDetails option>
    abstract GetProjectWithRoles : string -> Task<Dto.ProjectDetails option>

    abstract CountUsers : unit -> Task<int64>
    abstract CountProjects : unit -> Task<int64>
    abstract CountRealProjects : unit -> Task<int64>

    abstract UserExists : string -> Task<bool>
    abstract ProjectExists : string -> Task<bool>

    abstract CreateProject : Api.CreateProject -> Task<int>
    abstract CreateUser : Api.CreateUser -> Task<int>

    abstract UpsertUser : string -> Api.CreateUser -> Task<int>  // TODO: Change to Task<Result<int,string>> so we can return more meaningful error messages
    abstract ChangePassword : string -> Api.ChangePassword -> Task<bool>  // TODO: Change to Task<Result<int,string>> so we can return more meaningful error messages
    abstract UpdateUser : string -> Api.CreateUser -> Task<int>  // TODO: Change to Task<Result<int,string>> so we can return more meaningful error messages

    abstract AddMembership : string -> string -> string -> Task<bool>  // TODO: Change to Task<Result<unit,string>> so we can return more meaningful error messages
    abstract RemoveMembership : string -> string -> Task<bool>  // TODO: Ditto

    abstract ProjectsByUser : string -> Task<Dto.ProjectDetails []>
    abstract ProjectsByUserRole : string -> string -> Task<Dto.ProjectDetails []>
    abstract ProjectsAndRolesByUser : string -> Task<(Dto.ProjectDetails * string) []>
    abstract ProjectsAndRolesByUserRole : string -> string -> Task<(Dto.ProjectDetails * string) []>
    abstract LegacyProjectsAndRolesByUser : string -> Task<Dto.LegacyProjectDetails[]>

    abstract IsAdmin : string -> Task<bool>
    abstract EmailIsAdmin : string -> Task<bool>
    abstract IsUserManagerOfProject : string -> string -> Task<bool>
    abstract VerifyLoginInfo : Api.LoginCredentials -> Task<bool>
    abstract ArchiveProject : string -> Task<bool>


