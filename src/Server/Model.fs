module Model

open System
open System.Linq
open FSharp.Data.Sql
open Shared

[<Literal>]
let sampleConnString = "Server=localhost;Database=languagedepot;User=rmunn"

[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__

[<Literal>]
let schemaPath = __SOURCE_DIRECTORY__ + "/languagedepot.schema"

type sql = SqlDataProvider<Common.DatabaseProviderTypes.MYSQL,
                           sampleConnString,
                           ContextSchemaPath = schemaPath,
                           ResolutionPath = resolutionPath,
                           CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL,
                           UseOptionTypes = true>

// TODO: Add "is_archived" boolean to model (default false) so we can implement archiving; update queries that list or count projects to specify "where (isArchived = false)"
type Dto.ProjectDetails with
    static member FromSql (sqlProject : sql.dataContext.``languagedepot.projectsEntity``) = {
        Dto.ProjectDetails.code = sqlProject.Identifier |> Option.defaultWith (fun _ -> sqlProject.Name.ToLowerInvariant().Replace(" ", "_"))
        Dto.ProjectDetails.name = sqlProject.Name
        Dto.ProjectDetails.description = sqlProject.Description |> Option.defaultValue ""
        Dto.ProjectDetails.membership = None
    }
    static member FromSqlWithRoles (sqlProjectAndRoles : (sql.dataContext.``languagedepot.projectsEntity`` * string * int * string) list) =
        match sqlProjectAndRoles |> List.tryHead with
        | None -> None
        | Some (sqlProject, _, _, _) ->
            let memberships = sqlProjectAndRoles |> List.choose (fun (_, username, roleId, roleName) ->
                if String.IsNullOrEmpty username || roleId = 0 || String.IsNullOrEmpty roleName
                then None
                else
                    match RoleType.TryOfString roleName with
                    | Some role -> Some (username, role)
                    | None -> None)
            { Dto.ProjectDetails.FromSql sqlProject with membership = Some memberships } |> Some

type Dto.UserDetails with
    static member FromSql (sqlUserAndEmails : (sql.dataContext.``languagedepot.usersEntity`` * (sbyte * string)) list) =
        // for pair in sqlUserAndEmails do
        //     let user = fst pair
        //     printfn "Found %A: %s (%s %s) with email(s) %A" user user.Login user.Firstname user.Lastname (snd pair)
        let sqlUser = sqlUserAndEmails |> List.head |> fst
        let emails = sqlUserAndEmails |> List.sortBy snd |> List.map (fun (_, (_, address)) -> address)
        {
            Dto.UserDetails.username = sqlUser.Login
            Dto.UserDetails.firstName = sqlUser.Firstname
            Dto.UserDetails.lastName = sqlUser.Lastname
            Dto.UserDetails.emailAddresses = emails
            Dto.UserDetails.language = sqlUser.Language |> Option.defaultValue "en"
        }
    static member FromSqlWithEmails (sqlUserAndEmails : ((string * string * string * string option) * (sbyte * string)) list) =
        // for pair in sqlUserAndEmails do
        //     let (login, firstname, lastname, language) = fst pair
        //     printfn "Found %s (%s %s) with email(s) %A" login firstname lastname (snd pair)
        let (login, firstname, lastname, language) = sqlUserAndEmails |> List.head |> fst
        let emails = sqlUserAndEmails |> List.sortBy snd |> List.map (fun (_, (_, address)) -> address)
        {
            Dto.UserDetails.username = login
            Dto.UserDetails.firstName = firstname
            Dto.UserDetails.lastName = lastname
            Dto.UserDetails.emailAddresses = emails
            Dto.UserDetails.language = language |> Option.defaultValue "en"
        }

type Dto.RoleDetails with
    static member FromSql (sqlRole : sql.dataContext.``languagedepot.rolesEntity``) = {
        Dto.RoleDetails.name = sqlRole.Name
        Dto.RoleDetails.``type`` = RoleType.OfString sqlRole.Name
    }
    static member TypeFromSql (sqlRole : sql.dataContext.``languagedepot.rolesEntity``) = RoleType.OfString sqlRole.Name

type ListUsers = int option -> int option -> Async<Dto.UserDetails list>
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
type IsAdmin = IsAdmin of (string -> Async<bool>)
type SearchUsersExact = SearchUsersExact of (string -> Async<Dto.UserList>)
type SearchUsersLoose = SearchUsersLoose of (string -> Async<Dto.UserList>)
type GetUser = string -> Async<Dto.UserDetails option>
type GetProject = bool -> string -> Async<Dto.ProjectDetails option>
type CreateProject = Api.CreateProject -> Async<int>
type CreateUser = Api.CreateUser -> Async<int>
type UpsertUser = string -> Api.CreateUser -> Async<int>
type ChangePassword = string -> Api.ChangePassword -> Async<bool>
type VerifyLoginCredentials = Api.LoginCredentials -> Async<bool>
type AddMembership = AddMembership of (string -> string -> RoleType -> Async<bool>)
type RemoveMembership = RemoveMembership of (string -> string -> RoleType -> Async<bool>)
type RemoveUserFromAllRolesInProject = RemoveUserFromAllRolesInProject of (string -> string -> Async<bool>)
type ArchiveProject = bool -> string -> Async<bool>

let usersQueryAsync (connString : string) (limit : int option) (offset : int option) =
    async {
        let ctx = sql.GetDataContext connString
        let usersQuery = query {
            for user in ctx.Languagedepot.Users do
            join mail in !! ctx.Languagedepot.EmailAddresses on (user.Id = mail.UserId)
            select (user, ((if mail.IsDefault <> 0y then 1y else 2y), mail.Address))  // Sort default email(s) first, all others second
        }
        let! users =
            match limit, offset with
            | None, None -> usersQuery |> List.executeQueryAsync
            | None, Some offset -> query { for user in usersQuery do skip offset } |> List.executeQueryAsync
            | Some limit, None -> query { for user in usersQuery do take limit } |> List.executeQueryAsync
            | Some limit, Some offset -> query { for user in usersQuery do skip offset; take limit } |> List.executeQueryAsync
        let usersAndEmails = users |> List.groupBy (fun (user, _) -> user.Login) |> List.map (snd >> Dto.UserDetails.FromSql)
        return usersAndEmails
    }

let searchUsersLoose (connString : string) (searchTerm : string) =
    async {
        let ctx = sql.GetDataContext(connString, SelectOperations.DatabaseSide)
        let! users =
            query {
                for user in ctx.Languagedepot.Users do
                join mail in !! ctx.Languagedepot.EmailAddresses on (user.Id = mail.UserId)
                where (user.Login.Contains searchTerm ||
                       user.Firstname.Contains searchTerm ||
                       user.Lastname.Contains searchTerm ||
                       mail.Address.Contains searchTerm)
                select (user, ((if mail.IsDefault <> 0y then 1y else 2y), mail.Address))  // Sort default email(s) first, all others second
            } |> List.executeQueryAsync
        return users |> List.groupBy (fun (user, _) -> user.Login) |> List.map (snd >> Dto.UserDetails.FromSql)
    }

let searchUsersExact (connString : string) (searchTerm : string) =
    async {
        let ctx = sql.GetDataContext connString
        let! users =
            query {
                for user in ctx.Languagedepot.Users do
                join mail in !! ctx.Languagedepot.EmailAddresses on (user.Id = mail.UserId)
                where (user.Login = searchTerm ||
                       user.Firstname = searchTerm ||
                       user.Lastname = searchTerm ||
                       mail.Address = searchTerm)
                select (user, ((if mail.IsDefault <> 0y then 1y else 2y), mail.Address))  // Sort default email(s) first, all others second
            } |> List.executeQueryAsync
        return users |> List.groupBy (fun (user, _) -> user.Login) |> List.map (snd >> Dto.UserDetails.FromSql)
    }

let projectsQueryAsync (connString : string) (isPublic : bool) =
    async {
        let ctx = sql.GetDataContext connString
        let projectsQuery = query {
            for project in ctx.Languagedepot.Projects do
            where (if isPublic then project.IsPublic > 0y else project.IsPublic = 0y)
            where (project.Status = ProjectStatus.Active)
            select (Dto.ProjectDetails.FromSql project)
        }
        return! projectsQuery |> List.executeQueryAsync
    }

let projectsAndRolesQueryAsync (connString : string) (isPublic : bool) =
    async {
        let ctx = sql.GetDataContext connString
        let projectsQuery = query {
            for project in ctx.Languagedepot.Projects do
            where (if isPublic then project.IsPublic > 0y else project.IsPublic = 0y)
            where (project.Status = ProjectStatus.Active)
            join membership in !! ctx.Languagedepot.Members on (project.Id = membership.ProjectId)
            join memberRole in !! ctx.Languagedepot.MemberRoles on (membership.Id = memberRole.MemberId)
            join user in !! ctx.Languagedepot.Users on (membership.UserId = user.Id)
            join role in !! ctx.Languagedepot.Roles on (memberRole.RoleId = role.Id)
            select (project, user.Login, role.Id, role.Name)
        }
        let! projectsAndRoles = projectsQuery |> List.executeQueryAsync
        return projectsAndRoles |> List.groupBy (fun (project, _, _, _) -> project.Identifier) |> List.choose (snd >> Dto.ProjectDetails.FromSqlWithRoles)
    }

let projectsCountAsync (connString : string) () =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Languagedepot.Projects do
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
            for _ in ctx.Languagedepot.Users do
            count
        }
    }

