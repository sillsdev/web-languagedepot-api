namespace Shared

type SharedUser = {
    Name : string
    Email : string
}

type SharedProjects = {
    Projects : string list
}

type LoginInfo = {
    username : string
    password : string
}

type PatchProjects = {
    Add : SharedUser option
    Remove : SharedUser option
}
