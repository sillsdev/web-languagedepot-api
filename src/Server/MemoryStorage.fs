module MemoryStorage

open System
open System.Collections.Concurrent
open System.Collections.Generic
open Shared
open PasswordHashing

// REVAMP. This is going to produce DTO results rather than mimicking the MySQL database.
// So it'll be a bit like a mock server.

type PasswordDetails = {
    salt: string
    hashedPassword: string
}

let userStorage = new ConcurrentDictionary<string, Dto.UserDetails>()
let projectStorage = new ConcurrentDictionary<string, Dto.ProjectDetails>()
let passwordStorage = new ConcurrentDictionary<string, PasswordDetails>()
// MemoryStorage doesn't keep track of the "must change password" bool since that's not in Dto.UserDetails, so some tests might not work. TODO: Add that bool into userStorage

let initFromData (users : Shared.Dto.UserList) (projects : Shared.Dto.ProjectList) =
    for user in users do
        userStorage.GetOrAdd (user.username, (fun _ -> user)) |> ignore
    for project in projects do
        projectStorage.GetOrAdd (project.code, (fun _ -> project)) |> ignore

let storeNewPassword username cleartextPassword =
    passwordStorage.AddOrUpdate(username,
        (fun _ -> let salt = createSalt (Guid.NewGuid()) in { salt = salt; hashedPassword = hashPassword salt cleartextPassword }),
        (fun _ oldPassword -> { oldPassword with hashedPassword = hashPassword oldPassword.salt cleartextPassword })) |> ignore

let blankUserRecord username : Dto.UserDetails = {
    username = username
    firstName = ""
    lastName = ""
    email = None
    language = "" }

let blankProjectRecord projectCode : Dto.ProjectDetails = {
    code = projectCode
    name = ""
    description = ""
    membership = Dictionary<string,string>() }

(* Not needed now that the API design is username/projectCode-centric
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
*)

let roles = SampleData.StandardRoles

type KeyReplacementError =
    | TargetKeyAlreadyExists
    | InvalidTargetKey
    | OriginalKeyNotFound

let isValidUsername (username : string) = // TODO: Move to some kind of validation module
    System.Text.RegularExpressions.Regex(@"^[-_@.a-zA-Z0-9]+$").IsMatch username
    // Alphanumerics plus hyphen, underscore, dot, and @ sign (so emails can double as usernames)
    // TODO: Decide whether to remove @ from valid usernames, but then allow logging in with email addresses

let isValidProjectCode (projectCode : string) = // TODO: Move to some kind of validation module
    // Lowercase alphanumerics plus hyphen or underscore, but NOT nothing but digits
    System.Text.RegularExpressions.Regex(@"^[-_a-z0-9]+$").IsMatch projectCode
    && not (System.Text.RegularExpressions.Regex(@"^\d+$").IsMatch projectCode)

let usernameReplacementLock = obj()
let projectCodeReplacementLock = obj()

let replaceUsername oldUsername newUsername =
    let mutable replacements = []
    let replaceUsernameInList lst = lst |> List.map (fun ((name, role) as pair) -> if name = oldUsername then (newUsername, role) else pair)
    let mkNewMembers (oldMembers : IDictionary<string,string>) : IDictionary<string,string> =
        match oldMembers.TryGetValue oldUsername with
        | false, _ -> oldMembers
        | true, role ->
            let newMembers = Dictionary<string,string>(oldMembers)
            newMembers.Remove oldUsername |> ignore
            newMembers.[newUsername] <- role
            newMembers :> IDictionary<string,string>
    let mkNewProject _projectCode (oldProject : Dto.ProjectDetails) =
        { oldProject with membership = mkNewMembers oldProject.membership }
    for item in projectStorage do
        let projectCode, project = item.Key, item.Value
        if project.membership.ContainsKey oldUsername then
            replacements <- (projectCode, project) :: replacements
    // Check for overlapping username as late as possible; this won't eliminate race conditions, but it will minimize them as much as we can
    if not <| isValidUsername newUsername then
        Error InvalidTargetKey
    elif userStorage.ContainsKey newUsername then
        Error TargetKeyAlreadyExists
    else
        match userStorage.TryGetValue oldUsername with
        | false, _ -> Error OriginalKeyNotFound
        | true, oldUserRecord ->
            lock usernameReplacementLock (fun () ->
                for projectCode, project in replacements do
                    projectStorage.AddOrUpdate(projectCode, project, mkNewProject) |> ignore
                let newUserRecord = { oldUserRecord with username = newUsername }
                userStorage.AddOrUpdate(newUsername, newUserRecord, fun _ _ -> newUserRecord) |> ignore
                userStorage.TryRemove(oldUsername) |> ignore
                match passwordStorage.TryGetValue oldUsername with
                | false, _ -> ()
                | true, passwordRecord ->
                    passwordStorage.AddOrUpdate(newUsername, passwordRecord, fun _ _ -> passwordRecord) |> ignore
                    passwordStorage.TryRemove(oldUsername) |> ignore
                Ok ()
        )

let replaceProjectCode oldCode newCode =
    if not <| isValidProjectCode newCode then
        Error InvalidTargetKey
    elif projectStorage.ContainsKey newCode then
        Error TargetKeyAlreadyExists
    else
        match projectStorage.TryGetValue oldCode with
        | false, _ -> Error OriginalKeyNotFound
        | true, oldProjectRecord ->
            lock projectCodeReplacementLock (fun () ->
                let newProjectRecord = { oldProjectRecord with code = newCode }
                projectStorage.AddOrUpdate(newCode, newProjectRecord, fun _ _ -> newProjectRecord) |> ignore
                projectStorage.TryRemove(oldCode) |> ignore
                Ok ()
        )

let addUser username userDetails =
    userStorage.AddOrUpdate(username, userDetails, fun _ _ -> userDetails)

let addProject projectCode projectDetails =
    userStorage.AddOrUpdate(projectCode, projectDetails, fun _ _ -> projectDetails)

let editUser username (editFn : Dto.UserDetails -> Dto.UserDetails) =
    userStorage.AddOrUpdate(username, blankUserRecord, fun _ -> editFn)

let editProject projectCode (editFn : Dto.ProjectDetails -> Dto.ProjectDetails) =
    projectStorage.AddOrUpdate(projectCode, blankProjectRecord, fun _ -> editFn)

let delUser username =
    userStorage.TryRemove username |> ignore

let delProject projectCode =
    projectStorage.TryRemove projectCode |> ignore