let userExists (connString : string) username =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for user in ctx.Languagedepot.Users do
            select user.Login
            contains username }
    }

let projectExists (connString : string) projectCode =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Languagedepot.Projects do
            where (project.IsPublic > 0y)
            // We do NOT check where (project.Status = ProjectStatus.Active) here because we want to forbid re-using project codes even of inactive projects
            where (project.Identifier.IsSome)
            select project.Identifier.Value
            contains projectCode }
    }

let isAdmin (connString : string) username =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for user in ctx.Languagedepot.Users do
            where (user.Login = username)
            select (user.Admin <> 0y)
            headOrDefault }
    }

let getUser (connString : string) username =
    async {
        let ctx = sql.GetDataContext connString
        let! userAndEmails =
            query {
                for user in ctx.Languagedepot.Users do
                where (user.Login = username)
                join mail in !! ctx.Languagedepot.EmailAddresses on (user.Id = mail.UserId)
                select ((user.Login, user.Firstname, user.Lastname, user.Language), ((if mail.IsDefault <> 0y then 1y else 2y), mail.Address))  // Sort default email(s) first, all others second
            } |> List.executeQueryAsync
        // for pair in userAndEmails do
        //     let (login, firstname, lastname, language) = fst pair
        //     printfn "In getUser: found %s (%s %s) with language %A and email(s) %A" login firstname lastname language (snd pair)
        if userAndEmails |> List.isEmpty
        then return None
        else return userAndEmails |> Dto.UserDetails.FromSqlWithEmails |> Some
    }

