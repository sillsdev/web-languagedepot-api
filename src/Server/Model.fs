module Model

open System
open FSharp.Data.Sql
open Shared

[<Literal>]
let sampleConnString = "Server=localhost;Database=testldapi;User=rmunn"

[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__

type sql = SqlDataProvider<Common.DatabaseProviderTypes.MYSQL,
                           sampleConnString,
                           ResolutionPath = resolutionPath,
                           CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL,
                           UseOptionTypes = true>

// TODO: Add "is_archived" boolean to model (default false) so we can implement archiving; update queries that list or count projects to specify "where (isArchived = false)"
type Dto.ProjectDetails with
    static member FromSql (sqlProject : sql.dataContext.``testldapi.projectsEntity``) = {
        Dto.ProjectDetails.code = sqlProject.Identifier |> Option.defaultWith (fun _ -> sqlProject.Name.ToLowerInvariant().Replace(" ", "_"))
        Dto.ProjectDetails.name = sqlProject.Name
        Dto.ProjectDetails.description = sqlProject.Description |> Option.defaultValue ""
        Dto.ProjectDetails.membership = None  // TODO: Write function to populate this from a query
    }

type Dto.UserDetails with
    static member FromSql (sqlUser : sql.dataContext.``testldapi.usersEntity``) = {
        Dto.UserDetails.username = sqlUser.Login
        Dto.UserDetails.firstName = sqlUser.Firstname
        Dto.UserDetails.lastName = sqlUser.Lastname
        Dto.UserDetails.emailAddresses = []  // TODO: Populate from query
        Dto.UserDetails.language = sqlUser.Language |> Option.defaultValue "en"
    }

type Dto.RoleDetails with
    static member FromSql (sqlRole : sql.dataContext.``testldapi.rolesEntity``) = {
        Dto.RoleDetails.name = sqlRole.Name
        Dto.RoleDetails.``type`` = RoleType.OfString sqlRole.Name
    }
    static member TypeFromSql (sqlRole : sql.dataContext.``testldapi.rolesEntity``) = RoleType.OfString sqlRole.Name

type ListUsers = unit -> Async<Dto.UserDetails list>
type ListProjects = bool -> Async<Dto.ProjectList>
// These three CountFoo types all look the same, so we have to use a single-case DU to distinguish them
type CountUsers = CountUsers of (unit -> Async<int>)
type CountProjects = CountProjects of (unit -> Async<int>)
type CountRealProjects = CountRealProjects of (unit -> Async<int>)
type ListRoles = unit -> Async<Dto.RoleDetails list>
type ProjectsByUser = string -> Async<Dto.ProjectDetails list>
type ProjectsByUserRole = string -> RoleType -> Async<Dto.ProjectDetails list>
type ProjectsAndRolesByUser = string -> Async<(Dto.ProjectDetails * RoleType list) list>
type ProjectsAndRolesByUserRole = string -> RoleType -> Async<(Dto.ProjectDetails * RoleType list) list>
// Ditto for these two FooExists types: need a DU
type UserExists = UserExists of (string -> Async<bool>)
type ProjectExists = ProjectExists of (string -> Async<bool>)
type GetUser = string -> Async<Dto.UserDetails option>
type GetProject = bool -> string -> Async<Dto.ProjectDetails option>
type CreateProject = Api.CreateProject -> Async<int>
type CreateUser = Api.CreateUser -> Async<int>
type UpsertUser = string -> Api.CreateUser -> Async<int>
type ChangePassword = string -> Api.ChangePassword -> Async<bool>
type VerifyLoginCredentials = Api.LoginCredentials -> Async<bool>
type AddMembership = AddMembership of (string -> string -> RoleType -> Async<bool>)
type RemoveMembership = RemoveMembership of (string -> string -> RoleType -> Async<bool>)
// TODO: Add a RemoveUserEntirelyFromProject that's similar to RemoveMembership but doesn't specify a role
type ArchiveProject = bool -> string -> Async<bool>

let usersQueryAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        let usersQuery = query {
            for user in ctx.Testldapi.Users do
                select (Dto.UserDetails.FromSql user)
        }
        return! usersQuery |> List.executeQueryAsync
    }

let projectsQueryAsync (connString : string) (isPublic : bool) =
    async {
        let ctx = sql.GetDataContext connString
        let projectsQuery = query {
            for project in ctx.Testldapi.Projects do
                where (if isPublic then project.IsPublic > 0y else project.IsPublic = 0y)
                where (project.Status = ProjectStatus.Active)
                select (Dto.ProjectDetails.FromSql project)
        }
        return! projectsQuery |> List.executeQueryAsync
    }

let projectsCountAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Testldapi.Projects do
                where (project.IsPublic > 0y)
                where (project.Status = ProjectStatus.Active)
                count
        }
    }

