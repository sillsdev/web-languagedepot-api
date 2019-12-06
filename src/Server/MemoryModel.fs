module MemoryModel

open Shared

// Uses MemoryStorage to provide API calls

let listUsers : Model.ListUsers = fun _connString limit offset -> async {
    let limitFn = match limit with
                  | Some limit -> Seq.take limit
                  | None -> id
    let offsetFn = match offset with
                   | Some offset -> Seq.skip offset
                   | None -> id
    return MemoryStorage.userStorage.Values |> offsetFn |> limitFn |> List.ofSeq
}

let listProjects : Model.ListProjects = fun _connString -> async {
    return MemoryStorage.projectStorage.Values |> List.ofSeq
}

let countUsers = Model.CountUsers (fun _connString -> async {
    return MemoryStorage.userStorage.Count
})

let countProjects = Model.CountProjects (fun _connString -> async {
    return MemoryStorage.projectStorage.Count
})

let isRealProject (proj : Dto.ProjectDetails) =
    let projType = GuessProjectType.guessType proj.code proj.name proj.description
    projType <> Test && not (proj.code.StartsWith "test")

let countRealProjects = Model.CountRealProjects (fun _connString -> async {
    return MemoryStorage.projectStorage.Values |> Seq.filter isRealProject |> Seq.length
})

let listRoles : Model.ListRoles = fun _connString -> async {
    return Dto.standardRoles
}

let isMemberOf (proj : Dto.ProjectDetails) username =
    match proj.membership with
    | None -> false
    | Some members ->
        members |> Seq.map fst |> Seq.contains username

let projectsAndRolesByUser : Model.ProjectsAndRolesByUser = fun _connString username -> async {
    let projectsAndRoles = MemoryStorage.projectStorage.Values |> Seq.choose (fun proj ->
        match proj.membership with
        | None -> None
        | Some members ->
            let roles = members |> List.filter (fst >> ((=) username)) |> List.map snd
            if List.isEmpty roles then None else Some (proj, roles)
        )
    return List.ofSeq projectsAndRoles
}

let projectsAndRolesByUserRole : Model.ProjectsAndRolesByUserRole = fun _connString username roleType -> async {
    let! projectsAndRoles = projectsAndRolesByUser _connString username
    return (projectsAndRoles |> List.filter (fun (proj, roles) -> roles |> List.contains roleType))
}

let projectsByUser : Model.ProjectsByUser = fun _connString username -> async {
    let! projectsAndRoles = projectsAndRolesByUser _connString username
    return projectsAndRoles |> List.map fst
}

let projectsByUserRole : Model.ProjectsByUserRole = fun _connString username roleType -> async {
    let! projectsAndRoles = projectsAndRolesByUserRole _connString username roleType
    return projectsAndRoles |> List.map fst
}

let userExists = Model.UserExists (fun _connString username -> async {
    return MemoryStorage.userStorage.ContainsKey username
})

let projectExists = Model.ProjectExists (fun _connString code -> async {
    return MemoryStorage.projectStorage.ContainsKey code
})

let isAdmin = Model.IsAdmin (fun _connString username -> async {
    return username = "admin"
})

let searchUsersExact = Model.SearchUsersExact (fun _connString searchText -> async {
    return
        MemoryStorage.userStorage.Values
        |> Seq.filter (fun user ->
            user.username = searchText ||
            user.firstName = searchText ||
            user.lastName = searchText ||
            user.emailAddresses |> Option.contains searchText)
        |> List.ofSeq
})

let searchUsersLoose = Model.SearchUsersLoose (fun _connString searchText -> async {
    return
        MemoryStorage.userStorage.Values
        |> Seq.filter (fun user ->
            user.username.Contains(searchText) ||
            user.firstName.Contains(searchText) ||
            user.lastName.Contains(searchText) ||
            (user.emailAddresses |> Option.defaultValue "").Contains(searchText))
        |> List.ofSeq
})

