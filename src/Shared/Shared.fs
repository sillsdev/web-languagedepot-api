namespace Shared

type SharedUser = {
    Name : string
}
type SharedProjects = {
    Projects : string list
}

type LoginInfo = {
    username : string
    password : string
}

type AddUserToProjects = {
    Add : SharedUser
}

type RemoveUserFromProjects = {
    Remove : SharedUser
}

type PatchProjects =
    | Add of AddUserToProjects
    | Remove of RemoveUserFromProjects
