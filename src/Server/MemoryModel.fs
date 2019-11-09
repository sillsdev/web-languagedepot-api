module MemoryModel

open Shared

// Uses MemoryStorage to provide API calls

type ListUsers = unit -> Async<Dto.UserDetails list>
type ListProjects = bool -> Async<Dto.ProjectList>
type CountUsers = CountUsers of (unit -> Async<int>)
type CountProjects = CountProjects of (unit -> Async<int>)
type CountRealProjects = CountRealProjects of (unit -> Async<int>)
type ListRoles = unit -> Async<Dto.RoleDetails list>
type ProjectsByUser = string -> Async<Dto.ProjectDetails list>
type ProjectsByUserRole = string -> RoleType -> Async<Dto.ProjectDetails list>
type ProjectsAndRolesByUser = string -> Async<(Dto.ProjectDetails * RoleType list) list>
type ProjectsAndRolesByUserRole = string -> RoleType -> Async<(Dto.ProjectDetails * RoleType list) list>
type UserExists = UserExists of (string -> Async<bool>)
type ProjectExists = ProjectExists of (string -> Async<bool>)
type GetUser = string -> Async<Dto.UserDetails option>
type GetProject = bool -> string -> Async<Dto.ProjectDetails option>
type CreateProject = Api.CreateProject -> Async<int>
type CreateUser = Api.CreateUser -> Async<int>
type UpsertUser = string -> Api.CreateUser -> Async<int>
type ChangePassword = string -> Api.ChangePassword -> Async<bool>
type VerifyLoginCredentials = Api.LoginCredentials -> Async<bool>
type AddMembership = AddMembership of (string -> string -> RoleType -> Async<bool>)  // TODO: Change this in Model.fs as well
type RemoveMembership = RemoveMembership of (string -> string -> RoleType -> Async<bool>)
type ArchiveProject = bool -> string -> Async<bool>

let listUsers : Model.ListUsers = fun() -> async {
    return MemoryStorage.userStorage.Values |> List.ofSeq
}

let listProjects : Model.ListProjects = fun b -> async {
    return MemoryStorage.projectStorage.Values |> List.ofSeq // ignore public/private distinction here
}

let countUsers = Model.CountUsers (fun() -> async {
    return MemoryStorage.userStorage.Count
})

let countProjects = Model.CountProjects (fun() -> async {
    return MemoryStorage.projectStorage.Count
})

let isRealProject (proj : Dto.ProjectDetails) =
    let projType = GuessProjectType.guessType proj.code proj.name proj.description
    projType <> Test && not (proj.code.StartsWith "test")

let countRealProjects = Model.CountRealProjects (fun() -> async {
    return MemoryStorage.projectStorage.Values |> Seq.filter isRealProject |> Seq.length
})

let listRoles : Model.ListRoles = fun() -> async {
    return Dto.standardRoles
}

let isMemberOf (proj : Dto.ProjectDetails) username =
    match proj.membership with
    | None -> false
    | Some members ->
        members.managers |> List.contains username ||
        members.contributors |> List.contains username ||
        members.observers |> List.contains username ||
        members.programmers |> List.contains username

let projectsAndRolesByUser : Model.ProjectsAndRolesByUser = fun username -> async {
    let projectsAndRoles = MemoryStorage.projectStorage.Values |> Seq.choose (fun proj ->
        match proj.membership with
        | None -> None
        | Some members ->
            let roles = [
                if members.managers |> List.contains username then yield Manager
                if members.contributors |> List.contains username then yield Contributor
                if members.observers |> List.contains username then yield Observer
                if members.programmers |> List.contains username then yield Programmer
            ]
            if List.isEmpty roles then None else Some (proj, roles)
        )
    return List.ofSeq projectsAndRoles
}

let projectsAndRolesByUserRole : Model.ProjectsAndRolesByUserRole = fun username roleType -> async {
    let! projectsAndRoles = projectsAndRolesByUser username
    return (projectsAndRoles |> List.filter (fun (proj, roles) -> roles |> List.contains roleType))
}

let projectsByUser : Model.ProjectsByUser = fun username -> async {
    let! projectsAndRoles = projectsAndRolesByUser username
    return projectsAndRoles |> List.map fst
}

let projectsByUserRole : Model.ProjectsByUserRole = fun username roleType -> async {
    let! projectsAndRoles = projectsAndRolesByUserRole username roleType
    return projectsAndRoles |> List.map fst
}

// TODO: Keep implementing the internal API below.

let userExists = Model.UserExists (fun username -> async {
    return MemoryStorage.userStorage.ContainsKey username
})

let projectExists = Model.ProjectExists (fun code -> async {
    return MemoryStorage.projectStorage.ContainsKey code
})

let getUser : Model.GetUser = fun username -> async {
    return
        match MemoryStorage.userStorage.TryGetValue username with
        | false, _ -> None
        | true, user -> Some user
}

let getProject : Model.GetProject = fun _ projectCode -> async {
    return
        match MemoryStorage.projectStorage.TryGetValue projectCode with
        | false, _ -> None
        | true, project -> Some project
}

let createProject : Model.CreateProject = fun createProjectApiData -> async {
    let proj : Dto.ProjectDetails = {
        code = createProjectApiData.code
        name = createProjectApiData.name
        description = createProjectApiData.description |> Option.defaultValue ""
        membership = createProjectApiData.initialMembers
    }
    let added = MemoryStorage.projectStorage.TryAdd(proj.code,proj)
    return if added then 1 else 0
}