let getProject (connString : string) (isPublic : bool) projectCode =
    async {
        let ctx = sql.GetDataContext connString
        return query {
            for project in ctx.Languagedepot.Projects do
            where (if isPublic then project.IsPublic > 0y else project.IsPublic = 0y)
            where (not project.Identifier.IsNone)
            where (project.Identifier.Value = projectCode)
            select (Some (Dto.ProjectDetails.FromSql project))
            exactlyOneOrDefault }
    }

let getProjectWithRoles (connString : string) (isPublic : bool) projectCode =
    async {
        let ctx = sql.GetDataContext connString
        let projectQuery = query {
            for project in ctx.Languagedepot.Projects do
            join membership in !! ctx.Languagedepot.Members on (project.Id = membership.ProjectId)
            join memberRole in !! ctx.Languagedepot.MemberRoles on (membership.Id = memberRole.MemberId)
            join user in !! ctx.Languagedepot.Users on (membership.UserId = user.Id)
            join role in !! ctx.Languagedepot.Roles on (memberRole.RoleId = role.Id)
            where (if isPublic then project.IsPublic > 0y else project.IsPublic = 0y)
            where (project.Identifier.IsSome && project.Identifier.Value = projectCode)
            select (project, user.Login, role.Id, role.Name)
        }
        let! projectAndRoles = projectQuery |> List.executeQueryAsync
        return  Dto.ProjectDetails.FromSqlWithRoles projectAndRoles
    }

