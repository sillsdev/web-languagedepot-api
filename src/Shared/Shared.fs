namespace Shared

type JsonSuccess<'a> = {
    ok : bool
    data : 'a
}

type JsonError = {
    ok : bool
    message : string
}

type SharedUser = {
    Name : string
    Email : string
}

type SharedProjects = {
    Projects : string list
}


type ProjectType =
    | Unknown
    | Lift
    | Flex
    | OurWord
    | OneStory
    | Test
    | AdaptIt
    | School

type RoleType =
    | Manager
    | Contributor
    | Observer
    | Programmer
    with
        override this.ToString() =
            match this with
            | Manager -> "Manager"
            | Contributor -> "Contributor"
            | Observer -> "Observer"  // TODO: Switch back to the "do not use" text if it turns out we still want to disallow the Observer role
            | Programmer -> "Programmer"
        member this.ToNumericId() =  // Map to the role IDs used in Redmine database, hardcoded to save a DB lookup
            match this with
            | Manager -> 3
            | Contributor -> 4
            | Observer -> 5
            | Programmer -> 6
        static member TryOfString (s : string) =
           match s.ToLowerInvariant() with
            | "manager" -> Some Manager
            | "contributer" -> Some Contributor  // Misspelling of contributor, but this is how it's spelled in Redmine so we have to deal with it
            | "contributor" -> Some Contributor
            | "member" -> Some Contributor
            | "languagedepotprogrammer" -> Some Programmer
            | "programmer" -> Some Programmer
            | "obv - do not use" -> Some Observer
            | "observer" -> Some Observer
            | "non member" -> Some Observer
            | "anonymous" -> Some Observer
            | _ -> None
        static member OfString s =
            match RoleType.TryOfString s with
            | Some value -> value
            | None -> failwith (sprintf "Unknown role type %s" s)
        static member OfNumericId n =
            match n with
            | 1 -> Contributor  // Just so we don't fail on data that uses these... we never actually need them
            | 2 -> Contributor  // Just so we don't fail on data that uses these... we never actually need them
            | 3 -> Manager
            | 4 -> Contributor
            | 5 -> Observer
            | 6 -> Programmer
            | _ -> failwith (sprintf "Unknown role ID %d" n)

// TODO: Decide how to convert between role names in Redmine and the role types (Manager, etc) that our future model will use.
// Right now we just hardcode strings since they're not likely to change anytime soon.

module Dto =
    type UserDetails = {
        username : string
        firstName : string
        lastName : string
        email : string option
        // (two queries: select from emails where is_default = true, then select from emails where is_default = false. Then (default :: rest)).
        language : string // (interface language for this user) - is this useful to the frontend? ... yeah, because when you log in, the front end wants to know who logged in and what language to give you
    }

    type UserList = UserDetails[]

    // type MemberList = (string * RoleType) list

    type ProjectDetailsInternal = {
        code : string
        name : string
        description : string
        // ``type`` : ProjectType  // TODO: Decide if we want this one or not
        membership : System.Collections.Generic.IDictionary<string,string>  // Keys will be usernames and values will be role names
    }

    type ProjectDetails = {
        code : string
        name : string
        description : string
        // ``type`` : ProjectType  // TODO: Decide if we want this one or not
        membership : System.Collections.Generic.IDictionary<string,string>  // Keys will be usernames and values will be role names. Only supplied if we asked for it in the request API, otherwise it's empty.
    }

    type ProjectList = ProjectDetails[]  // Depending on the situation, this will sometimes include MemberLists and sometimes it won't (e.g., list all projects vs. list one's I'm a member of)

    type RoleDetails = {
        id : int
        name : string
    }