let realProjectsCountAsync (connString : string) () =
    async {
        let! projects = projectsQueryAsync connString true
        return
            projects
            |> Seq.map (fun project -> project, GuessProjectType.guessType project.code project.name project.description)
            |> Seq.filter (fun (project, projectType) -> projectType <> Test && not (project.code.StartsWith "test"))
            |> Seq.length
    }

let usersCountAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for _ in ctx.Testldapi.Users do
            count
        }
    }

let userExists (connString : string) username =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for user in ctx.Testldapi.Users do
                select user.Login
                contains username }
    }

let projectExists (connString : string) projectCode =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Testldapi.Projects do
                where (project.IsPublic > 0y)
                // We do NOT check where (project.Status = ProjectStatus.Active) here because we want to forbid re-using project codes even of inactive projects
                where (project.Identifier.IsSome)
                select project.Identifier.Value
                contains projectCode }
    }

let getUser (connString : string) username =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some (Dto.UserDetails.FromSql user))
                exactlyOneOrDefault }
    }

let getProject (connString : string) (isPublic : bool) projectCode =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Testldapi.Projects do
                where (if isPublic then project.IsPublic > 0y else project.IsPublic = 0y)
                where (not project.Identifier.IsNone)
                where (project.Identifier.Value = projectCode)
                select (Some (Dto.ProjectDetails.FromSql project))
                exactlyOneOrDefault }
    }

let createProject (connString : string) (project : Api.CreateProject) =
    async {
        // TODO: Handle case where project already exists, and reject if it does. Also, API shape needs to become Async<int option> instead of Async<int>
        let ctx = sql.GetDataContext connString
        let sqlProject = ctx.Testldapi.Projects.Create()
        // sqlProject.Id <- project.Id // int
        sqlProject.Name <- project.name // string
        sqlProject.Description <- project.description // string option // Long
        // sqlProject.Homepage <- project.Homepage // string option // Long
        // sqlProject.IsPublic <- if project.IsPublic then 1y else 0y
        // sqlProject.ParentId <- project.ParentId // int option
        let now = DateTime.UtcNow  // TODO: Pass in the "now" value to use, so unit tests can be more deterministic
        sqlProject.CreatedOn <- Some now
        sqlProject.UpdatedOn <- Some now
        sqlProject.Identifier <- Some project.code // string option // 20 chars
        sqlProject.Status <- ProjectStatus.Active
        do! ctx.SubmitUpdatesAsync()
        return sqlProject.Id
    }

let createUserImpl (connString : string) (user : Api.CreateUser) =
    async {
        let ctx = sql.GetDataContext connString
        let salt = PasswordHashing.createSalt (Guid.NewGuid())
        let hashedPassword = PasswordHashing.hashPassword salt user.password

        let sqlUser = ctx.Testldapi.Users.Create()
        sqlUser.Firstname <- user.firstName
        sqlUser.Lastname <- user.lastName
        sqlUser.HashedPassword <- hashedPassword
        sqlUser.Login <- user.username
        do! ctx.SubmitUpdatesAsync()  // This populates sqlUser.Id, which we'll need when we create email address records

        let now = DateTime.UtcNow
        if not (List.isEmpty user.emailAddresses) then
            user.emailAddresses |> List.iter (fun email ->
                let sqlMail = ctx.Testldapi.EmailAddresses.Create()
                sqlMail.UserId <- sqlUser.Id
                sqlMail.Address <- email
                sqlMail.IsDefault <- 1y
                sqlMail.Notify <- 0y
                sqlMail.CreatedOn <- now
                sqlMail.UpdatedOn <- now
            )
            do! ctx.SubmitUpdatesAsync()
        return sqlUser.Id
    }

let createUser (connString : string) (user : Api.CreateUser) = createUserImpl connString user

let upsertUser (connString : string) (login : string) (updatedUser : Api.CreateUser) =
    async {
        let ctx = sql.GetDataContext connString
        let maybeUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = login)
                select (Some user)
                exactlyOneOrDefault
        }
        match maybeUser with
        | None ->
            return! createUserImpl connString updatedUser
        | Some sqlUser ->
            sqlUser.Login <- updatedUser.username
            sqlUser.Firstname <- updatedUser.firstName
            sqlUser.Lastname <- updatedUser.lastName
            // Password should be changed with changePassword API, so we don't update the password here
            // TODO: Deal with email addresses changing -- API doesn't allow it now, but it should
            sqlUser.Language <- updatedUser.language
            sqlUser.UpdatedOn <- Some DateTime.UtcNow  // TODO: Pass in "now" value to use, so unit tests can be deterministic
            sqlUser.MustChangePasswd <- if updatedUser.mustChangePassword then 1y else 0y
            do! ctx.SubmitUpdatesAsync()
            return sqlUser.Id
    }

