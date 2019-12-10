module MockServer

open JsonHelpers

let getAllProjects() = async {
    return success SampleData.Projects
}

let getAllUsers() = async {
    return success SampleData.Users
}

let getUser username = async {
    return SampleData.Users |> Array.tryFind (fun user -> user.username = username) |> success
}

// TODO: Continue writing this
