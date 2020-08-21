module Model

open System
open System.Linq
open System.Threading.Tasks
open System.Collections.Generic
open Shared
open MySqlConnector
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Configuration

// TODO: Add "is_archived" boolean to model (default false) so we can implement archiving; update queries that list or count projects to specify "where (isArchived = false)"

let getSqlResult (convertRow : MySqlDataReader -> 'result) (reader : MySqlDataReader) = task {
    if not reader.HasRows then
        return Array.empty
    else
        let result = new ResizeArray<'result>()
        let mutable hasMore = true
        let! readResult = reader.ReadAsync()
        hasMore <- readResult
        while hasMore do
            let item = convertRow reader
            result.Add item
            let! readResult = reader.ReadAsync()
            hasMore <- readResult
        return result.ToArray()
}

let fetchDataWithParams (connString : string) (sql : string) (setParams : MySqlCommand -> unit) (convertRow : MySqlDataReader -> 'result) = task {
    use conn = new MySqlConnection(connString)
    do! conn.OpenAsync()
    use cmd = new MySqlCommand(sql, conn)
    setParams cmd
    use! reader = cmd.ExecuteReaderAsync()
    let! result = reader :?> MySqlDataReader |> getSqlResult convertRow
    return result
}

let fetchData (connString : string) (sql : string) (convertRow : MySqlDataReader -> 'result) =
    fetchDataWithParams connString sql ignore convertRow

let fetchDataWithLimitOffset (connString : string) (sql : string) (limit : int option) (offset : int option) (convertRow : MySqlDataReader -> 'result) =
    let withLimit = match limit with | None -> "" | Some n -> sprintf " LIMIT %d" n
    let withOffset = match offset with | None -> "" | Some n -> sprintf " OFFSET %d" n
    fetchDataWithParams connString (sql + withLimit + withOffset) ignore convertRow

let doScalarQueryWithParams<'result> (connString : string) (sql : string) (setParams : MySqlCommand -> unit) = task {
    use conn = new MySqlConnection(connString)
    do! conn.OpenAsync()
    use cmd = new MySqlCommand(sql, conn)
    setParams cmd
    let! boxedResult = cmd.ExecuteScalarAsync()
    return (unbox<'result> boxedResult)
}

let doScalarQuery<'result> connString sql =
    doScalarQueryWithParams<'result> connString sql ignore

let doCountQueryWithParams connString sql setParams = doScalarQueryWithParams<int64> connString sql setParams

let doCountQuery connString sql = doCountQueryWithParams connString sql ignore

let doNonQueryWithParams (connString : string) (sql : string) (setParams : MySqlCommand -> unit) = task {
    use conn = new MySqlConnection(connString)
    do! conn.OpenAsync()
    use cmd = new MySqlCommand(sql, conn)
    setParams cmd
    let! result = cmd.ExecuteNonQueryAsync()
    try
        let count = unbox<int> result
        return count
    with _ ->
        return 0
}

let doNonQuery (connString : string) (sql : string) =
    doNonQueryWithParams connString sql ignore

let convertUserRow (reader : MySqlDataReader) =
    {
        Dto.UserDetails.username = reader.GetString(0)
        Dto.UserDetails.firstName = reader.GetString(1)
        Dto.UserDetails.lastName = reader.GetString(2)
        Dto.UserDetails.language = if reader.IsDBNull(3) then "en" else reader.GetString(3)
        Dto.UserDetails.email = if reader.IsDBNull(4) then None else Some (reader.GetString(4))
    }

let baseUsersQuery = "SELECT login, firstname, lastname, language, address FROM users LEFT JOIN email_addresses ON users.id = email_addresses.user_id"

type IModel =
    abstract ListUsers : int option -> int option -> Task<Dto.UserDetails []>
    abstract ListProjects : int option -> int option -> Task<Dto.ProjectList>
    abstract ListRoles : int option -> int option -> Task<(int * string)[]>
    abstract ListProjectsAndRoles : int option -> int option -> Task<Dto.ProjectDetails []>

    abstract SearchUsersExact : string -> Task<Dto.UserList>
    abstract SearchUsersLoose : string -> Task<Dto.UserList>

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

    abstract UpsertUser : string -> Api.CreateUser -> Task<int>
    abstract ChangePassword : string -> Api.ChangePassword -> Task<bool>
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
    abstract VerifyLoginInfo : Api.LoginCredentials -> Task<bool>
    abstract ArchiveProject : string -> Task<bool>


type MySqlModel(config : IConfiguration, isPublic : bool) =
    let settings = SettingsHelper.getSettingsValue<Settings.MySqlSettings> config
    let connString = if isPublic then settings.ConnString else settings.ConnStringPrivate

    member this.decode (s : string) =
        match Thoth.Json.Net.Decode.Auto.fromString s with
        | Ok m -> m
        | Error e -> failwithf "Decoding error: %s" e  // TODO: Handle this more gracefully, returning the error all the way to the client

    member this.convertProjectRow (reader : MySqlDataReader) = {
            Dto.ProjectDetails.code = reader.GetString("identifier")
            Dto.ProjectDetails.name = reader.GetString("name")
            Dto.ProjectDetails.description = reader.GetString("description")
            Dto.ProjectDetails.membership = reader.GetString("user_roles") |> this.decode  // TODO: Handle case where we don't want the complete membership list
        }

    member this.baseProjectQuery = "SELECT identifier, name, description, \"{}\" AS user_roles FROM projects"

    // TODO: Either we set a global MySQL setting, or we run the following before our projects query. Choosing the global setting for now since it's simpler.
    // let projectsPreQuery = "SET SESSION sql_mode=(SELECT REPLACE(@@sql_mode,'ONLY_FULL_GROUP_BY',''))"

    member this.projectWithMembersBaseQuery =
        "SELECT projects.identifier, projects.name, projects.description, json_objectagg(users.login, roles.name) AS user_roles" +
        " FROM projects" +
        " LEFT JOIN members ON members.project_id = projects.id" +
        " JOIN member_roles ON member_roles.member_id = members.id" +
        " JOIN roles ON roles.id = member_roles.role_id" +
        " JOIN users on members.user_id = users.id"
    // MySQL wants WHERE clause before GROUP BY clause, so we add GROUP BY as a separate step
    member this.projectsWithMembersGroupByClause = " GROUP BY projects.identifier"

    member this.createUserImpl (user : Api.CreateUser) (sql : string) =
        // TODO: Just make everything here a task{} instead of async{}; it'll be simpler
        task {
            // TODO: Password creation belongs in the controller, not the model. This requires a new CreateUserInternal data type which will carry the hashed password and the salt
            let salt = PasswordHashing.createSalt (Guid.NewGuid())
            let hashedPassword = PasswordHashing.hashPassword salt user.password
            use conn = new MySqlConnection(connString)
            do! conn.OpenAsync()
            use transaction = conn.BeginTransaction()
            use cmd = new MySqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("login", user.username) |> ignore
            cmd.Parameters.AddWithValue("firstname", user.firstName) |> ignore
            cmd.Parameters.AddWithValue("lastname", user.lastName) |> ignore
            cmd.Parameters.AddWithValue("hashedPassword", hashedPassword) |> ignore
            cmd.Parameters.AddWithValue("salt", salt) |> ignore
            cmd.Parameters.AddWithValue("status", UserStatus.Active) |> ignore
            let! result = cmd.ExecuteNonQueryAsync()
            if result < 1 then
                do! transaction.RollbackAsync()
                return (if result = 0 then -1 else result)
            else
                let didUpsert = result > 1
                // Inserted user, so insert email addresses as well
                let newUserId = int cmd.LastInsertedId
                match user.emailAddresses with
                | None -> return newUserId
                | Some email ->
                    let sql = "INSERT INTO email_addresses (user_id, address, is_default, created_on, updated_on) " +
                              "VALUES (@userId, @email, 1, NOW(), NOW())"
                    if didUpsert then ()  // TODO: Nope. INSERT ON DUPLICATE KEY UPDATE isn't going to work here because Redmine doesn't have a UNIQUE constraint on email addresses...!
                    use cmd = new MySqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("user_id", newUserId) |> ignore
                    cmd.Parameters.AddWithValue("address", email) |> ignore
                    let! result = cmd.ExecuteNonQueryAsync()
                    if result < 1 then
                        do! transaction.RollbackAsync()
                        return (if result = 0 then -1 else result)
                    else
                        do! transaction.CommitAsync()
                        return newUserId
        }

    member this.projectsAndRolesQueryAsync () =
        task {
            let sql = this.projectWithMembersBaseQuery + this.projectsWithMembersGroupByClause
            let! result = fetchData connString sql this.convertProjectRow
            return result
        }

    member this.projectsAndRolesByUserImpl username = task {
        let sql =
            "SELECT DISTINCT projects.identifier AS identifier, roles.name AS role" +
            " FROM members" +
            " JOIN projects ON members.project_id = projects.id" +
            " JOIN member_roles ON member_roles.member_id = members.id" +
            " JOIN roles ON roles.id = member_roles.role_id" +
            " JOIN users on members.user_id = users.id" +
            " WHERE users.login = @username"
        let setParams (cmd : MySqlCommand) =
                cmd.Parameters.AddWithValue("username", username) |> ignore
        let convertRow (reader : MySqlDataReader) =
            reader.GetString("identifier"), reader.GetString("role")
        let! result = fetchDataWithParams connString sql setParams convertRow
        return result
    }
    // TODO: Figure out how to "pass through" the result from json_objectagg

    member this.getProjectDetails (projectCodes : string[]) = task {
        // Unfortunately we can't do " WHERE projects.identifier IN @projectCodes" as MySqlConnector doesn't support string arrays as parameters
        // So we have to get clever, using each projectCode as its own variable name AND its own value, and constructing a WHERE clause that looks
        // like "WHERE projects.identifier = @projectCode1 OR projects.identifier = @projectCode2" with each variable having a value that matches its name
        // Also, the hyphen character isn't allowed in an identifier in MySQL
        if projectCodes |> Array.isEmpty
        then return Array.empty
        else
            let whereClause = projectCodes |> Seq.mapi (fun idx _ -> sprintf "projects.identifier = @var%d" idx) |> String.concat " OR "
            let safeWhereClause = if String.IsNullOrEmpty whereClause then "" else " WHERE " + whereClause
            let sql = this.projectWithMembersBaseQuery + safeWhereClause + this.projectsWithMembersGroupByClause
            let setParams (cmd : MySqlCommand) =
                for idx, code in projectCodes |> Seq.indexed do
                    cmd.Parameters.AddWithValue(sprintf "var%d" idx, code) |> ignore
            let! result = fetchDataWithParams connString sql setParams this.convertProjectRow
            return result
    }

    // TODO: This doesn't belong in MySqlModel
    member this.verifyPass (clearPass : string) (salt : string) (hashPass : string) =
        let calculatedHash = PasswordHashing.hashPassword salt clearPass
        calculatedHash = hashPass

    interface IModel with

        member this.ListUsers (limit : int option) (offset : int option) =
            task {
                let sql = baseUsersQuery
                let! result = fetchDataWithLimitOffset connString sql limit offset convertUserRow
                return result
            }

        member this.GetUser username =
            task {
                let sql = baseUsersQuery + " WHERE login = @username"
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("username", username) |> ignore
                let! result = fetchDataWithParams connString sql setParams convertUserRow
                return result |> Array.tryHead
            }

        member this.SearchUsersLoose (searchTerm : string) =
            task {
                let sql = baseUsersQuery + " WHERE login LIKE @searchTerm OR firstname LIKE @searchTerm OR lastname LIKE @searchTerm OR address LIKE @searchTerm"
                let escapedSearchTerm = "%" + searchTerm.Replace(@"\", @"\\").Replace("%", @"\%") + "%"
                // TODO: Test whether that works, or whether we need to write something like "LIKE %@searchTerm%" instead
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("searchTerm", escapedSearchTerm) |> ignore
                let! result = fetchDataWithParams connString sql setParams convertUserRow
                return result
            }

        member this.SearchUsersExact (searchTerm : string) =
            task {
                let sql = baseUsersQuery + " WHERE login = @searchTerm OR firstname = @searchTerm OR lastname = @searchTerm OR address = @searchTerm"
                let! result = fetchDataWithParams connString sql (fun cmd -> cmd.Parameters.AddWithValue("searchTerm", searchTerm) |> ignore) convertUserRow
                return result
            }

        member this.ListProjects (limit : int option) (offset : int option) =
            task {
                let sql = this.baseProjectQuery
                let! result = fetchDataWithLimitOffset connString sql limit offset this.convertProjectRow
                return result
            }

        member this.ListProjectsAndRoles (limit : int option) (offset : int option) =
            task {
                let sql = this.projectWithMembersBaseQuery + this.projectsWithMembersGroupByClause
                let! result = fetchDataWithLimitOffset connString sql limit offset this.convertProjectRow
                return result
            }

        member this.CountProjects() =
            task {
                let sql = "SELECT COUNT(*) FROM projects"
                return! doCountQuery connString sql
            }

        member this.CountRealProjects() =
            task {
                let sql = this.baseProjectQuery
                let! result = fetchData connString sql this.convertProjectRow
                return
                    result
                    |> Seq.map (fun project -> project, GuessProjectType.guessType project.code project.name project.description)
                    |> Seq.filter (fun (project, projectType) -> projectType <> Test && not (project.code.StartsWith "test"))
                    |> Seq.length
                    |> int64
            }

        member this.CountUsers() =
            task {
                let sql = "SELECT COUNT(*) FROM users"
                return! doCountQuery connString sql
            }

        member this.UserExists username =
            task {
                let sql = "SELECT COUNT(*) FROM users WHERE login = @username"
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("username", username) |> ignore
                let! count = doCountQueryWithParams connString sql setParams
                return (count > 0L)
            }

        member this.ProjectExists projectCode =
            task {
                let sql = "SELECT COUNT(*) FROM projects WHERE identifier = @projectCode"
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("projectCode", projectCode) |> ignore
                let! count = doCountQueryWithParams connString sql setParams
                return (count > 0L)
            }

        member this.IsAdmin username =
            task {
                let sql = "SELECT is_admin FROM users WHERE login = @username"
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("username", username) |> ignore
                let convertRow (reader : MySqlDataReader) = reader.GetBoolean(0)
                let! results = fetchDataWithParams connString sql setParams convertRow
                if results.Length > 0 then return results.[0] else return false
            }

        member this.GetProject projectCode =
            task {
                let sql = this.baseProjectQuery + " WHERE projects.identifier = @projectCode"
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("projectCode", projectCode) |> ignore
                let! result = fetchDataWithParams connString sql setParams this.convertProjectRow
                return result |> Array.tryHead
            }

        member this.GetProjectWithRoles projectCode =
            task {
                let whereClause = " WHERE projects.identifier = @projectCode"
                let sql = this.projectWithMembersBaseQuery + whereClause + this.projectsWithMembersGroupByClause
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("projectCode", projectCode) |> ignore
                let! result = fetchDataWithParams connString sql setParams this.convertProjectRow
                if result.Length = 0 then return None else return Some result.[0]
            }

        member this.CreateProject (project : Api.CreateProject) =
            task {
                let sqlTxt = "INSERT INTO projects (name, description, identifier, status, created_on, updated_on) VALUES (@name, @description, @identifier, @status, NOW(), NOW())"
                use conn = new MySqlConnection(connString)
                do! conn.OpenAsync()
                use cmd = new MySqlCommand(sqlTxt, conn)
                cmd.Parameters.AddWithValue("name", project.name) |> ignore
                cmd.Parameters.AddWithValue("description", project.description) |> ignore
                cmd.Parameters.AddWithValue("identifier", project.code) |> ignore
                cmd.Parameters.AddWithValue("status", ProjectStatus.Active) |> ignore
                let! result = cmd.ExecuteNonQueryAsync()
                if result = 0 then return -1
                elif result < 0 then return result
                else
                    let newId = int cmd.LastInsertedId
                    return newId
            }

        member this.CreateUser (user : Api.CreateUser) =
            let sql = "INSERT INTO users (login, firstname, lastname, hashed_password, salt, status, created_on, updated_on) " +
                      "VALUES (@login, @firstname, @lastname, @hashedPassword, @salt, @status, NOW(), NOW())"
            this.createUserImpl user sql

        member this.UpdateUser (login : string) (updatedUser : Api.CreateUser) =
            task {
                // Everyone may change their own data, but only admins may change some else's data
                let sql = "SELECT is_admin, login FROM users WHERE login = @login"
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("login", updatedUser.login.username) |> ignore
                    // TODO: Once we move "is this allowed?" logic into controller, verify password as well
                let! result = fetchDataWithParams connString sql setParams (fun row -> row.GetBoolean("is_admin"), row.GetString("login"))
                if result.Length = 0 then
                    // TODO: Edit to return a Result<unit, errorDU> so we can indicate why this may fail (e.g., "Invalid login" or whatever). In this case, login user not found
                    // That errorDU should live in ErrorCodes.fs
                    return ()
                let isAdmin, loggedInUser = result.[0]
                // TODO: Move "is this allowed?" logic into the controller, not here
                // TODO: Once we move "is this allowed?" logic into controller, verify password as well
                let allowed = isAdmin || loggedInUser = updatedUser.username
                if not allowed then
                    // Return error indicating 403 forbidden
                    return 0
                else
                    let! salt =
                        let sql = "SELECT salt FROM users where login = @username"
                        let setParams (cmd : MySqlCommand) =
                            cmd.Parameters.AddWithValue("username", updatedUser.username) |> ignore
                        doScalarQueryWithParams connString sql setParams
                    let isPasswordChange = not (String.IsNullOrEmpty updatedUser.password)
                    let hashedPassword = if isPasswordChange then PasswordHashing.hashPassword salt updatedUser.password else ""
                    let sql =
                        if isPasswordChange then
                            "UPDATE users SET login = @username, hashed_password = @hashedPassword, must_change_passwd = @mustChangePassword, firstname = @firstName, lastname = @lastName, language = @language" +
                            " WHERE login = @loggedInUser"
                        else
                            "UPDATE users SET login = @username, must_change_passwd = @mustChangePassword, firstname = @firstName, lastname = @lastName, language = @language" +
                            " WHERE login = @loggedInUser"
                    let setParams (cmd : MySqlCommand) =
                        cmd.Parameters.AddWithValue("login", updatedUser.username) |> ignore
                        if isPasswordChange then
                            cmd.Parameters.AddWithValue("hashedPassword", hashedPassword) |> ignore
                        cmd.Parameters.AddWithValue("mustChangePassword", updatedUser.mustChangePassword) |> ignore
                        cmd.Parameters.AddWithValue("firstName", updatedUser.firstName) |> ignore
                        cmd.Parameters.AddWithValue("lastName", updatedUser.lastName) |> ignore
                        cmd.Parameters.AddWithValue("language", updatedUser.language |> Option.defaultValue "en") |> ignore
                        cmd.Parameters.AddWithValue("loggedInUser", loggedInUser) |> ignore
                    let! changedRows = doNonQueryWithParams connString sql setParams
                    // TODO: Detect changeRows being 0 and return an error code
                    return changedRows
            }

        member this.UpsertUser login updatedUser =
            task {
                let! shouldUpdate = (this :> IModel).UserExists updatedUser.username
                if not shouldUpdate then
                    return! (this :> IModel).CreateUser updatedUser
                else
                    return! (this :> IModel).UpdateUser login updatedUser
            }
            // This won't work, because the Redmine data model doesn't have "login" as a unique key constraint (?!?)
            // let sql = "INSERT INTO users (login, firstname, lastname, hashed_password, salt, status, created_on, updated_on) " +
            //           "VALUES (@login, @firstname, @lastname, @hashedPassword, @salt, @status, NOW(), NOW()) " +
            //           "ON DUPLICATE KEY UPDATE firstname = @firstname, lastname = @lastname, hashedPassword = @hashedPassword, salt = @salt, status = @status, updated_on = NOW()"
            // createUserImpl connString updatedUser sql

        member this.ProjectsAndRolesByUser username = task {
            let! projectsAndRoles = this.projectsAndRolesByUserImpl username
            let projectCodes, roles = projectsAndRoles |> Array.unzip
            let! projects = this.getProjectDetails projectCodes
            return Array.zip projects roles
        }

        member this.LegacyProjectsAndRolesByUser username = task {
            let! projectsAndRoles = this.projectsAndRolesByUserImpl username
            let projectCodes, roles = projectsAndRoles |> Array.unzip
            let! projects = this.getProjectDetails projectCodes
            let legacyProjects = (projects, roles) ||> Array.zip |> Array.map (fun (proj,role) ->
                let result : Dto.LegacyProjectDetails = {
                    identifier = proj.code
                    name = proj.name
                    repository = "http://public.languagedepot.org"
                    role = role.ToLowerInvariant()
                }
                result
            )
            return legacyProjects
        }

        member this.ProjectsAndRolesByUserRole username (roleName : string) = task {
            let! projectsAndRoles = this.projectsAndRolesByUserImpl username
            let projectCodes, roles = projectsAndRoles |> Array.filter (fun (proj, role) -> role = roleName) |> Array.unzip
            let! projects = this.getProjectDetails projectCodes
            return Array.zip projects roles
        }

        member this.ProjectsByUserRole username (roleName : string) = task {
            let! projectsAndRoles = this.projectsAndRolesByUserImpl username
            let projectCodes, _ = projectsAndRoles |> Array.filter (fun (proj, role) -> role = roleName) |> Array.unzip
            return! this.getProjectDetails projectCodes
        }

        member this.ProjectsByUser username = task {
            let! projectsAndRoles = this.projectsAndRolesByUserImpl username
            let projectCodes, _ = projectsAndRoles |> Array.unzip
            return! this.getProjectDetails projectCodes
        }

        member this.ListRoles (limit : int option) (offset : int option) =
            task {
                let sql = "SELECT id, name FROM roles"
                let convertRow (reader : MySqlDataReader) =
                    reader.GetInt32("id"), reader.GetString("name")
                return! fetchDataWithLimitOffset connString sql limit offset convertRow
            }

        member this.VerifyLoginInfo (loginCredentials : Api.LoginCredentials) =
            // During development of the client UI, just accept any credentials. TODO: Natually, restore real code before going to production
            task { return true }
            // task {
            //     let sql = "SELECT salt, hashed_password FROM users where login = @username"
            //     let setParams (cmd : MySqlCommand) =
            //         cmd.Parameters.AddWithValue("username", loginCredentials.username) |> ignore
            //     let convertRow (reader : MySqlDataReader) =
            //         reader.GetString("salt"), reader.GetString("hashed_password")
            //     let rows = fetchDataWithParams connString sql setParams convertRow
            //     if rows.Length > 0 then
            //         let salt, hashedPassword = rows.[0]
            //         return verifyPass loginCredentials.password salt hashedPassword
            //     else
            //         return false
            // }

        member this.ChangePassword (login : string) (changeRequest : Api.ChangePassword) =
            task {
                let! salt =
                    let sql = "SELECT salt FROM users where login = @username"
                    let setParams (cmd : MySqlCommand) =
                        cmd.Parameters.AddWithValue("username", changeRequest.username) |> ignore
                    doScalarQueryWithParams connString sql setParams
                let hashedPassword = PasswordHashing.hashPassword salt changeRequest.password
                let sql = "UPDATE users SET hashed_password = @hashedPassword, must_change_passwd = @mustChangePassword, updated_on = NOW() WHERE login = @username"
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("hashedPassword", hashedPassword) |> ignore
                    cmd.Parameters.AddWithValue("mustChangePassword", changeRequest.mustChangePassword |> Option.defaultValue false) |> ignore
                let! result = doNonQueryWithParams connString sql setParams
                return (result = 1)
            }

        member this.RemoveMembership (username : string) (projectCode : string) =
            task {
                use conn = new MySqlConnection(connString)
                do! conn.OpenAsync()
                use transaction = conn.BeginTransaction()
                let sql =
                    "SELECT members.id FROM members" +
                    " JOIN users ON users.id = members.user_id" +
                    " JOIN projects ON projects.id = members.project_id" +
                    " WHERE users.login = @username AND projects.identifier = @projectCode"
                use cmd = new MySqlCommand(sql, conn, transaction)
                cmd.Parameters.AddWithValue("username", username) |> ignore
                cmd.Parameters.AddWithValue("projectCode", projectCode) |> ignore
                let! reader = cmd.ExecuteReaderAsync()  // NOT use! because we want to explicitly release the reader later
                let! memberRowIds = reader :?> MySqlDataReader |> getSqlResult (fun reader -> reader.GetInt32(0))
                do! reader.DisposeAsync()  // Releases the connection so we can reuse it in the DELETE statements below
                // Also have to delete from member_roles table
                let whereClause = memberRowIds |> Seq.mapi (fun idx _ -> sprintf "member_id = @var%d" idx) |> String.concat " OR "
                let safeWhereClause = if String.IsNullOrEmpty whereClause then "" else " WHERE " + whereClause
                let sql = "DELETE FROM member_roles" + safeWhereClause
                use cmd = new MySqlCommand(sql, conn, transaction)
                for idx, code in memberRowIds |> Seq.indexed do
                    cmd.Parameters.AddWithValue(sprintf "var%d" idx, code) |> ignore
                let! _ = cmd.ExecuteNonQueryAsync()
                let whereClause = memberRowIds |> Seq.mapi (fun idx _ -> sprintf "id = @var%d" idx) |> String.concat " OR "
                let safeWhereClause = if String.IsNullOrEmpty whereClause then "" else " WHERE " + whereClause
                let sql = "DELETE FROM members" + safeWhereClause
                use cmd = new MySqlCommand(sql, conn, transaction)
                for idx, code in memberRowIds |> Seq.indexed do
                    cmd.Parameters.AddWithValue(sprintf "var%d" idx, code) |> ignore
                let! _ = cmd.ExecuteNonQueryAsync()
                do! transaction.CommitAsync()
                return true
            }

        member this.AddMembership (username : string) (projectCode : string) (roleName : string) =

            let getId (sql : string) (paramName : string) (paramValue : string) (conn : MySqlConnection) (transaction: MySqlTransaction) = task {
                    use cmd = new MySqlCommand(sql, conn, transaction)
                    cmd.Parameters.AddWithValue(paramName, paramValue) |> ignore
                    use! reader = cmd.ExecuteReaderAsync()
                    let! ids = reader :?> MySqlDataReader |> getSqlResult (fun reader -> reader.GetInt32(0))
                    if ids.Length = 0 then
                        return -1
                    else
                        return ids.[0]
            }

            let escapeForLikeClause (s : string) =
                s.Replace(@"\", @"\\").Replace("%",@"\%").Replace("_",@"\_")

            task {
                use conn = new MySqlConnection(connString)
                do! conn.OpenAsync()
                use transaction = conn.BeginTransaction()

                // Use a LIKE clause for roles because that's case-insensitive in MySQL: https://dev.mysql.com/doc/refman/5.7/en/string-comparison-functions.html#operator_like
                let! roleId = getId "SELECT id FROM roles WHERE name LIKE @roleName" "roleName" (escapeForLikeClause roleName) conn transaction
                let! userId = getId "SELECT id FROM users WHERE login = @username" "username" username conn transaction
                let! projId = getId "SELECT id FROM projects WHERE identifier = @projectCode" "projectCode" projectCode conn transaction
                if roleId < 0 || userId < 0 || projId < 0 then
                    // Can't do anything with invalid data
                    do! transaction.RollbackAsync()
                    return false
                else
                    // First, check whether user is already a member
                    let sql =
                        "SELECT members.id FROM members" +
                        " JOIN users ON users.id = members.user_id" +
                        " JOIN projects ON projects.id = members.project_id" +
                        " WHERE users.login = @username AND projects.identifier = @projectCode"
                    use cmd = new MySqlCommand(sql, conn, transaction)
                    cmd.Parameters.AddWithValue("username", username) |> ignore
                    cmd.Parameters.AddWithValue("projectCode", projectCode) |> ignore
                    let! reader = cmd.ExecuteReaderAsync()  // NOT use! because we want to explicitly release the reader later
                    let! memberRowIds = reader :?> MySqlDataReader |> getSqlResult (fun reader -> reader.GetInt32(0))
                    do! reader.DisposeAsync()  // Releases the connection so we can reuse it in the DELETE statements below
                    if memberRowIds.Length > 0 then
                        let memberId = memberRowIds.[0]
                        // Already a member; just update the membership role that user already has
                        let sql = "UPDATE member_roles SET role_id = @roleId WHERE member_id = @memberId"
                        use cmd = new MySqlCommand(sql, conn, transaction)
                        cmd.Parameters.AddWithValue("roleId", roleId) |> ignore
                        cmd.Parameters.AddWithValue("memberId", memberId) |> ignore
                        let! _ = cmd.ExecuteNonQueryAsync()
                        do! transaction.CommitAsync()
                        return true
                    else
                        // Add new members *and* member_roles entries
                        let sql =
                            "INSERT INTO members (user_id, project_id, created_on) " +
                            "VALUES (@userId, @projectId, NOW())"
                        use cmd = new MySqlCommand(sql, conn, transaction)
                        cmd.Parameters.AddWithValue("userId", userId) |> ignore
                        cmd.Parameters.AddWithValue("projectId", projId) |> ignore
                        let! affectedRows = cmd.ExecuteNonQueryAsync()
                        if affectedRows <= 0 then
                            do! transaction.RollbackAsync()
                            return false
                        else
                            let memberId = cmd.LastInsertedId
                            let sql =
                                "INSERT INTO member_roles (member_id, role_id) " +
                                "VALUES (@memberId, @roleId)"
                            use cmd = new MySqlCommand(sql, conn, transaction)
                            cmd.Parameters.AddWithValue("memberId", memberId) |> ignore
                            cmd.Parameters.AddWithValue("roleId", roleId) |> ignore
                            let! affectedRows = cmd.ExecuteNonQueryAsync()
                            if affectedRows <= 0 then
                                do! transaction.RollbackAsync()
                                return false
                            else
                                do! transaction.CommitAsync()
                                return true
            }

        member this.ArchiveProject (projectCode : string) =
            task {
                use conn = new MySqlConnection(connString)
                do! conn.OpenAsync()
                use transaction = conn.BeginTransaction()

                let sql = sprintf "UPDATE projects SET status = @status WHERE identifier = @projectCode"
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("status", ProjectStatus.Archived) |> ignore
                    cmd.Parameters.AddWithValue("projectCode", projectCode) |> ignore
                let! affectedRows = doNonQueryWithParams connString sql setParams
                if affectedRows > 0 then
                    do! transaction.CommitAsync()
                    return true
                else
                    do! transaction.RollbackAsync()
                    return false
            }

        member this.EmailIsAdmin email =
            task {
                let sql = "SELECT COUNT(u.login) FROM email_addresses AS e JOIN users AS u ON e.user_id = u.id WHERE u.admin=1 AND e.address = @email"
                let setParams (cmd : MySqlCommand) =
                    cmd.Parameters.AddWithValue("email", email) |> ignore
                let! count = doCountQueryWithParams connString sql setParams
                return (count > 0L)
                // Note that this will return false for both a non-admin address and a non-existing address.
                // This is by design, because this is a publicly-accessible API endpoint and we don't want to leak info about real email addresses.
            }

type MySqlPublicModel(config : IConfiguration) =
    inherit MySqlModel(config, true)

type MySqlPrivateModel(config : IConfiguration) =
    inherit MySqlModel(config, false)

module ModelRegistration =
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.DependencyInjection.Extensions

    let registerServices (builder : IServiceCollection) (connString : string) =
        // We need to turn off MySQL's ONLY_FULL_GROUP_BY setting for our entire session
        builder
            .AddScoped<MySqlPublicModel>()
            .AddScoped<MySqlPrivateModel>()
        |> ignore