let mkUserDetailsFromApiData (createUserApiData : Api.CreateUser) : Dto.UserDetails = {
    username = createUserApiData.username
    firstName = createUserApiData.firstName
    lastName = createUserApiData.lastName
    emailAddresses = createUserApiData.emailAddresses
    language = createUserApiData.language |> Option.defaultValue "en"
}

let createUser : Model.CreateUser = fun createUserApiData -> async {
    let user = mkUserDetailsFromApiData createUserApiData
    let added = MemoryStorage.userStorage.TryAdd(user.username,user)
    return if added then 1 else 0
}

let rec upsertUser : Model.UpsertUser = fun username createUserApiData -> async {
    if username = createUserApiData.username then
        let newUser = mkUserDetailsFromApiData createUserApiData
        MemoryStorage.userStorage.AddOrUpdate(username, newUser, fun _ _ -> newUser) |> ignore
        return 1
    else
        match MemoryStorage.replaceUsername username createUserApiData.username with
        | Error _ -> return 0
        | Ok _ ->
            // Now recursive call to update the rest of the info
            return! upsertUser createUserApiData.username createUserApiData
}

let changePassword : Model.ChangePassword = fun username changePasswordApiData -> async {
    let cleartext = changePasswordApiData.password
    MemoryStorage.storeNewPassword username changePasswordApiData.password
    return true
}

let verifyLoginCredentials : Model.VerifyLoginCredentials = fun loginCredentials -> async {
    match MemoryStorage.passwordStorage.TryGetValue loginCredentials.username with
    | false, _ -> return false  // User not found also returns false, so we don't disclose the lack of a username
    | true, passwordDetails ->
        let hashedPassword = PasswordHashing.hashPassword passwordDetails.salt loginCredentials.password
        return hashedPassword = passwordDetails.hashedPassword
}

let addOrRemoveInList isAdd item lst =
    if isAdd then
        if lst |> List.contains item then lst else item :: lst
    else
        lst |> List.filter (fun listItem -> listItem <> item)

let addOrRemoveMembership isAdd username projectCode (roleType : RoleType) = async {
    match MemoryStorage.userStorage.TryGetValue username with
    | false, _ -> return false
    | true, _ ->  // We don't need user details, we just want to make sure the user exists
        match MemoryStorage.projectStorage.TryGetValue projectCode with
        | false, _ -> return false
        | true, projectDetails ->
            let update (details : Dto.ProjectDetails) =
                let memberList = details.membership |> Option.defaultWith (fun _ -> {
                    managers = []
                    contributors = []
                    observers = []
                    programmers = []
                })
                let newMemberList =
                    match roleType with
                    | Manager -> { memberList with managers = addOrRemoveInList isAdd username memberList.managers }
                    | Contributor -> { memberList with contributors = addOrRemoveInList isAdd username memberList.contributors }
                    | Observer -> { memberList with observers = addOrRemoveInList isAdd username memberList.observers }
                    | Programmer -> { memberList with programmers = addOrRemoveInList isAdd username memberList.programmers }
                { details with membership = Some newMemberList }
            MemoryStorage.projectStorage.AddOrUpdate(username,
                (fun _ -> update projectDetails),
                (fun _ oldDetails -> update oldDetails)) |> ignore
            return true
}

let addMembership = Model.AddMembership (addOrRemoveMembership true)
let removeMembership = Model.RemoveMembership (addOrRemoveMembership false)

let archiveProject : Model.ArchiveProject = fun isPublic projectCode -> raise (System.NotImplementedException("Not implemented"))

module ModelRegistration =
    open Microsoft.Extensions.DependencyInjection

    let registerServices (builder : IServiceCollection) (connString : string) =
        builder
            .AddSingleton<Model.ListUsers>(listUsers)
            .AddSingleton<Model.ListProjects>(listProjects)
            .AddSingleton<Model.CountUsers>(countUsers)
            .AddSingleton<Model.CountProjects>(countProjects)
            .AddSingleton<Model.CountRealProjects>(countRealProjects)
            .AddSingleton<Model.ListRoles>(listRoles)
            .AddSingleton<Model.UserExists>(userExists)
            .AddSingleton<Model.ProjectExists>(projectExists)
            .AddSingleton<Model.GetUser>(getUser)
            .AddSingleton<Model.GetProject>(getProject)
            .AddSingleton<Model.CreateProject>(createProject)
            .AddSingleton<Model.CreateUser>(createUser)
            .AddSingleton<Model.UpsertUser>(upsertUser)
            .AddSingleton<Model.ChangePassword>(changePassword)
            .AddSingleton<Model.ProjectsByUser>(projectsByUser)
            .AddSingleton<Model.ProjectsByUserRole>(projectsByUserRole)
            .AddSingleton<Model.ProjectsAndRolesByUser>(projectsAndRolesByUser)
            .AddSingleton<Model.ProjectsAndRolesByUserRole>(projectsAndRolesByUserRole)
            .AddSingleton<Model.VerifyLoginCredentials>(verifyLoginCredentials)
            .AddSingleton<Model.AddMembership>(addMembership)
            .AddSingleton<Model.RemoveMembership>(removeMembership)
            .AddSingleton<Model.ArchiveProject>(archiveProject)
        |> ignore