let changePassword (connString : string) (login : string) (changeRequest : Api.ChangePassword) =
    async {
        let ctx = sql.GetDataContext connString
        let maybeUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = login)
                select (Some user)
                exactlyOneOrDefault
        }
        match maybeUser with
        | None -> return false
        | Some sqlUser ->
            let allowed = sqlUser.Admin <> 0y || sqlUser.Login = changeRequest.username
            // TODO: This puts business logic (admins can change passwords) into the model, and it really belongs in the controller. Fix this.
            if allowed then
                sqlUser.HashedPassword <- PasswordHashing.hashPassword (sqlUser.Salt |> Option.defaultValue "") changeRequest.password
                do! ctx.SubmitUpdatesAsync()
                return true
            else
                return false
    }

let projectsAndRolesByUser (connString : string) username = async {
    let ctx = sql.GetDataContext connString
    let projectsQuery = query {
        for user in ctx.Testldapi.Users do
        where (user.Login = username)
        join membership in ctx.Testldapi.Members
            on (user.Id = membership.UserId)
        join project in ctx.Testldapi.Projects
            on (membership.ProjectId = project.Id)
        where (project.Status = ProjectStatus.Active)
        join memberRole in ctx.Testldapi.MemberRoles
            on (membership.Id = memberRole.MemberId)

        select (Dto.ProjectDetails.FromSql project, RoleType.OfNumericId memberRole.RoleId)
    }
    let! projectsAndRoles = projectsQuery |> List.executeQueryAsync
    return projectsAndRoles |> List.groupBy fst |> List.map (fun (proj, projAndRoles) -> (proj, List.map snd projAndRoles))
}

let projectsAndRolesByUserRole connString username (roleType : RoleType) = async {
    let! projectsAndRoles = projectsAndRolesByUser connString username
    return projectsAndRoles |> List.filter (fun (proj, roles) -> roles |> List.contains roleType)
}

let projectsByUserRole connString username (roleType : RoleType) = async {
    let! projectsAndRoles = projectsAndRolesByUser connString username
    return projectsAndRoles |> List.filter (fun (proj, roles) -> roles |> List.contains roleType) |> List.map fst
}

let projectsByUser connString username = async {
    let! projectsAndRoles = projectsAndRolesByUser connString username
    return projectsAndRoles |> List.map fst
}

let roleNames (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        let roleQuery = query {
            for role in ctx.Testldapi.Roles do
                select (Dto.RoleDetails.FromSql role)
        }
        return! roleQuery |> List.executeQueryAsync
    }

let verifyPass (clearPass : string) (salt : string) (hashPass : string) =
    let calculatedHash = PasswordHashing.hashPassword salt clearPass
    calculatedHash = hashPass

let verifyLoginInfo (connString : string) (loginCredentials : Api.LoginCredentials) =
    async {
        let ctx = sql.GetDataContext connString
        let! user = query { for user in ctx.Testldapi.Users do
                                where (user.Login = loginCredentials.username)
                                select user } |> Seq.tryHeadAsync
        match user with
        | None -> return false
        | Some user -> return verifyPass loginCredentials.password (user.Salt |> Option.defaultValue "") user.HashedPassword
    }

let addMembershipById (connString : string) (userId : int) (projectId : int) (roleId : int) =
    async {
        let ctx = sql.GetDataContext connString
        let membershipQuery = query {
            for membership in ctx.Testldapi.Members do
                where (membership.ProjectId = projectId && membership.UserId = userId)
                select membership }
        let withRole = query {
            for membership in membershipQuery do
                join memberRole in ctx.Testldapi.MemberRoles
                    on (membership.Id = memberRole.MemberId)
                where (memberRole.RoleId = roleId)
                select (Some memberRole)
                headOrDefault
        }
        match withRole with
        | Some _ -> () // Already exists, no need to change it
        | None -> // add
            // First, was there a membership record?
            // If not, we need to add it *and* the corresponding role
            let! maybeMember = membershipQuery |> Seq.tryHeadAsync
            match maybeMember with
            | Some membership ->
                // User is a member already but with a different role
                let memberRole = ctx.Testldapi.MemberRoles.Create()
                memberRole.MemberId <- membership.Id
                memberRole.RoleId <- roleId
                memberRole.InheritedFrom <- None
                do! ctx.SubmitUpdatesAsync()
            | None ->
                // User is not yet a member under any role
                let membership = ctx.Testldapi.Members.Create()
                membership.MailNotification <- 0y
                membership.CreatedOn <- Some (System.DateTime.UtcNow)
                membership.ProjectId <- projectId
                membership.UserId <- userId
                do! ctx.SubmitUpdatesAsync()  // Populate membership.Id
                let memberRole = ctx.Testldapi.MemberRoles.Create()
                memberRole.MemberId <- membership.Id
                memberRole.RoleId <- roleId
                memberRole.InheritedFrom <- None
                do! ctx.SubmitUpdatesAsync()
    }

