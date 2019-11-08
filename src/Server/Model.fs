module Model

open System
open BCrypt.Net
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
type Shared.Project with
    static member FromSql (sqlProject : sql.dataContext.``testldapi.projectsEntity``) = {
        Id = sqlProject.Id
        Name = sqlProject.Name
        Description = sqlProject.Description
        Homepage = sqlProject.Homepage
        IsPublic = sqlProject.IsPublic <> 0y
        ParentId = sqlProject.ParentId
        CreatedOn = sqlProject.CreatedOn
        UpdatedOn = sqlProject.UpdatedOn
        Identifier = sqlProject.Identifier
        Status = sqlProject.Status
        Lft = None
        Rgt = None
        InheritMembers = false
        DefaultVersionId = None
        DefaultAssignedToId = None
    }

type Shared.ProjectForListing with
    static member FromSql ((id,identifier,createdOn,name,description) : int * Option<string> * Option<DateTime> * string * Option<string>) = {
        Id = id
        Name = name
        CreatedOn = createdOn
        Identifier = identifier
        Typ = GuessProjectType.guessType identifier name description
    }

type Shared.User with
    static member FromSql (sqlUser : sql.dataContext.``testldapi.usersEntity``) = {
        Id = sqlUser.Id
        Login = sqlUser.Login
        HashedPassword = sqlUser.HashedPassword
        FirstName = sqlUser.Firstname
        LastName = sqlUser.Lastname
        Admin = sqlUser.Admin <> 0y
        Status = sqlUser.Status
        LastLoginOn = sqlUser.LastLoginOn
        Language = sqlUser.Language
        AuthSourceId = sqlUser.AuthSourceId
        CreatedOn = sqlUser.CreatedOn
        UpdatedOn = sqlUser.UpdatedOn
        Type = sqlUser.Type
        IdentityUrl = sqlUser.IdentityUrl
        MailNotification = sqlUser.MailNotification
        Salt = sqlUser.Salt
        MustChangePasswd = sqlUser.MustChangePasswd <> 0y
        PasswdChangedOn = sqlUser.PasswdChangedOn
    }

type Shared.MailAddress with
    static member FromSql (sqlMail : sql.dataContext.``testldapi.email_addressesEntity``) = {
        Id = sqlMail.Id
        UserId = sqlMail.UserId
        Address = sqlMail.Address
        IsDefault = sqlMail.IsDefault <> 0y
        Notify = sqlMail.Notify <> 0y
        CreatedOn = sqlMail.CreatedOn
        UpdatedOn = sqlMail.UpdatedOn
    }

type Shared.Role with
    static member FromSql (sqlRole : sql.dataContext.``testldapi.rolesEntity``) = {
        Id = sqlRole.Id
        Name = sqlRole.Name
        Position = sqlRole.Position
        Assignable = (sqlRole.Assignable |> Option.defaultValue 0y) <> 0y
        Builtin = sqlRole.Builtin
        Permissions = sqlRole.Permissions
        IssuesVisibility = sqlRole.IssuesVisibility
        UsersVisibility = sqlRole.UsersVisibility
        TimeEntriesVisibility = sqlRole.TimeEntriesVisibility
        AllRolesManaged = sqlRole.AllRolesManaged <> 0y
        Settings = sqlRole.Settings
    }

type Shared.Membership with
    static member FromSql (sqlMember : sql.dataContext.``testldapi.membersEntity``) = {
        Id = sqlMember.Id
        UserId = sqlMember.UserId
        ProjectId = sqlMember.ProjectId
        CreatedOn = sqlMember.CreatedOn
        MailNotification = sqlMember.MailNotification <> 0y
    }

type Shared.MembershipRole with
    static member FromSql (sqlMemberRole : sql.dataContext.``testldapi.member_rolesEntity``) = {
        Id = sqlMemberRole.Id
        MembershipId = sqlMemberRole.MemberId
        RoleId = sqlMemberRole.RoleId
        InheritedFrom = sqlMemberRole.InheritedFrom
    }

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
type VerifyLoginInfo = Api.LoginCredentials -> Async<bool>
type AddMembership = AddMembership of (string -> string -> RoleType -> Async<bool>)
type RemoveMembership = RemoveMembership of (string -> string -> RoleType -> Async<bool>)
type ArchiveProject = bool -> string -> Async<bool>

let usersQueryAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        let usersQuery = query {
            for user in ctx.Testldapi.Users do
                select (User.FromSql user)
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
                select (project.Id, project.Identifier, project.CreatedOn, project.Name, project.Description)
        }
        let! projects = projectsQuery |> List.executeQueryAsync
        return projects |> List.map ProjectForListing.FromSql
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
        let ctx = sql.GetDataContext connString
        let projectsQuery = query {
            for project in ctx.Testldapi.Projects do
                where (project.IsPublic > 0y)
                where (project.Status = ProjectStatus.Active)
                select (project.Id, project.Identifier, project.CreatedOn, project.Name, project.Description)
            }
        let! projects = projectsQuery |> Seq.executeQueryAsync
        return
            projects
            |> Seq.map ProjectForListing.FromSql
            |> Seq.filter (fun project -> project.Typ <> Test && not ((defaultArg project.Identifier "").StartsWith "test"))
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
                select (Some (User.FromSql user))
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
                select (Some (ProjectDetails.FromSql project))
                exactlyOneOrDefault }
    }

let createProject (connString : string) (project : Api.CreateProject) =
    async {
        // TODO: Handle case where project already exists, and reject if it does. Also, API shape needs to become Async<int option> instead of Async<int>
        let ctx = sql.GetDataContext connString
        let sqlProject = ctx.Testldapi.Projects.Create()
        // sqlProject.Id <- project.Id // int
        sqlProject.Name <- project.Name // string
        sqlProject.Description <- project.Description // string option // Long
        // sqlProject.Homepage <- project.Homepage // string option // Long
        // sqlProject.IsPublic <- if project.IsPublic then 1y else 0y
        // sqlProject.ParentId <- project.ParentId // int option
        // sqlProject.CreatedOn <- project.CreatedOn // System.DateTime option
        // sqlProject.UpdatedOn <- project.UpdatedOn // System.DateTime option
        sqlProject.Identifier <- project.Identifier // string option // 20 chars
        sqlProject.Status <- ProjectStatus.Active
        do! ctx.SubmitUpdatesAsync()
        return sqlProject.Id
    }

let createUserImpl (connString : string) (user : Shared.CreateUser) =
    async {
        let ctx = sql.GetDataContext connString
        let hashedPassword = BCrypt.HashPassword user.CleartextPassword  // TODO: Password hashing doesn't belong in the model
        let sqlUser = ctx.Testldapi.Users.Create()
        sqlUser.Firstname <- user.FirstName
        sqlUser.Lastname <- user.LastName
        sqlUser.HashedPassword <- hashedPassword
        sqlUser.Login <- user.Login

        do! ctx.SubmitUpdatesAsync()

        if not (String.IsNullOrEmpty user.Mail) then
            let sqlMail = ctx.Testldapi.EmailAddresses.Create()
            let now = DateTime.UtcNow
            sqlMail.UserId <- sqlUser.Id
            sqlMail.Address <- user.Mail
            sqlMail.IsDefault <- 1y
            sqlMail.Notify <- 0y
            sqlMail.CreatedOn <- now
            sqlMail.UpdatedOn <- now
            do! ctx.SubmitUpdatesAsync()
        return sqlUser.Id
    }

let createUser (connString : string) (user : Shared.CreateUser) = createUserImpl connString user

let upsertUser (connString : string) (login : string) (updatedUser : Shared.UpdateUser) =
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
            let createUser = {
                Login = updatedUser.User.Login
                CleartextPassword = ""  // TODO: Figure out how to deal with passwords in user-upsert model
                FirstName = updatedUser.User.FirstName
                LastName = updatedUser.User.LastName
                Mail = ""  // TODO: Revamp user model to include email again. Deal with having multiple email addresses, one of which will be default
            }
            return! createUserImpl connString createUser  // TODO: Figure out what the data model for users in the API will be; surely it won't *exactly* match Redmine. Then write createUserImpl to take that data model, and use it here. (See above comment, too.)
        | Some sqlUser ->
            // let sqlUser = ctx.Testldapi.Users.Create()  // TODO: Write this
            sqlUser.Firstname <- updatedUser.User.FirstName
            sqlUser.Lastname <- updatedUser.User.LastName
            match maybeUser, updatedUser.NewPassword with
            | None, None -> sqlUser.HashedPassword <- ""  // New user and no password specified: blank password so they can't log in yet
            | Some user, None -> ()  // Existing user: not updating the password
            | _, Some password -> sqlUser.HashedPassword <- BCrypt.HashPassword password
            sqlUser.Login <- updatedUser.User.Login
            // TODO: Deal with email addresses changing -- API doesn't allow it now, but it should
            sqlUser.Admin <- if updatedUser.User.Admin then 1y else 0y
            sqlUser.Status <- updatedUser.User.Status
            sqlUser.LastLoginOn <- updatedUser.User.LastLoginOn
            sqlUser.Language <- updatedUser.User.Language
            sqlUser.AuthSourceId <- updatedUser.User.AuthSourceId
            sqlUser.CreatedOn <- updatedUser.User.CreatedOn
            sqlUser.UpdatedOn <- updatedUser.User.UpdatedOn
            sqlUser.Type <- updatedUser.User.Type
            sqlUser.IdentityUrl <- updatedUser.User.IdentityUrl
            sqlUser.MailNotification <- updatedUser.User.MailNotification
            sqlUser.Salt <- updatedUser.User.Salt
            sqlUser.MustChangePasswd <- if updatedUser.User.MustChangePasswd then 1y else 0y
            sqlUser.PasswdChangedOn <- updatedUser.User.PasswdChangedOn
            do! ctx.SubmitUpdatesAsync()
            return sqlUser.Id
    }

