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

let storeNewPassword username cleartextPassword =
    passwordStorage.AddOrUpdate(username,
        (fun _ -> let salt = createSalt (Guid.NewGuid()) in { salt = salt; hashedPassword = hashPassword salt cleartextPassword }),
        (fun _ oldPassword -> { oldPassword with hashedPassword = hashPassword oldPassword.salt cleartextPassword })) |> ignore

let blankUserRecord : Dto.UserDetails = {
    username = ""
    firstName = ""
    lastName = ""
    emailAddresses = []
    language = "" }

let blankProjectRecord : Dto.ProjectDetails = {
    code = ""
    name = ""
    description = ""
    membership = None }

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

// TODO: Move this list to the unit test setup
let roles = Dto.standardRoles

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
    let listReplace old ``new`` lst = lst |> List.map (fun x -> if x = old then ``new`` else x)
    let mkNewMembers (oldMembers : Dto.MemberList) : Dto.MemberList = {
        managers = oldMembers.managers |> listReplace oldUsername newUsername
        contributors = oldMembers.contributors |> listReplace oldUsername newUsername
        observers = oldMembers.observers |> listReplace oldUsername newUsername
        programmers = oldMembers.programmers |> listReplace oldUsername newUsername
    }
    let mkNewProject _projectCode (oldProject : Dto.ProjectDetails) =
        match oldProject.membership with
        | None -> oldProject
        | Some members ->
            { oldProject with membership = Some (mkNewMembers members) }
    for item in projectStorage do
        let projectCode, project = item.Key, item.Value
        match project.membership with
        | None -> ()
        | Some members ->
            if members.managers |> List.contains oldUsername ||
               members.contributors |> List.contains oldUsername ||
               members.observers |> List.contains oldUsername ||
               members.programmers |> List.contains oldUsername then
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
