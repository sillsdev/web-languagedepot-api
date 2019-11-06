module MemoryStorage

open System
open System.Collections.Concurrent
open System.Collections.Generic
open Shared
open BCrypt.Net

let userStorage = new ConcurrentDictionary<string, User>()
let mailStorage = new ConcurrentDictionary<string, MailAddress>()
let projectStorage = new ConcurrentDictionary<string, Project>()
let roleStorage = new ConcurrentDictionary<int, Role>()
let membershipStorage = new ConcurrentDictionary<int, Membership>()
let membershipRoleStorage = new ConcurrentDictionary<int, MembershipRole>()

let counter init =
    let mutable n = init
    fun() ->
        n <- n + 1
        n

let projectIdCounter = counter 0
let userIdCounter = counter 0
let mailIdCounter = counter 0
let roleIdCounter = counter 0
let membershipIdCounter = counter 0
let memberRoleIdCounter = counter 0

// TODO: Move this list to the unit test setup
let roles : Role list = [
    { Id = 1; Name = "Non member"; Position = Some 1; Assignable = true; Builtin = 1; Permissions = None; IssuesVisibility = "default"; UsersVisibility = "all"; TimeEntriesVisibility = "all"; AllRolesManaged = true; Settings = None }
    { Id = 2; Name = "Anonymous"; Position = Some 2; Assignable = true; Builtin = 2; Permissions = None; IssuesVisibility = "default"; UsersVisibility = "all"; TimeEntriesVisibility = "all"; AllRolesManaged = true; Settings = None }
    { Id = 3; Name = "Manager"; Position = Some 3; Assignable = true; Builtin = 0; Permissions = None; IssuesVisibility = "default"; UsersVisibility = "all"; TimeEntriesVisibility = "all"; AllRolesManaged = true; Settings = None }
    { Id = 4; Name = "Contributer"; Position = Some 4; Assignable = true; Builtin = 0; Permissions = None; IssuesVisibility = "default"; UsersVisibility = "all"; TimeEntriesVisibility = "all"; AllRolesManaged = true; Settings = None }
    { Id = 5; Name = "Obv - do not use"; Position = Some 5; Assignable = true; Builtin = 0; Permissions = None; IssuesVisibility = "default"; UsersVisibility = "all"; TimeEntriesVisibility = "all"; AllRolesManaged = true; Settings = None }
    { Id = 6; Name = "LanguageDepotProgrammer"; Position = Some 6; Assignable = true; Builtin = 0; Permissions = None; IssuesVisibility = "default"; UsersVisibility = "all"; TimeEntriesVisibility = "all"; AllRolesManaged = true; Settings = None }
]

let mkProjectForListing (project : Project) : ProjectForListing =
    {
        Id = project.Id
        Name = project.Name
        Typ = GuessProjectType.guessType project.Identifier project.Name project.Description
        CreatedOn = project.CreatedOn
        Identifier = project.Identifier
    }

let usersQueryAsync() = async {
    return userStorage.Values |> List.ofSeq
}

let projectsQueryAsync isPublic = async {
    return
        projectStorage.Values
        |> Seq.filter (fun project -> project.IsPublic = isPublic)
        |> Seq.map mkProjectForListing
        |> List.ofSeq
}

let usersCountAsync() = async {
    return userStorage.Count
}

let projectsCountAsync() = async {
    return projectStorage.Count
}

let realProjectsCountAsync() = async {
    return
        projectStorage.Values
        |> Seq.map mkProjectForListing
        // TODO: The filter below is used in both Model and here. We should extract it into a function.
        |> Seq.filter (fun project -> project.Typ <> Test && not ((defaultArg project.Identifier "").StartsWith "test"))
        |> Seq.length
}

let listRoles() = async {
    return roleStorage.Values |> List.ofSeq
}