let changePassword (connString : string) (login : string) (changeRequest : Shared.ChangePassword) =
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
            let verified = BCrypt.Verify(changeRequest.OldPassword, sqlUser.HashedPassword)
            if verified then
                sqlUser.HashedPassword <- BCrypt.HashPassword changeRequest.NewPassword
                do! ctx.SubmitUpdatesAsync()
                return true
            else
                return false
    }

let projectsByUserRole (connString : string) username (roleId : RoleType) =
    async {
        let ctx = sql.GetDataContext connString
        let requestedUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some user)
                exactlyOneOrDefault }
        match requestedUser with
        | None -> return []
        | Some requestedUser ->
            let projectsQuery =
                query {
                    for project in ctx.Testldapi.Projects do
                        join membership in ctx.Testldapi.Members
                            on (project.Id = membership.ProjectId)
                        where (project.Status = ProjectStatus.Active)
                        where (membership.UserId = requestedUser.Id)
                        select (project, membership)
                    }
            let resultQuery =
                if roleId < 0 then  // TODO: Now we can make roleId be an option
                    query { for project, _ in projectsQuery do select (ProjectDetails.FromSql project) }
                else
                    query {
                        for project, membership in projectsQuery do
                            join memberRole in ctx.Testldapi.MemberRoles
                                on (membership.Id = memberRole.MemberId)
                            where (memberRole.RoleId = roleId)
                            select (ProjectDetails.FromSql project)
                    }
            return! resultQuery |> List.executeQueryAsync
    }

let projectsByUser username connString = projectsByUserRole connString username -1

// This should produce something like:
// SELECT identifier,users.login,roles.name FROM
//   projects
//     RIGHT JOIN members ON members.project_id = projects.id
//     RIGHT JOIN member_roles ON members.id = member_id
//     LEFT JOIN users ON user_id = users.id
//     LEFT JOIN roles ON role_id = roles.id ;

let projectsAndRolesByUserRole (connString : string) username (roleId : RoleType) =
    async {
        let ctx = sql.GetDataContext connString
        let requestedUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some user)
                exactlyOneOrDefault }
        match requestedUser with
        | None -> return []
        | Some requestedUser ->
            let projectsQuery = query {
                for project in ctx.Testldapi.Projects do
                    join membership in ctx.Testldapi.Members
                        on (project.Id = membership.ProjectId)
                    join memberRole in ctx.Testldapi.MemberRoles
                        on (membership.Id = memberRole.MemberId)
                    join role in ctx.Testldapi.Roles on (memberRole.RoleId = role.Id)
                    where (project.Status = ProjectStatus.Active)
                    where (membership.UserId = requestedUser.Id)
                    select (project, role)
                }
            let resultQuery =
                if roleId < 0 then  // TODO: Now we can make roleId be an option
                    query { for project, role in projectsQuery do select (Project.FromSql project, Role.FromSql role) }
                else
                    query {
                        for project, role in projectsQuery do
                            where (role.Id = roleId)
                            select (Project.FromSql project, Role.FromSql role)
                    }
            return! resultQuery |> List.executeQueryAsync
    }

let projectsAndRolesByUser (connString : string) username =
    projectsAndRolesByUserRole connString username -1

let roleNames (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        let roleQuery = query {
            for role in ctx.Testldapi.Roles do
                select (Role.FromSql role)
        }
        return! roleQuery |> List.executeQueryAsync
    }

