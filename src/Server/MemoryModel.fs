module MemoryModel

open Shared
open FSharp.Control.Tasks.V2
open System.Collections.Generic

// Uses MemoryStorage to provide API calls

type MemoryModel() =
    // TODO: This needs to be an instance of a common ancestor class with MySqlModel
    member this.listUsers limit offset = task {
        let limitFn = match limit with
                      | Some limit -> Seq.take limit
                      | None -> id
        let offsetFn = match offset with
                       | Some offset -> Seq.skip offset
                       | None -> id
        return MemoryStorage.userStorage.Values |> offsetFn |> limitFn |> Array.ofSeq
    }

    member this.listProjects() = task {
        return MemoryStorage.projectStorage.Values |> Array.ofSeq
    }

    member this.countUsers() = task {
        return int64 MemoryStorage.userStorage.Count
    }

    member this.countProjects() = task {
        return int64 MemoryStorage.projectStorage.Count
    }

    member this.isRealProject (proj : Dto.ProjectDetails) =
        let projType = GuessProjectType.guessType proj.code proj.name proj.description
        projType <> Test && not (proj.code.StartsWith "test")

    member this.countRealProjects() = task {
        return MemoryStorage.projectStorage.Values |> Seq.filter this.isRealProject |> Seq.length |> int64
    }

    member this.listRoles() = task {
        return SampleData.StandardRoles |> Array.map (fun role -> role.id, role.name)
    }

    member this.isMemberOf (proj : Dto.ProjectDetails) username =
        proj.membership |> Map.containsKey username

    member this.projectsAndRolesByUser username = task {
        let projectsAndRoles = MemoryStorage.projectStorage.Values |> Seq.choose (fun proj ->
            let maybeRole = proj.membership |> Map.tryFind username
            match maybeRole with
            | None -> None
            | Some role -> Some (proj,role))
        return Array.ofSeq projectsAndRoles
    }

    member this.legacyProjectsAndRolesByUser username = task {
        let projectsAndRoles = MemoryStorage.projectStorage.Values |> Seq.choose (fun proj ->
            let maybeRole = proj.membership |> Map.tryFind username
            match maybeRole with
            | None -> None
            | Some role -> Some (proj,role))
        let legacyProjectDetails =
            projectsAndRoles
            |> Seq.map (fun (proj,role) ->
                let result : Dto.LegacyProjectDetails = {
                    identifier = proj.code
                    name = proj.name
                    repository = "http://public.languagedepot.org"
                    role = role
                }
                result
            )
            |> Array.ofSeq
        return legacyProjectDetails
    }

    member this.projectsAndRolesByUserRole username roleType = task {
        let! projectsAndRoles = this.projectsAndRolesByUser username
        return (projectsAndRoles |> Array.filter (fun (proj, role) -> role = roleType))
    }

    member this.projectsByUser username = task {
        let! projectsAndRoles = this.projectsAndRolesByUser username
        return projectsAndRoles |> Array.map fst
    }

    member this.projectsByUserRole username roleType = task {
        let! projectsAndRoles = this.projectsAndRolesByUserRole username roleType
        return projectsAndRoles |> Array.map fst
    }

    member this.userExists username = task {
        return MemoryStorage.userStorage.ContainsKey username
    }

    member this.projectExists code = task {
        return MemoryStorage.projectStorage.ContainsKey code
    }

    member this.isAdmin username = task {
        return username = "admin"
    }

    member this.searchUsersExact searchText = task {
        return
            MemoryStorage.userStorage.Values
            |> Seq.filter (fun user ->
                user.username = searchText ||
                user.firstName = searchText ||
                user.lastName = searchText ||
                user.email |> Option.contains searchText)
            |> Array.ofSeq
    }

    member this.searchUsersLoose (searchText : string) = task {
        return
            MemoryStorage.userStorage.Values
            |> Seq.filter (fun user ->
                user.username.Contains(searchText) ||
                user.firstName.Contains(searchText) ||
                user.lastName.Contains(searchText) ||
                (user.email |> Option.defaultValue "").Contains(searchText))
            |> Array.ofSeq
    }

    member this.getUser username = task {
        return
            match MemoryStorage.userStorage.TryGetValue username with
            | false, _ -> None
            | true, user -> Some user
    }

    member this.getProject projectCode = task {
        return
            match MemoryStorage.projectStorage.TryGetValue projectCode with
            | false, _ -> None
            | true, project -> Some project
    }

    member this.createProject (createProjectApiData : Api.CreateProject) = task {
        let proj : Dto.ProjectDetails = {
            code = createProjectApiData.code
            name = createProjectApiData.name
            description = createProjectApiData.description |> Option.defaultValue ""
            membership = createProjectApiData.initialMembers
        }
        let added = MemoryStorage.projectStorage.TryAdd(proj.code,proj)
        return if added then 1 else 0
    }

    member this.mkUserDetailsFromApiData (createUserApiData : Api.CreateUser) : Dto.UserDetails = {
        username = createUserApiData.username
        firstName = createUserApiData.firstName
        lastName = createUserApiData.lastName
        email = createUserApiData.emailAddresses
        language = createUserApiData.language |> Option.defaultValue "en"
    }

    member this.createUser (createUserApiData : Api.CreateUser) = task {
        let user = this.mkUserDetailsFromApiData createUserApiData
        let added = MemoryStorage.userStorage.TryAdd(user.username,user)
        return if added then 1 else 0
    }

    member this.upsertUser username (createUserApiData : Api.CreateUser) = task {
        if username = createUserApiData.username then
            let newUser = this.mkUserDetailsFromApiData createUserApiData
            MemoryStorage.userStorage.AddOrUpdate(username, newUser, fun _ _ -> newUser) |> ignore
            return 1
        else
            match MemoryStorage.replaceUsername username createUserApiData.username with
            | Error _ -> return 0
            | Ok _ ->
                // Now recursive call to update the rest of the info
                return! this.upsertUser createUserApiData.username createUserApiData
    }

    member this.changePassword username (changePasswordApiData : Api.ChangePassword) = task {
        let cleartext = changePasswordApiData.password
        MemoryStorage.storeNewPassword username changePasswordApiData.password
        return true
    }

    member this.verifyLoginCredentials loginCredentials = task {
        // Skip verifying login credentials until I update the sample data to have "x" as the default password everywhere
        return true
        // match MemoryStorage.passwordStorage.TryGetValue loginCredentials.username with
        // | false, _ -> return false  // User not found also returns false, so we don't disclose the lack of a username
        // | true, passwordDetails ->
        //     let hashedPassword = PasswordHashing.hashPassword passwordDetails.salt loginCredentials.password
        //     return hashedPassword = passwordDetails.hashedPassword
    }

    member this.addOrRemoveInList isAdd item lst =
        if isAdd then
            if lst |> List.contains item then lst else item :: lst
        else
            lst |> List.filter (fun listItem -> listItem <> item)

    member this.addMembership username projectCode (roleName : string) = task {
        match MemoryStorage.userStorage.TryGetValue username with
        | false, _ -> return false
        | true, _ ->  // We don't need user details, we just want to make sure the user exists
            match MemoryStorage.projectStorage.TryGetValue projectCode with
            | false, _ -> return false
            | true, projectDetails ->
                let update (details : Dto.ProjectDetails) =
                    let newMemberList = details.membership |> Map.add username roleName
                    { details with membership = newMemberList }
                MemoryStorage.projectStorage.AddOrUpdate(username,
                    (fun _ -> update projectDetails),
                    (fun _ oldDetails -> update oldDetails)) |> ignore
                return true
    }

    member this.removeMembership (username : string) (projectCode : string) = task {
        match MemoryStorage.userStorage.TryGetValue username with
        | false, _ -> return false
        | true, _ ->  // We don't need user details, we just want to make sure the user exists
            match MemoryStorage.projectStorage.TryGetValue projectCode with
            | false, _ -> return false
            | true, projectDetails ->
                let update (details : Dto.ProjectDetails) =
                    let newMemberList = details.membership |> Map.remove username
                    { details with membership = newMemberList }
                MemoryStorage.projectStorage.AddOrUpdate(username,
                    (fun _ -> update projectDetails),
                    (fun _ oldDetails -> update oldDetails)) |> ignore
                return true
    }

    member this.archiveProject = fun projectCode -> raise (System.NotImplementedException("Not implemented"))

module ModelRegistration =
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.DependencyInjection.Extensions

    let registerServices (builder : IServiceCollection) (_connString : string) =
        MemoryStorage.initFromData SampleData.Users SampleData.Projects
        builder
            .RemoveAll<MemoryModel>()
            .AddSingleton<MemoryModel>(MemoryModel())  // TODO: This needs to be an instance of a common ancestor class with MySqlModel
        |> ignore