let createProject (connString : string) (project : Api.CreateProject) =
    async {
        // TODO: Handle case where project already exists, and reject if it does. Also, API shape needs to become Async<int option> instead of Async<int>
        let ctx = sql.GetDataContext connString
        let sqlProject = ctx.Languagedepot.Projects.Create()
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

        let sqlUser = ctx.Languagedepot.Users.Create()
        sqlUser.Firstname <- user.firstName
        sqlUser.Lastname <- user.lastName
        sqlUser.HashedPassword <- hashedPassword
        sqlUser.Login <- user.username
        do! ctx.SubmitUpdatesAsync()  // This populates sqlUser.Id, which we'll need when we create email address records

        let now = DateTime.UtcNow
        if not (List.isEmpty user.emailAddresses) then
            user.emailAddresses |> List.iter (fun email ->
                let sqlMail = ctx.Languagedepot.EmailAddresses.Create()
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
            for user in ctx.Languagedepot.Users do
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
            for user in ctx.Languagedepot.Users do
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
        for user in ctx.Languagedepot.Users do
        where (user.Login = username)
        join membership in ctx.Languagedepot.Members
            on (user.Id = membership.UserId)
        join project in ctx.Languagedepot.Projects
            on (membership.ProjectId = project.Id)
        where (project.Status = ProjectStatus.Active)
        join memberRole in ctx.Languagedepot.MemberRoles
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
            for role in ctx.Languagedepot.Roles do
                select (Dto.RoleDetails.FromSql role)
        }
        return! roleQuery |> List.executeQueryAsync
    }

let verifyPass (clearPass : string) (salt : string) (hashPass : string) =
    let calculatedHash = PasswordHashing.hashPassword salt clearPass
    calculatedHash = hashPass

let verifyLoginInfo (connString : string) (loginCredentials : Api.LoginCredentials) =
    // During development of the client UI, just accept any credentials. TODO: Natually, restore real code before going to production
    async { return true }
    // async {
    //     let ctx = sql.GetDataContext connString
    //     let! user = query { for user in ctx.Languagedepot.Users do
    //                             where (user.Login = loginCredentials.username)
    //                             select user } |> Seq.tryHeadAsync
    //     match user with
    //     | None -> return false
    //     | Some user -> return verifyPass loginCredentials.password (user.Salt |> Option.defaultValue "") user.HashedPassword
    // }