module Api =
    type LoginCredentials = {
        username : string
        password : string
    }

    type CreateProject = {
        login : LoginCredentials // TODO : Use "username" rather than "login" for login field in Redmine's users table
        code : string
        name : string
        description : string option
        initialMembers : System.Collections.Generic.Dictionary<string,string>
    }

    type ArchiveProject = {
        login : LoginCredentials
        code : string
    }

    type DeleteProject = {
        login : LoginCredentials
        code : string
    }

    type MembershipRecordApiCall = {
        username : string
        role : string  // One of four values allowed : "manager", "contributor", "observer", "programmer" -- will be converted to RoleType
    }

    type MembershipRecordInternal = {
        username : string
        role : RoleType
    }

    type EditProjectMembershipApiCall = {
        login : LoginCredentials // (the login credentials of an existing admin account, not the one being demoted)
        // projectCode : string  // Not needed; the URL provides this
        add : MembershipRecordApiCall list option
        remove : MembershipRecordApiCall list option
        removeUser : string option
    }

    type EditProjectMembershipInternalDetails =
        | AddUserRoles of MembershipRecordInternal list
        | RemoveUserRoles of MembershipRecordInternal list
        | RemoveUserEntirely of string

    type EditProjectMembershipInternal = {
        login : LoginCredentials // (the login credentials of an existing admin account, not the one being demoted)
        projectCode : string
        details : EditProjectMembershipInternalDetails
    }

    type ChangeUserActiveStatus = {
        login : LoginCredentials // (the login credentials of an existing admin account, or the one being changed)
        username : string
        active : bool  // Suspended = false, active = true
    }

    type DeleteUser = {
        login : LoginCredentials // (the login credentials of an existing admin account, or the one being deleted)
        username : string
    }

    type CreateUser = {
        login : LoginCredentials // (the login credentials of the admin creating the account)
        username : string // (required)
        password : string // (required, cleartext - will be hashed by the server)
        mustChangePassword : bool // (admin can choose for this to be false, but box is checked by default)
        firstName : string
        lastName : string
        language : string option // (will default to "en" if not provided)
        emailAddresses : string option
    }

    type EditUser = {
        login : LoginCredentials // (the login credentials of the person editing the account : must either be same user as the account we're editing, or must be admin)
        username : string // (required, cannot be changed in this API endpoint. A separate API endpoint exists for changing username)
        // NOT password : string option (We'll use a separate API endpoint to change the password.)
        firstName : string option
        lastName : string option
        language : string option // (will default to no change if not provided, just like every other string option in this model)
        emailAddresses : string list option  // (if not provided, no change. If provided, replace current list with new one, even if new list is empty)
    }

    type ChangePassword = {
        login : LoginCredentials // (the login credentials of the person changing the password : must either be same user as the account we're editing, or must be admin)
        username : string
        password : string
        mustChangePassword : bool option // (option so it can be omitted : should be omitted/None if user is changing own password. If admin changing someone else's password, required.)
    }

    type ChangeUsername = {
        login : LoginCredentials // (the login credentials of the person changing the username : must either be same user as the account we're editing, or must be admin)
        oldUsername : string // (required even if we're logging in as that user, for API consistency)
        newUsername : string
    }

    type PromoteUserToAdmin = {
        login : LoginCredentials // (the login credentials of an existing admin account, not the one being promoted)
        username : string
    }

    type DemoteAdminToNormalUser = {
        login : LoginCredentials // (the login credentials of an existing admin account, not the one being demoted)
        username : string
    }

    type ShowProjectsUserBelongsTo = {
        login : LoginCredentials // (the login credentials of an admin account, or the user account whose username is specified below)
        username : string
        role : string option  // (if unspecified, return all projects user is a member of, else show projects where username holds this role)
    }

    type SearchProjects = {
        login : LoginCredentials // (the login credentials of the user doing the search)
        searchText : string option  // Search by either name or project code, but not(?) description
        // If searchText is omitted, will return all projects ONLY if login account is admin. Otherwise will return all public projects.
        offset : int option // If specified, used for paging
        limit : int option  // If specified, used for paging
    }

    type SearchUsers = {
        login : LoginCredentials // (the login credentials of the user doing the search)
        emailSearch : string option // If specified, must be exact match (though case-insensitive (in invariant culture) so not "exact" by that standard)
        nameSearch : string option // If specified, will do "LIKE '%str%'", so beware SQL injection here.
        // If both email and name are specified, will do an AND. If an OR is desired, submit two API calls, one with emailSearch and one with nameSearch, and frontend can join them together while removing overlapping usernames
        // If neither email nor name is specified, will return all users ONLY if login account is admin. Otherwise will return HTTP "Not Authorized" error.
        offset : int option // If specified, used for paging
        limit : int option  // If specified, used for paging
    }


// TODO: These two can probably be moved to Model.fs now
module ProjectStatus =
    // Values copied from Redmine
    let [<Literal>] Active = 1
    let [<Literal>] Closed = 5
    let [<Literal>] Archived = 9

module UserStatus =
    // Values copied from Redmine
    let [<Literal>] Anonymous = 0
    let [<Literal>] Active = 1
    let [<Literal>] Registered = 2
    let [<Literal>] Locked = 3

// TODO: Figure out an API for handling repositories