let withUsernameCheck (username : string) (cont : User -> 'a list) =
    match userStorage.TryGetValue username with
    | false, _ -> []
    | true, user -> cont user

let projectsByPredicate pred projection =
    let memberships = membershipStorage.Values |> Seq.filter pred
    let projectsLookup =
        new Dictionary<int, Project>(
            projectStorage
            |> Seq.map (fun kv -> let project = kv.Value in KeyValuePair(project.Id, project)))
    memberships
    |> Seq.choose (fun membership ->
        match projectsLookup.TryGetValue membership.ProjectId with
        | true, project -> Some (projection membership project)
        | false, _ -> None)
    |> List.ofSeq

let projectsByRoleBasedPredicate pred projection =
    let memberships = membershipStorage.Values |> Seq.filter (fun membership -> match membershipRoleStorage.TryGetValue membership.Id with | false, _ -> false | true, memberRole -> match roleStorage.TryGetValue memberRole.RoleId with | false, _ -> false | true, role -> pred membership role)
    let projectsLookup =
        new Dictionary<int, Project>(
            projectStorage
            |> Seq.map (fun kv -> let project = kv.Value in KeyValuePair(project.Id, project)))
    memberships
    |> Seq.choose (fun membership ->
        match projectsLookup.TryGetValue membership.ProjectId with
        | true, project -> Some (projection membership roleStorage.[membershipRoleStorage.[membership.Id].RoleId] project)
        | false, _ -> None)
    |> List.ofSeq

let projectsByUser (username : string) = async {
    match userStorage.TryGetValue username with
    | false, _ ->
        return []
    | true, user ->
        return projectsByPredicate (fun membership -> membership.UserId = user.Id) (fun _ project -> project)
}

let projectsByUserRole (username : string) (roleId : int) = async {
    match userStorage.TryGetValue username with
    | false, _ ->
        return []
    | true, user ->
        return projectsByRoleBasedPredicate (fun membership role -> membership.UserId = user.Id && role.Id = roleId) (fun _ _ project -> project)
}

let projectsAndRolesByUser (username : string) =
    async {
        match userStorage.TryGetValue username with
        | false, _ ->
            return []
        | true, user ->
            return projectsByRoleBasedPredicate (fun membership _ -> membership.UserId = user.Id) (fun _ role project -> project, role)
    }

let projectsAndRolesByUserRole (username : string) (roleId : int) = async {
    match userStorage.TryGetValue username with
    | false, _ ->
        return []
    | true, user ->
        return projectsByRoleBasedPredicate (fun membership role -> membership.UserId = user.Id && role.Id = roleId) (fun _ role project -> project, role)
}

let userExists username = async {
    return userStorage.ContainsKey username
}

let projectExists projectCode = async {
    return projectStorage.ContainsKey projectCode
}

let getUser username = async {
    match userStorage.TryGetValue username with
    | false, _ -> return None
    | true, user -> return Some user
}

let getProject isPublic projectCode = async {
    match projectStorage.TryGetValue projectCode with
    | false, _ -> return None
    | true, project ->
        if project.IsPublic = isPublic then
            return Some project
        else
            return None
}

let createProject (projectDto : CreateProject) = async {
    // TODO: Once real API shape becomes Async<int option> instead of Async<int>, update this to match
    match projectDto.Identifier with
    | None -> return -1  // Would become None
    | Some projectCode ->
        if projectStorage.ContainsKey projectCode then
            return -1  // Would become None
        else
            let now = DateTime.UtcNow
            let newProject : Project = {
                Id = projectIdCounter()
                Name = projectDto.Name
                Description = projectDto.Description
                Homepage = None
                IsPublic = true
                ParentId = None
                CreatedOn = Some now
                UpdatedOn = Some now
                Identifier = Some projectCode
                Status = ProjectStatus.Active
                Lft = None
                Rgt = None
                InheritMembers = false
                DefaultVersionId = None
                DefaultAssignedToId = None
            }
            projectStorage.AddOrUpdate(projectCode, newProject, fun _ _ -> newProject) |> ignore
            return newProject.Id
}

let mkUserFromDto (userDto : CreateUser) : User * MailAddress =
    let now = DateTime.UtcNow
    let user = {
        Id = userIdCounter()
        Login = userDto.Login
        HashedPassword = BCrypt.HashPassword userDto.CleartextPassword  // TODO: Match the model change once we make it
        FirstName = userDto.FirstName
        LastName = userDto.LastName
        Admin = userDto.Login = "admin"
        Status = UserStatus.Active
        LastLoginOn = None
        Language = Some "en"
        AuthSourceId = None
        CreatedOn = Some now
        UpdatedOn = Some now
        Type = None
        IdentityUrl = None
        MailNotification = "only_my_events"
        Salt = None
        MustChangePasswd = false
        PasswdChangedOn = None }
    let mail = {
        Id = mailIdCounter()
        UserId = user.Id
        Address = userDto.Mail
        IsDefault = true
        Notify = true
        CreatedOn = now
        UpdatedOn = now
    }
    user, mail

let createUser (userDto : CreateUser) = async {
    if userStorage.ContainsKey userDto.Login then
        return -1  // Would become None
    else
        let newUser, newMail = mkUserFromDto userDto
        userStorage.AddOrUpdate(userDto.Login, newUser, fun _ _ -> newUser) |> ignore
        mailStorage.AddOrUpdate(userDto.Login, newMail, fun _ _ -> newMail) |> ignore
        return newUser.Id
}

let upsertUser username (updatedUser : Shared.UpdateUser) = async {
    let newUser =
        match updatedUser.NewPassword with
        | None ->
            // TODO: Pass in the "now" value we want to use, so that testing can be more determinate
            let now = DateTime.UtcNow
            { updatedUser.User with UpdatedOn = Some now }
        | Some password ->
            let now = DateTime.UtcNow
            { updatedUser.User with
                UpdatedOn = Some now
                HashedPassword = BCrypt.HashPassword password }
    if newUser.Login = username then
        // Not changing username
        userStorage.AddOrUpdate(username, newUser, fun _ _ -> newUser) |> ignore
    else
        // Username being changed; SQL version doesn't need to adjust keys, but we do
        userStorage.Remove username |> ignore
        userStorage.AddOrUpdate(username, newUser, fun _ _ -> newUser) |> ignore
    return newUser.Id
}

let changePassword username (changePasswordDto : ChangePassword) = async {
    match userStorage.TryGetValue username with
    | false, _ -> return false
    | true, user ->
        if BCrypt.Verify(changePasswordDto.OldPassword, user.HashedPassword) then
            let newUser = { user with HashedPassword = BCrypt.HashPassword changePasswordDto.NewPassword }
            userStorage.AddOrUpdate(username, newUser, fun _ _ -> newUser) |> ignore
            return true
        else
            return false
}

let verifyLoginInfo (loginInfo : LoginInfo) = async {
    match userStorage.TryGetValue loginInfo.username with
    | false, _ -> return false
    | true, user -> return BCrypt.Verify(loginInfo.password, user.HashedPassword)
}

let addOrRemoveMembershipById (isAdd : bool) (userId : int) (projectId : int) (roleId : int) =
        let membershipFilter (membership : Membership) =
            membership.UserId = userId && membership.ProjectId = projectId &&
            (if isAdd then roleStorage.[membership.Id].Id = roleId else true)
        let results = membershipStorage.Values |> Seq.filter membershipFilter |> List.ofSeq
        match results with
        | [] ->
            if isAdd then
                let now = DateTime.UtcNow
                let newId = membershipIdCounter()
                let newMembership : Membership = {
                    Id = newId
                    UserId = userId
                    ProjectId = projectId
                    CreatedOn = Some now
                    MailNotification = false
                }
                let newMemberRoleId = memberRoleIdCounter()
                let newMemberRole : MembershipRole = {
                    Id = newMemberRoleId
                    MembershipId = newId
                    RoleId = roleId
                    InheritedFrom = None
                }
                membershipStorage.AddOrUpdate(newId, newMembership, fun _ _ -> newMembership) |> ignore
                membershipRoleStorage.AddOrUpdate(newMemberRoleId, newMemberRole, fun _ _ -> newMemberRole) |> ignore
            // If removing, no items found = nothing to remove, so success
        | items ->
            if not isAdd then
                items |> List.iter(fun membership ->
                                    membershipStorage.Remove membership.Id |> ignore
                                    let memberRoles = membershipRoleStorage.Values |> Seq.filter (fun r -> r.MembershipId = membership.Id) |> List.ofSeq
                                    memberRoles |> List.iter (fun r -> membershipRoleStorage.Remove r.Id |> ignore))
            // If adding, then existing items mean we're already done

let addOrRemoveMembership (isAdd : bool) (username : string) (projectCode : string) (roleId : int) = async {
    let maybeUser =
        match userStorage.TryGetValue username with
        | true, user -> Some user
        | false, _ -> None
    let maybeProject =
        match projectStorage.TryGetValue projectCode with
        | true, project -> Some project
        | false, _ -> None
    match maybeUser, maybeProject, roleStorage.ContainsKey roleId with
        | None, _, _
        | _, None, _
        | _, _, false ->
            return false
        | Some user, Some project, true ->
            addOrRemoveMembershipById isAdd user.Id project.Id roleId
            return true
}

let archiveProject isPublic projectCode = async {
    let! maybeProject = getProject isPublic projectCode
    match maybeProject with
    | None -> return false
    | Some project ->
        let newProject = { project with Status = ProjectStatus.Archived }
        projectStorage.AddOrUpdate(projectCode, newProject, fun _ _ -> newProject) |> ignore
        return true
}

module MemoryStorageRegistration =
    open Microsoft.Extensions.DependencyInjection

    let registerServices (builder : IServiceCollection) (connString : string) =
        builder
            .AddSingleton<Model.ListUsers>(usersQueryAsync)
            .AddSingleton<Model.ListProjects>(projectsQueryAsync)
            .AddSingleton<Model.CountUsers>(Model.CountUsers (usersCountAsync))
            .AddSingleton<Model.CountProjects>(Model.CountProjects (projectsCountAsync))
            .AddSingleton<Model.CountRealProjects>(Model.CountRealProjects (realProjectsCountAsync))
            .AddSingleton<Model.ListRoles>(listRoles)
            .AddSingleton<Model.UserExists>(Model.UserExists (userExists))
            .AddSingleton<Model.ProjectExists>(Model.ProjectExists (projectExists))
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
            .AddSingleton<Model.VerifyLoginInfo>(verifyLoginInfo)
            .AddSingleton<Model.AddMembership>(Model.AddMembership (addOrRemoveMembership true))
            .AddSingleton<Model.RemoveMembership>(Model.RemoveMembership (addOrRemoveMembership false))
            .AddSingleton<Model.ArchiveProject>(archiveProject)
        |> ignore