let removeMembershipById (connString : string) (userId : int) (projectId : int) (roleId : int) =
    async {
        let ctx = sql.GetDataContext connString
        let membershipQuery = query {
            for membership in ctx.Testldapi.Members do
                where (membership.ProjectId = projectId && membership.UserId = userId)
                select membership }
        let! memberRolesToDelete =
            query {
                for membership in membershipQuery do
                    join memberRole in ctx.Testldapi.MemberRoles
                        on (membership.Id = memberRole.MemberId)
                    where (memberRole.MemberId = membership.Id)
                    select (memberRole)
            } |> List.executeQueryAsync
        let! membershipsToDelete = membershipQuery |> List.executeQueryAsync
        memberRolesToDelete |> List.iter (fun sqlMemberRole -> sqlMemberRole.Delete())
        membershipsToDelete |> List.iter (fun sqlMembership -> sqlMembership.Delete())
        do! ctx.SubmitUpdatesAsync()
    }

let addOrRemoveMembership (connString : string) (isAdd : bool) (username : string) (projectCode : string) (roleType : RoleType) =
    // Most of the code checking usernames and project codes and fetching numeric IDs is shared between add and remove operations
    async {
        let ctx = sql.GetDataContext connString
        let roleId = roleType.ToNumericId()
        let maybeUserId = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some user.Id)
                exactlyOneOrDefault }
        let maybeProjectId = query {
            for project in ctx.Testldapi.Projects do
                where (project.Status = ProjectStatus.Active &&
                       project.Identifier.IsSome &&
                       project.Identifier.Value = projectCode)
                select (Some project.Id)
                exactlyOneOrDefault }
        match maybeUserId, maybeProjectId with
        | None, _
        | _, None ->
            return false
        | Some userId, Some projectId ->
            let fn = if isAdd then addMembershipById else removeMembershipById
            do! fn connString userId projectId roleId
            return true
    }

let archiveProject (connString : string) (isPublic : bool) (projectCode : string) =
    async {
        let ctx = sql.GetDataContext connString
        let maybeProject = query {
            for project in ctx.Testldapi.Projects do
                where (if isPublic then project.IsPublic > 0y else project.IsPublic = 0y)
                where (not project.Identifier.IsNone)
                where (project.Identifier.Value = projectCode)
                select (Some project)
                exactlyOneOrDefault }
        match maybeProject with
        | None -> return false
        | Some project ->
            project.Status <- ProjectStatus.Archived
            do! ctx.SubmitUpdatesAsync()
            return true
    }

module ModelRegistration =
    open Microsoft.Extensions.DependencyInjection

    let registerServices (builder : IServiceCollection) (connString : string) =
        builder
            .AddSingleton<ListUsers>(usersQueryAsync connString)
            .AddSingleton<ListProjects>(projectsQueryAsync connString)
            .AddSingleton<CountUsers>(CountUsers (usersCountAsync connString))
            .AddSingleton<CountProjects>(CountProjects (projectsCountAsync connString))
            .AddSingleton<CountRealProjects>(CountRealProjects (realProjectsCountAsync connString))
            .AddSingleton<ListRoles>(roleNames connString)
            .AddSingleton<UserExists>(UserExists (userExists connString))
            .AddSingleton<ProjectExists>(ProjectExists (projectExists connString))
            .AddSingleton<GetUser>(getUser connString)
            .AddSingleton<GetProject>(getProject connString)
            .AddSingleton<CreateProject>(createProject connString)
            .AddSingleton<CreateUser>(createUser connString)
            .AddSingleton<UpsertUser>(upsertUser connString)
            .AddSingleton<ChangePassword>(changePassword connString)
            .AddSingleton<ProjectsByUser>(projectsByUser connString)
            .AddSingleton<ProjectsByUserRole>(projectsByUserRole connString)
            .AddSingleton<ProjectsAndRolesByUserRole>(projectsAndRolesByUserRole connString)
            .AddSingleton<ProjectsAndRolesByUser>(projectsAndRolesByUser connString)
            .AddSingleton<VerifyLoginCredentials>(verifyLoginInfo connString)
            .AddSingleton<AddMembership>(AddMembership (addOrRemoveMembership connString true))
            .AddSingleton<RemoveMembership>(RemoveMembership (addOrRemoveMembership connString false))
            .AddSingleton<ArchiveProject>(archiveProject connString)
        |> ignore
