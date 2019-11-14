## Design notes for how the front end should look

This will help inform what we do in the Shared.fs model.

### What we want to do with the front end

#### Projects
- Create projects
- Assign users to projects as managers, contributors, etc.
- Have a user page where you can "create a new managed project" so when I'm on Robin's page, whether or not I'm logged in as Robin, I can create a project owned by Robin
    - Assuming, of course, that I'm logged in with an admin user account
    - If I'm logged in as a normal user, I can only see my own page, so I can only create projects from my own page
    - Pretty much any creation API needs to pass login credentials to the server
- Archive or delete projects

#### Users
- Create users
- Edit user data (change name, change username -- yes, username is changeable since we're using numeric IDs)
- Grant LD admin rights to users (must be logged in as another admin to do this)
- Remove admin rights from another user (cannot revoke your own admin rights, so that you won't be left with 0 admins)
- From user page, manage project membership
    - Join project(s) as member/manager/programmer
    - Remove from project(s) (red X button to the right of list of projects, or maybe trashcan icon)
        - This removes user from all roles in the project at once
    - Change project roles
        - Can set roles individually with checkboxes
- Suspend user (set inactive so user can't log in, but user account still exists)
- Unsuspend (reactivate) user
- Delete user account (remove from all projects - see if MySQL cascade will do the right thing here)
- Change user's password
    - If logged in as user, can change own password
    - If logged in as admin, can change anyone's password
        - Extra checkbox for "must change password on first login", defaulting to checked unless we're changing our own password
    - This is a separate step from editing user data
    - Same API endpoint? TBD

#### Other?
- Repositories: might be nice to be able to get an hg history, and/or rollback to a previous commit
    - Outside of scope for MVP


### What front end should look like

#### All-projects page

- Show all projects (if we're admin), or projects we're a member of (if we're not admin)
- Click on project to go to individual project page

#### Single project page

- Project name, description, code
- List of members, grouped by role (in order managers, contributors, observers, programmers)
- Members can appear multiple times: if I'm a manager and a contributor I show up in both lists
- Each member has an edit icon (pencil) and a delete icon (red X) next to their name in that role category
    - That just deletes the member from that role
- Each role has a green + (plus) icon underneath the member list to assign someone else in that role
    - Assigning someone requires you to type in a complete email and search for users by email address. If you don't know their email, no dice
        - This avoids data exposure
    - BUT if you're an admin, searching for users is greatly relaxed and you can search by name, etc.
    - MAYBE you can do a mass search and assign multiple users at once from this page? Select role first. Maybe a green ++ icon in the role??? Maybe not.
- All four roles are listed even if they have no members, in that same order
- If removing the last project manager, a popup says "Please confirm you want to remove the last project manager from project X, leaving project X with no project managers" and there are two choices, "Remove last manager" (not default, color gray) and "Do nothing" (default, color blue/green/whatever scheme)
- No confirmation dialog needed for other roles, of course

##### Not yet done from this page

- Green + icon doesn't yet do anything; should pop up a user-search dialog (which I have yet to implement)
    - Also note that how the user-search dialog behaves will depend on whether you have admin rights
    - In first-pass implementation, just provide a checkbox to say "I am an admin" so we can stick to front-end work

#### All-users page

- Only accessible to admins
- Show all users (grouped in sets of N - 20, 50, 100)
- Search by name / email
  - Query "first_name LIKE %foo%", same for last_name and email(s)
  - NOTE: This search will also be used in the single-project page, so make it reusable
- Some actions available right from this page:
    - Checkbox for mass ... what? deletion, but anything else?
    - Wait, is mass deletion even a good idea?
    - Okay, no checkbox here. Only clicking on users.
    - Or maybe select a bunch of users to assign to a single project (project search bar, select the project AND the role, then checkboxes appear)
- Click on user to go to single-user page

#### Single user page

- Admins can see anyone, otherwise must be logged in as the user you're looking at
    - Or perhaps a project manager can see details for the users in their own project? Yeah, that's a good rule.
    - That rule might have to wait till after MVP
- Edit name (first & last)
- Promote user to LD admin (only if currently logged in as admin)
- Demote user if was already admin, removing LD admin rights
- Search bar for projects (by name or code)
    - Add as (role), with four checkboxes for picking the role(s) to add (laid out with the checkboxes vertical)
        - Actually, clicking the green + pops up a little sub-menu: "Add as...". Can click role name to pick just that role, no checkboxes
        - When we're already a member, there's still the green + to add as some other role (only three/two/one role appears in sub-menu)
        - If all roles are selected, green + still appears for consistency but does nothing
    - Appears *above* list of existing projects
    - Search results box will push existing-projects list *down*
    - Search results box has some indication of membership (e.g, "Foo Project (manager, contributor)" to show that I'm already both of those roles)
    - Interaction buttons (green +, red X if we use it, pencil icon) only appear on hover? Or appear all the time? Start with "on hover" since that's
      the more challenging one, and replace with "all the time" if we decide that on-hover is too much
        - BUT... on smaller screens we have to have them showing up all the time! Mobile view can't hover!
        - Eh, clicking on the project will let you edit the user's memberships. Maybe the pencil is always there, but the + and X appear on hover.
        - Yeah, pencil always there, + and X on hover, is a good thing to aim for. Nice and complex to code, so it's challenging.
- List of existing projects that user is a member of, separated out by role (Manager of: (list), Contributor of: (list), etc.)
    - Projects can appear twice if I'm both a manager and a contributor, and so on
    - Remove from project (removes just that role)
    - Edit membership (goes to membership-editing page, see below)


#### Membership-editing page

- Edit one specific user's membership in one specific project
- Four checkboxes
- Removing manager role still checks whether user is last manager (especially important here because this page doesn't show who the other managers are)
- MAYBE: Invite user (provide email address, email is sent with a "click here to accept" code, user chooses username & password after accepting invite)
    - Would not require admin access, just manager access
    - Detects if email address already belongs to a user account, and then instead of creating a new user account, email says "you have been invited to join the X project" and then clicking the acceptance link just takes you to the project (you're automatically added without needing to accept the link, to keep things simpler)
    - This will be in version 2, after the MVP


### What data does the API need to send?

// RESPONSES

UserDetails = {
    username
    firstName
    lastName
    emailAddresses (default first, rest in whatever order they come back from the database)
        (two queries: select from emails where is_default = true, then select from emails where is_default = false. Then (default :: rest)).
    language (interface language for this user) - is this useful to the frontend? ... yeah, because when you log in, the front end wants to know who logged in and what language to give you
}

UserList = list of UserDetails

MemberList = {
    managers: [username list]
    contributors: [username list]
    observers: [username list]
    programmers: [username list]
}

ProjectDetails = {
    code: string
    name: string
    description: string
    membership: MemberList option  // Because we'll sometimes want the membership list, and sometimes not. Only supplied if we asked for it in the request API, otherwise it's None/omitted
}

ProjectList = list of ProjectDetails  // Depending on the situation, this will sometimes include MemberLists and sometimes it won't (e.g., list all projects vs. list one's I'm a member of)

// REQUESTS

LoginCredentials = {
    username: string
    password: string
}

CreateProject = {
    code: string
    name: string
    initialMembers: MemberList option
    login: LoginCredentials // TODO: Use "username" rather than "login" for login field in Redmine's users table
}

ArchiveProject = {
    code: string
    login: LoginCredentials
}

DeleteProject = // same as ArchiveProject

MembershipRecord = {
    username: string
    role: string  // One of four values allowed: "manager", "contributor", "observer", "programmer" -- in F# this will be a name-only DU
}

EditProjectMembership = {
    login: LoginCredentials (the login credentials of an existing admin account, not the one being demoted)
    add: MembershipRecord list option
    remove: MembershipRecord list option
    removeUser: username option
    // Must specify one, and exactly one, of these three. Internally this will be converted to a DU so we use onion architecture, but on the outside the JSON looks like three optional fields
}

ChangeUserActiveStatus = {
    login: LoginCredentials (the login credentials of an existing admin account, or the one being changed)
    username: string
    active: bool  // Suspended = false, active = true
}

DeleteUser = {
    login: LoginCredentials (the login credentials of an existing admin account, or the one being deleted)
    username: string
}

CreateUser = {
    login: LoginCredentials (the login credentials of the admin creating the account)
    username: string (required)
    password: string (required, cleartext - will be hashed by the server)
    mustChangePassword: bool (admin can choose for this to be false, but box is checked by default)
    firstName: string
    lastName: string
    language: string option (will default to "en" if not provided)
    emailAddresses: [string list]  (may be empty. If not empty, first email in list will be default email and rest will not be default)
}

EditUser = {
    login: LoginCredentials (the login credentials of the person editing the account: must either be same user as the account we're editing, or must be admin)
    username: string (required, cannot be changed in this API endpoint. A separate API endpoint exists for changing username)
    // NOT password: string option (We'll use a separate API endpoint to change the password.)
    firstName: string option
    lastName: string option
    language: string option (will default to no change if not provided, just like every other string option in this model)
    emailAddresses: [string list] option  (if not provided, no change. If provided, replace current list with new one, even if new list is empty)
}

ChangePassword = {
    login: LoginCredentials (the login credentials of the person changing the password: must either be same user as the account we're editing, or must be admin)
    username: string
    password: string
    mustChangePassword: bool option (option so it can be omitted: should be omitted/None if user is changing own password. If admin changing someone else's password, required.)
}

ChangeUsername = {
    login: LoginCredentials (the login credentials of the person changing the username: must either be same user as the account we're editing, or must be admin)
    oldUsername: string (required even if we're logging in as that user, for API consistency)
    newUsername: string
}

PromoteUserToAdmin = {
    login: LoginCredentials (the login credentials of an existing admin account, not the one being promoted)
    username: string
}

DemoteAdminToNormalUser = {
    login: LoginCredentials (the login credentials of an existing admin account, not the one being demoted)
    username: string
}

ShowProjectsUserBelongsTo = {
    login: LoginCredentials (the login credentials of an admin account, or the user account whose username is specified below)
    username: string
    role: string option (if unspecified, return all projects user is a member of, else show projects where username holds this role)
}

SearchProjects = {
    login: LoginCredentials (the login credentials of the user doing the search)
    searchText: string option  // Search by either name or project code, but not(?) description
    // If searchText is omitted, will return all projects ONLY if login account is admin. Otherwise will return all public projects.
    offset: int option // If specified, used for paging
    limit: int option  // If specified, used for paging
}

SearchUsers = {
    login: LoginCredentials (the login credentials of the user doing the search)
    emailSearch: string option // If specified, must be exact match (though case-insensitive (in invariant culture) so not "exact" by that standard)
    nameSearch: string option // If specified, will do "LIKE '%str%'", so beware SQL injection here.
    // If both email and name are specified, will do an AND. If an OR is desired, submit two API calls, one with emailSearch and one with nameSearch, and frontend can join them together while removing overlapping usernames
    // If neither email nor name is specified, will return all users ONLY if login account is admin. Otherwise will return HTTP "Not Authorized" error.
    offset: int option // If specified, used for paging
    limit: int option  // If specified, used for paging
}