let getUser : Model.GetUser = fun _connString username -> async {
    return
        match MemoryStorage.userStorage.TryGetValue username with
        | false, _ -> None
        | true, user -> Some user
}

let getProject : Model.GetProject = fun _connString projectCode -> async {
    return
        match MemoryStorage.projectStorage.TryGetValue projectCode with
        | false, _ -> None
        | true, project -> Some project
}

let createProject : Model.CreateProject = fun _connString createProjectApiData -> async {
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

let createUser : Model.CreateUser = fun _connString createUserApiData -> async {
    let user = mkUserDetailsFromApiData createUserApiData
    let added = MemoryStorage.userStorage.TryAdd(user.username,user)
    return if added then 1 else 0
}

let rec upsertUser : Model.UpsertUser = fun _connString username createUserApiData -> async {
    if username = createUserApiData.username then
        let newUser = mkUserDetailsFromApiData createUserApiData
        MemoryStorage.userStorage.AddOrUpdate(username, newUser, fun _ _ -> newUser) |> ignore
        return 1
    else
        match MemoryStorage.replaceUsername username createUserApiData.username with
        | Error _ -> return 0
        | Ok _ ->
            // Now recursive call to update the rest of the info
            return! upsertUser _connString createUserApiData.username createUserApiData
}

let changePassword : Model.ChangePassword = fun _connString username changePasswordApiData -> async {
    let cleartext = changePasswordApiData.password
    MemoryStorage.storeNewPassword username changePasswordApiData.password
    return true
}

let verifyLoginCredentials : Model.VerifyLoginCredentials = fun _connString loginCredentials -> async {
    // Skip verifying login credentials until I update the sample data to have "x" as the default password everywhere
    return true
    // match MemoryStorage.passwordStorage.TryGetValue loginCredentials.username with
    // | false, _ -> return false  // User not found also returns false, so we don't disclose the lack of a username
    // | true, passwordDetails ->
    //     let hashedPassword = PasswordHashing.hashPassword passwordDetails.salt loginCredentials.password
    //     return hashedPassword = passwordDetails.hashedPassword
}

let addOrRemoveInList isAdd item lst =
    if isAdd then
        if lst |> List.contains item then lst else item :: lst
    else
        lst |> List.filter (fun listItem -> listItem <> item)

let addOrRemoveMembership isAdd _connString username projectCode (roleType : RoleType) = async {
    match MemoryStorage.userStorage.TryGetValue username with
    | false, _ -> return false
    | true, _ ->  // We don't need user details, we just want to make sure the user exists
        match MemoryStorage.projectStorage.TryGetValue projectCode with
        | false, _ -> return false
        | true, projectDetails ->
            let update (details : Dto.ProjectDetails) =
                let memberList = details.membership |> Option.defaultValue []
                let newMemberList = memberList |> addOrRemoveInList isAdd (username, roleType)
                { details with membership = Some newMemberList }
            MemoryStorage.projectStorage.AddOrUpdate(username,
                (fun _ -> update projectDetails),
                (fun _ oldDetails -> update oldDetails)) |> ignore
            return true
}

let addMembership = Model.AddMembership (addOrRemoveMembership true)
let removeMembership = Model.RemoveMembership (addOrRemoveMembership false)

let removeUserFromAllRolesInProject = Model.RemoveUserFromAllRolesInProject (fun _connString (username : string) (projectCode : string) -> async {
    match MemoryStorage.userStorage.TryGetValue username with
    | false, _ -> return false
    | true, _ ->  // We don't need user details, we just want to make sure the user exists
        match MemoryStorage.projectStorage.TryGetValue projectCode with
        | false, _ -> return false
        | true, projectDetails ->
            let update (details : Dto.ProjectDetails) =
                let memberList = details.membership |> Option.defaultValue []
                let newMemberList = memberList |> List.filter (fst >> ((<>) username))
                { details with membership = Some newMemberList }
            MemoryStorage.projectStorage.AddOrUpdate(username,
                (fun _ -> update projectDetails),
                (fun _ oldDetails -> update oldDetails)) |> ignore
            return true
})

let archiveProject : Model.ArchiveProject = fun projectCode -> raise (System.NotImplementedException("Not implemented"))

module ModelRegistration =
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.DependencyInjection.Extensions

    let registerServices (builder : IServiceCollection) (_connString : string) =
        MemoryStorage.initFromData SampleData.Users SampleData.Projects
        builder
            .RemoveAll<Model.ListUsers>()
            .AddSingleton<Model.ListUsers>(listUsers)
            .RemoveAll<Model.ListProjects>()
            .AddSingleton<Model.ListProjects>(listProjects)
            .RemoveAll<Model.CountUsers>()
            .AddSingleton<Model.CountUsers>(countUsers)
            .RemoveAll<Model.CountProjects>()
            .AddSingleton<Model.CountProjects>(countProjects)
            .RemoveAll<Model.CountRealProjects>()
            .AddSingleton<Model.CountRealProjects>(countRealProjects)
            .RemoveAll<Model.ListRoles>()
            .AddSingleton<Model.ListRoles>(listRoles)
            .RemoveAll<Model.UserExists>()
            .AddSingleton<Model.UserExists>(userExists)
            .RemoveAll<Model.ProjectExists>()
            .AddSingleton<Model.ProjectExists>(projectExists)
            .RemoveAll<Model.IsAdmin>()
            .AddSingleton<Model.IsAdmin>(isAdmin)
            .RemoveAll<Model.SearchUsersExact>()
            .AddSingleton<Model.SearchUsersExact>(searchUsersExact)
            .RemoveAll<Model.SearchUsersLoose>()
            .AddSingleton<Model.SearchUsersLoose>(searchUsersLoose)
            .RemoveAll<Model.GetUser>()
            .AddSingleton<Model.GetUser>(getUser)
            .RemoveAll<Model.GetProject>()
            .AddSingleton<Model.GetProject>(getProject)
            .RemoveAll<Model.CreateProject>()
            .AddSingleton<Model.CreateProject>(createProject)
            .RemoveAll<Model.CreateUser>()
            .AddSingleton<Model.CreateUser>(createUser)
            .RemoveAll<Model.UpsertUser>()
            .AddSingleton<Model.UpsertUser>(upsertUser)
            .RemoveAll<Model.ChangePassword>()
            .AddSingleton<Model.ChangePassword>(changePassword)
            .RemoveAll<Model.ProjectsByUser>()
            .AddSingleton<Model.ProjectsByUser>(projectsByUser)
            .RemoveAll<Model.ProjectsByUserRole>()
            .AddSingleton<Model.ProjectsByUserRole>(projectsByUserRole)
            .RemoveAll<Model.ProjectsAndRolesByUser>()
            .AddSingleton<Model.ProjectsAndRolesByUser>(projectsAndRolesByUser)
            .RemoveAll<Model.ProjectsAndRolesByUserRole>()
            .AddSingleton<Model.ProjectsAndRolesByUserRole>(projectsAndRolesByUserRole)
            .RemoveAll<Model.VerifyLoginCredentials>()
            .AddSingleton<Model.VerifyLoginCredentials>(verifyLoginCredentials)
            .RemoveAll<Model.AddMembership>()
            .AddSingleton<Model.AddMembership>(addMembership)
            .RemoveAll<Model.RemoveMembership>()
            .AddSingleton<Model.RemoveMembership>(removeMembership)
            .RemoveAll<Model.RemoveUserFromAllRolesInProject>()
            .AddSingleton<Model.RemoveUserFromAllRolesInProject>(removeUserFromAllRolesInProject)
            .RemoveAll<Model.ArchiveProject>()
            .AddSingleton<Model.ArchiveProject>(archiveProject)
        |> ignore
