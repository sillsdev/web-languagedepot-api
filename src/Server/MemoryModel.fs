module MemoryModel

open Shared
open FSharp.Control.Tasks.V2
open System.Collections.Generic

// Uses MemoryStorage to provide API calls

let adminEmails = [
    "admin@example.net"
    "robin_munn@sil.org"
]

let doLimitOffset limit offset =
    let limitFn = match limit with
                  | Some limit -> Seq.take<'a> limit
                  | None -> id
    let offsetFn = match offset with
                   | Some offset -> Seq.skip<'a> offset
                   | None -> id
    limitFn >> offsetFn

type MemoryModel() =
    member this.isRealProject (proj : Dto.ProjectDetails) =
        let projType = GuessProjectType.guessType proj.code proj.name proj.description
        projType <> Test && not (proj.code.StartsWith "test")

    member this.isMemberOf (proj : Dto.ProjectDetails) username =
        proj.membership |> Map.containsKey username

    member this.mkUserDetailsFromApiData (createUserApiData : Api.CreateUser) : Dto.UserDetails = {
        username = createUserApiData.username
        firstName = createUserApiData.firstName
        lastName = createUserApiData.lastName
        email = createUserApiData.emailAddresses
        language = createUserApiData.language |> Option.defaultValue "en"
    }

    member this.addOrRemoveInList isAdd item lst =
        if isAdd then
            if lst |> List.contains item then lst else item :: lst
        else
            lst |> List.filter (fun listItem -> listItem <> item)

    interface Model.IModel with
        member this.ListUsers limit offset = task {
            return MemoryStorage.userStorage.Values |> doLimitOffset limit offset |> Array.ofSeq
        }

        member this.ListProjects limit offset = task {
            return MemoryStorage.projectStorage.Values |> doLimitOffset limit offset |> Array.ofSeq
        }

        member this.ListProjectsAndRoles limit offset = task {
            return MemoryStorage.projectStorage.Values |> doLimitOffset limit offset |> Array.ofSeq
        }

        member this.CountUsers() = task {
            return int64 MemoryStorage.userStorage.Count
        }

        member this.CountProjects() = task {
            return int64 MemoryStorage.projectStorage.Count
        }
        member this.CountRealProjects() = task {
            return MemoryStorage.projectStorage.Values |> Seq.filter this.isRealProject |> Seq.length |> int64
        }

        member this.ListRoles limit offset = task {
            return SampleData.StandardRoles |> doLimitOffset limit offset |> Array.ofSeq |> Array.map (fun role -> role.id, role.name)
        }

        member this.ProjectsAndRolesByUser username = task {
            let projectsAndRoles = MemoryStorage.projectStorage.Values |> Seq.choose (fun proj ->
                let maybeRole = proj.membership |> Map.tryFind username
                match maybeRole with
                | None -> None
                | Some role -> Some (proj,role))
            return Array.ofSeq projectsAndRoles
        }

        member this.LegacyProjectsAndRolesByUser username = task {
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

        member this.ProjectsAndRolesByUserRole username roleType = task {
            let! projectsAndRoles = (this :> Model.IModel).ProjectsAndRolesByUser username
            return (projectsAndRoles |> Array.filter (fun (proj, role) -> role = roleType))
        }

        member this.ProjectsByUser username = task {
            let! projectsAndRoles = (this :> Model.IModel).ProjectsAndRolesByUser username
            return projectsAndRoles |> Array.map fst
        }

        member this.ProjectsByUserRole username roleType = task {
            let! projectsAndRoles = (this :> Model.IModel).ProjectsAndRolesByUserRole username roleType
            return projectsAndRoles |> Array.map fst
        }

        member this.UserExists username = task {
            return MemoryStorage.userStorage.ContainsKey username
        }

        member this.ProjectExists code = task {
            return MemoryStorage.projectStorage.ContainsKey code
        }

        member this.IsAdmin username = task {
            return username = "admin"
        }

        member this.SearchUsersExact searchText = task {
            return
                MemoryStorage.userStorage.Values
                |> Seq.filter (fun user ->
                    user.username = searchText ||
                    user.firstName = searchText ||
                    user.lastName = searchText ||
                    user.email |> Option.contains searchText)
                |> Array.ofSeq
        }

        member this.SearchUsersLoose (searchText : string) = task {
            return
                MemoryStorage.userStorage.Values
                |> Seq.filter (fun user ->
                    user.username.Contains(searchText) ||
                    user.firstName.Contains(searchText) ||
                    user.lastName.Contains(searchText) ||
                    (user.email |> Option.defaultValue "").Contains(searchText))
                |> Array.ofSeq
        }

        member this.SearchProjectsExact searchText = task {
            return
                MemoryStorage.projectStorage.Values
                |> Seq.filter (fun project ->
                    project.code = searchText ||
                    project.name = searchText ||
                    project.description = searchText)
                |> Array.ofSeq
        }

        member this.SearchProjectsLoose (searchText : string) = task {
            return
                MemoryStorage.projectStorage.Values
                |> Seq.filter (fun project ->
                    project.code.Contains(searchText) ||
                    project.name.Contains(searchText) ||
                    project.description.Contains(searchText))
                |> Array.ofSeq
        }

        member this.GetUser username = task {
            return
                match MemoryStorage.userStorage.TryGetValue username with
                | false, _ -> None
                | true, user -> Some user
        }

        member this.GetProject projectCode = task {
            return
                match MemoryStorage.projectStorage.TryGetValue projectCode with
                | false, _ -> None
                | true, project -> Some project
        }

        member this.GetProjectWithRoles projectCode = (this :> Model.IModel).GetProject projectCode

        member this.CreateProject (createProjectApiData : Api.CreateProject) = task {
            let proj : Dto.ProjectDetails = {
                code = createProjectApiData.code
                name = createProjectApiData.name
                description = createProjectApiData.description |> Option.defaultValue ""
                membership = createProjectApiData.initialMembers
            }
            let added = MemoryStorage.projectStorage.TryAdd(proj.code,proj)
            return if added then 1 else 0
        }

        member this.CreateUser (createUserApiData : Api.CreateUser) = task {
            let user = this.mkUserDetailsFromApiData createUserApiData
            let added = MemoryStorage.userStorage.TryAdd(user.username,user)
            return if added then 1 else 0
        }

        member this.UpsertUser username (createUserApiData : Api.CreateUser) = task {
            if username = createUserApiData.username then
                let newUser = this.mkUserDetailsFromApiData createUserApiData
                MemoryStorage.userStorage.AddOrUpdate(username, newUser, fun _ _ -> newUser) |> ignore
                return 1
            else
                match MemoryStorage.replaceUsername username createUserApiData.username with
                | Error _ -> return 0
                | Ok _ ->
                    // Now recursive call to update the rest of the info
                    return! (this :> Model.IModel).UpsertUser createUserApiData.username createUserApiData
        }

        member this.UpdateUser username (createUserApiData : Api.CreateUser) = task {
            if username = createUserApiData.username then
                let newUser = this.mkUserDetailsFromApiData createUserApiData
                MemoryStorage.userStorage.AddOrUpdate(username, newUser, fun _ _ -> newUser) |> ignore
                return 1
            else
                match MemoryStorage.replaceUsername username createUserApiData.username with
                | Error _ -> return 0
                | Ok _ ->
                    // Now recursive call to update the rest of the info
                    return! (this :> Model.IModel).UpsertUser createUserApiData.username createUserApiData
        }

        member this.ChangePassword username (changePasswordApiData : Api.ChangePassword) = task {
            let cleartext = changePasswordApiData.password
            MemoryStorage.storeNewPassword username changePasswordApiData.password
            return true
        }

        member this.VerifyLoginInfo loginCredentials = task {
            // Skip verifying login credentials until I update the sample data to have "x" as the default password everywhere
            return true
            // match MemoryStorage.passwordStorage.TryGetValue loginCredentials.username with
            // | false, _ -> return false  // User not found also returns false, so we don't disclose the lack of a username
            // | true, passwordDetails ->
            //     let hashedPassword = PasswordHashing.hashPassword passwordDetails.salt loginCredentials.password
            //     return hashedPassword = passwordDetails.hashedPassword
        }

        member this.AddMembership username projectCode (roleName : string) = task {
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

        member this.RemoveMembership (username : string) (projectCode : string) = task {
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

        member this.ArchiveProject projectCode = raise (System.NotImplementedException("Not implemented"))

        member this.EmailIsAdmin email = task { return adminEmails |> List.contains email }

        member this.IsUserManagerOfProject username projectCode = task { return true }

module ModelRegistration =
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.DependencyInjection.Extensions

    let registerServices (builder : IServiceCollection) (_connString : string) =
        MemoryStorage.initFromData SampleData.Users SampleData.Projects
        builder
            .RemoveAll<MemoryModel>()
            .AddSingleton<MemoryModel>(MemoryModel())  // TODO: This needs to be an instance of a common ancestor class with MySqlModel
        |> ignore