let addMembershipById (connString : string) (userId : int) (projectId : int) (roleId : int) =
    async {
        let ctx = sql.GetDataContext connString
        let membershipQuery = query {
            for membership in ctx.Languagedepot.Members do
                where (membership.ProjectId = projectId && membership.UserId = userId)
                select membership }
        let withRole = query {
            for membership in membershipQuery do
                join memberRole in !! ctx.Languagedepot.MemberRoles
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
                let memberRole = ctx.Languagedepot.MemberRoles.Create()
                memberRole.MemberId <- membership.Id
                memberRole.RoleId <- roleId
                memberRole.InheritedFrom <- None
                do! ctx.SubmitUpdatesAsync()
            | None ->
                // User is not yet a member under any role
                let membership = ctx.Languagedepot.Members.Create()
                membership.MailNotification <- 0y
                membership.CreatedOn <- Some (System.DateTime.UtcNow)
                membership.ProjectId <- projectId
                membership.UserId <- userId
                do! ctx.SubmitUpdatesAsync()  // Populate membership.Id
                let memberRole = ctx.Languagedepot.MemberRoles.Create()
                memberRole.MemberId <- membership.Id
                memberRole.RoleId <- roleId
                memberRole.InheritedFrom <- None
                do! ctx.SubmitUpdatesAsync()
    }

let removeMembershipImpl (connString : string) (membershipQuery : IQueryable<sql.dataContext.``languagedepot.membersEntity``>) =
    async {
        let ctx = sql.GetDataContext connString
        // If the Redmine table had proper foreign key relationships, we could use ON DELETE CASCADE and this would be a lot simpler.
        // As it is, we have to go through a slightly convoluted process to delete the correct member_roles and members entries.
        let! memberRolesToDelete =
            query {
                for membership in membershipQuery do
                    join memberRole in !! ctx.Languagedepot.MemberRoles
                        on (membership.Id = memberRole.MemberId)
                    where (memberRole.MemberId = membership.Id)
                    select (memberRole)
            } |> List.executeQueryAsync
        let! membershipsToDelete = membershipQuery |> List.executeQueryAsync
        let memberRoleIdsToDelete = memberRolesToDelete |> List.map (fun mr -> mr.Id)
        let membershipIdsToDelete = membershipsToDelete |> List.map (fun mr -> mr.Id)
        let! _deleteCountMemberRoles =
            query {
                for memberRole in ctx.Languagedepot.MemberRoles do
                    where (memberRole.Id |=| memberRoleIdsToDelete)  // The custom |=| operator translates to "item IN (list-of-items)" in SQL
                    select (memberRole)
            } |> Seq.``delete all items from single table``
        let! _deleteCountMemberships =
            query {
                for membership in ctx.Languagedepot.Members do
                    where (membership.Id |=| membershipIdsToDelete)
                    select (membership)
            } |> Seq.``delete all items from single table``
        // Currently the delete counts are always 0 due to https://github.com/fsprojects/SQLProvider/issues/633 so there's no point in using them to confirm success or failure
        // printfn "%d role entries and %d membership entries deleted" deleteCountMemberRoles deleteCountMemberships
        return ()
    }

let removeMembershipById (connString : string) (userId : int) (projectId : int) (roleId : int) =
    async {
        let ctx = sql.GetDataContext connString
        let membershipQuery = query {
            for membership in ctx.Languagedepot.Members do
                where (membership.ProjectId = projectId && membership.UserId = userId)
                select membership }
        do! removeMembershipImpl connString membershipQuery
    }

let removeUserFromAllRolesInProject (connString : string) (username : string) (projectCode : string) =
    async {
        let ctx = sql.GetDataContext connString
        let membershipQuery = query {
            for membership in ctx.Languagedepot.Members do
            join project in ctx.Languagedepot.Projects on (membership.ProjectId = project.Id)
            join user in ctx.Languagedepot.Users on (membership.UserId = user.Id)
            where (project.Identifier.IsSome && project.Identifier.Value = projectCode)
            where (user.Login = username)
            select membership }
        do! removeMembershipImpl connString membershipQuery
        return true
    }

let addOrRemoveMembership (connString : string) (isAdd : bool) (username : string) (projectCode : string) (roleType : RoleType) =
    // Most of the code checking usernames and project codes and fetching numeric IDs is shared between add and remove operations
    async {
        let ctx = sql.GetDataContext connString
        let roleId = roleType.ToNumericId()
        let maybeUserId = query {
            for user in ctx.Languagedepot.Users do
                where (user.Login = username)
                select (Some user.Id)
                exactlyOneOrDefault }
        let maybeProjectId = query {
            for project in ctx.Languagedepot.Projects do
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
            for project in ctx.Languagedepot.Projects do
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
            .AddSingleton<ListProjects>(projectsAndRolesQueryAsync connString)
            .AddSingleton<CountUsers>(CountUsers (usersCountAsync connString))
            .AddSingleton<CountProjects>(CountProjects (projectsCountAsync connString))
            .AddSingleton<CountRealProjects>(CountRealProjects (realProjectsCountAsync connString))
            .AddSingleton<ListRoles>(roleNames connString)
            .AddSingleton<UserExists>(UserExists (userExists connString))
            .AddSingleton<ProjectExists>(ProjectExists (projectExists connString))
            .AddSingleton<IsAdmin>(IsAdmin (isAdmin connString))
            .AddSingleton<SearchUsersExact>(SearchUsersExact (searchUsersExact connString))
            .AddSingleton<SearchUsersLoose>(SearchUsersLoose (searchUsersLoose connString))
            .AddSingleton<GetUser>(getUser connString)
            .AddSingleton<GetProject>(getProjectWithRoles connString)
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
            .AddSingleton<RemoveUserFromAllRolesInProject>(RemoveUserFromAllRolesInProject (removeUserFromAllRolesInProject connString))
            .AddSingleton<ArchiveProject>(archiveProject connString)
        |> ignore
        FSharp.Data.Sql.Common.QueryEvents.SqlQueryEvent |> Event.add (printfn "Executing SQL: %O")