let hexStrToBytes (hexStr : string) =
    let len = hexStr.Length
    if len % 2 <> 0 then
        raise (ArgumentException("hexStr", "Hex-encoded byte strings must have an even length"))
    let result = Array.zeroCreate (len / 2)
    for i in 0..2..len - 1 do
        result.[i/2] <- System.Convert.ToByte(hexStr.[i..i+1], 16)
    result

// TODO: Add salt to this function. Redmine v4 uses SHA1(salt + SHA1(password)), where + is string concatenation.
// If there is no salt, then the password is only SHA1'ed once, not twice.
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

let verifyLoginInfo (connString : string) (loginInfo : Shared.LoginInfo) =
    async {
        let ctx = sql.GetDataContext connString
        let! user = query { for user in ctx.Testldapi.Users do
                                where (user.Login = loginInfo.username)
                                select user } |> Seq.tryHeadAsync
        match user with
        | None -> return false
        | Some user -> return verifyPass loginInfo.password user.HashedPassword
    }


let addOrRemoveMembershipById (connString : string) (isAdd : bool) (userId : int) (projectId : int) (roleId : int) =
    async {
        let ctx = sql.GetDataContext connString
        let membershipQuery = query {
            for membership in ctx.Testldapi.Members do
                where (membership.ProjectId = projectId && membership.UserId = userId)
                select membership }
        if isAdd then
            let withRole = query {
                for membership in membershipQuery do
                    join memberRole in ctx.Testldapi.MemberRoles
                        on (membership.Id = memberRole.MemberId)
                    where (memberRole.RoleId = roleId)
                    select (Some memberRole)
                    headOrDefault
            }
            match withRole with
            | None -> // add
                // First, was there a membership record?
                // If not, we need to add it *and* the corresponding role
                let! membershipResults = membershipQuery |> List.executeQueryAsync
                match List.tryHead membershipResults with
                | Some sqlMembership ->
                    // Only need to add a new role to this member
                    let sqlMemberRole = ctx.Testldapi.MemberRoles.Create()
                    sqlMemberRole.MemberId <- sqlMembership.Id
                    sqlMemberRole.RoleId <- roleId
                    sqlMemberRole.InheritedFrom <- None
                | None ->
                    // Add a new membership *and* a new role
                    let sqlMembership = ctx.Testldapi.Members.Create()
                    sqlMembership.MailNotification <- 0y
                    sqlMembership.CreatedOn <- Some (System.DateTime.UtcNow)
                    sqlMembership.ProjectId <- projectId
                    sqlMembership.UserId <- userId
                    let sqlMemberRole = ctx.Testldapi.MemberRoles.Create()
                    sqlMemberRole.MemberId <- sqlMembership.Id
                    sqlMemberRole.RoleId <- roleId
                    sqlMemberRole.InheritedFrom <- None
                do! ctx.SubmitUpdatesAsync()
            | Some sqlMembership ->
                // Already exists; nothing to do
                ()
        else
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

let addOrRemoveMembership (connString : string) (isAdd : bool) (username : string) (projectCode : string) (roleId : RoleType) =
    async {
        let ctx = sql.GetDataContext connString
        let maybeUser = query {
            for user in ctx.Testldapi.Users do
                where (user.Login = username)
                select (Some user)
                exactlyOneOrDefault }
        let maybeProject = query {
            for project in ctx.Testldapi.Projects do
                where (project.Status = ProjectStatus.Active)
                where (project.Identifier.IsSome)
                where (project.Identifier.Value = projectCode)
                select (Some project)
                exactlyOneOrDefault }
        let validRole = query {
            for role in ctx.Testldapi.Roles do
                select role.Id
                contains roleId }
        match maybeUser, maybeProject, validRole with
        | None, _, _
        | _, None, _
        | _, _, false ->
            return false
        | Some sqlUser, Some sqlProject, true ->
            do! addOrRemoveMembershipById connString isAdd sqlUser.Id sqlProject.Id roleId
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
            .AddSingleton<ProjectsAndRolesByUser>(projectsAndRolesByUser connString)
            .AddSingleton<ProjectsAndRolesByUserRole>(projectsAndRolesByUserRole connString)
            .AddSingleton<VerifyLoginInfo>(verifyLoginInfo connString)
            .AddSingleton<AddMembership>(AddMembership (addOrRemoveMembership connString true))
            .AddSingleton<RemoveMembership>(RemoveMembership (addOrRemoveMembership connString false))
            .AddSingleton<ArchiveProject>(archiveProject connString)
        |> ignore
