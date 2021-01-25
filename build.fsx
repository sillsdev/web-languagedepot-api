#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.Runtime
open Fake.BuildServer

let args = Target.getArguments() |> Option.defaultValue [||]
let argUsage = """
Deploy script.

Usage:
  Deploy [options]

Options:
  --upload-destination=<dest>       Upload destination for rsync command (user@host:/path)
  --create-restart-file=<filename>  Optionally create a restart file after rsync completes
  --secret-api-token=<token>        Secret API token (should not have any punctuation other than -_./~+)

The deployment target may be specified either at the command line or via a TeamCity
system parameter named `system.upload.destination`. Valid values are:
  testing - Deploy to local Vagrant VM for testing
  staging - Deploy to QA server
  live - Deploy to live server
  foo@bar.baz: - Deploy to the foo@bar.baz: location

The secret API token should be specified as a TeamCity system parameter
named `system.secret.api.token`. Only specify it as a command-line option
if you are running a development build yourself.
"""
let parser = Docopt(argUsage)
let parsedArgs = parser.Parse(args)

let optionNameToTeamCityName (optionName : string) =
    let start = if optionName.StartsWith "--" then 2 else 0
    optionName.Substring(start).Replace('-','.')
    // No `system.` at front since FAKE adds that for you

let getArg optionName =
    parsedArgs
    |> DocoptResult.tryGetArgument optionName
    |> Option.orElseWith (fun () ->
        if BuildServer.buildServer = TeamCity then
            let varName = optionNameToTeamCityName optionName
            TeamCity.BuildParameters.System |> Map.tryFind varName
        else
            None
        )

let getRequiredArg optionName =
    match getArg optionName with
    | None -> failwith <| sprintf "Option %s is required; please specify it either on command line or in TeamCity variable 'system.%s'" optionName (optionNameToTeamCityName optionName)
    | Some value -> value

let serverPath = Path.getFullName "./src/Server"
let clientPath = Path.getFullName "./src/Client"
let clientDeployPath = Path.combine clientPath "deploy"
let deployDir = Path.getFullName "./deploy"
let bundleDir = Path.combine deployDir "server"

let serverPort = Environment.environVarOrDefault "SERVER_PROXY_PORT" "8085"
let clientPort = Environment.environVarOrDefault "ANGULAR_SERVE_PORT" "4200"

let testSqlPath = Path.getFullName "./testlanguagedepot.sql"

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let nodeTool = platformTool "node" "node.exe"
let npmTool = platformTool "npm" "npm.cmd"
let npxTool = platformTool "npx" "npx.cmd"
let ngTool =
    let basename = if Environment.isUnix then "ng" else "ng.cmd"
    clientPath </> "node_modules" </> "@angular" </> "cli" </> "bin" </> basename
// let yarnTool = platformTool "yarn" "yarn.cmd"

let runTool cmd arguments workingDir =
    Command.RawCommand (cmd, arguments |> Arguments.OfArgs)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runToolSimple cmd args workingDir =
    let arguments = args |> String.split ' '
    runTool cmd arguments workingDir

let withVerbosity v (x : DotNet.Options) = { x with Verbosity = Some v }

let runDotNet cmd workingDir =
    // Process.setEnvironmentVariable "ASPNETCORE_ENVIRONMENT" "Development"
    Environment.setEnvironVar "ASPNETCORE_ENVIRONMENT" "Development"  // TODO: Set this from some external variable, or in TeamCity, or something
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir >> withVerbosity DotNet.Verbosity.Detailed) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let openBrowser url =
    //https://github.com/dotnet/corefx/issues/10361
    Command.ShellCommand url
    |> CreateProcess.fromCommand
    |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
    |> Proc.run
    |> ignore


Target.create "Clean" (fun _ ->
    [ bundleDir
      clientDeployPath ]
    |> Shell.cleanDirs
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runToolSimple nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Npm version:"
    runToolSimple npmTool "--version"  __SOURCE_DIRECTORY__
    runToolSimple npmTool "install" clientPath
)

Target.create "SetApiToken" (fun _ ->
    match getArg "--secret-api-token" with
    | None -> ()
    | Some apiToken ->
        Shell.regexReplaceInFileWithEncoding
            "let \\[<Literal>\\] SecretApiToken = \"[^\"]*\""
            (sprintf "let [<Literal>] SecretApiToken = \"%s\"" apiToken)
            (System.Text.UTF8Encoding(false))
            (serverPath @@ "Server.fs")
        // And ensure it will always be reset after the build finishes, even if the build fails
        Target.activateFinal "ResetApiToken"
)

Target.createFinal "ResetApiToken" (fun _ ->
    Shell.regexReplaceInFileWithEncoding
        "let \\[<Literal>\\] SecretApiToken = \"[^\"]*\""
        "let [<Literal>] SecretApiToken = \"not-a-secret\""
        (System.Text.UTF8Encoding(false))
        (serverPath @@ "Server.fs")
)

Target.create "Build" (fun _ ->
    runDotNet "build" serverPath
    // Shell.regexReplaceInFileWithEncoding
    //     "let app = \".+\""
    //    ("let app = \"" + release.NugetVersion + "\"")
    //     System.Text.Encoding.UTF8
    //     (Path.combine clientPath "Version.fs")
    runToolSimple ngTool "build" clientPath
)

Target.create "BuildServerOnly" (fun _ ->
    runDotNet "build" serverPath
)

Target.create "Run" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }

    let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"
    let safeClientOnly = Environment.hasEnvironVar "safeClientOnly"

    let client = async {
        runTool ngTool ["serve"; "--port"; clientPort; "--proxy-config"; "proxy.conf.json"; if not vsCodeSession then "--open"] clientPath
    }

    let tasks =
        [ if not safeClientOnly then yield server
          yield client ]

    tasks
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

let buildDocker tag =
    let args = sprintf "build -t %s ." tag
    runToolSimple "docker" args __SOURCE_DIRECTORY__

let deploy restartFilename dest =
    match restartFilename with
    | None -> ()
    | Some fname -> File.delete fname  // Ensure we won't upload it by accident
    let args = sprintf "-vzr --exclude=secrets.json --exclude=ldapi-server.ini server/Server/ %s" dest
    runToolSimple "rsync" args deployDir
    // If the rsync fails, an exception will be thrown so the next part won't execute
    Trace.tracefn "Upload to %s succeeded, triggering server restart" dest
    match restartFilename with
    | None -> ()
    | Some fname ->
        File.create fname
        let args = sprintf "-v %s %s" fname dest
        runToolSimple "rsync" args deployDir

let vagrant() =
    runToolSimple "vagrant" "up" deployDir

Target.create "Bundle" (fun _ ->
    let serverDir = Path.combine bundleDir "Server"
    // let clientDir = Path.combine bundleDir "Client"
    // let publicDir = Path.combine clientDir "public"

    let publishArgs = sprintf "publish -c Release -o \"%s\"" serverDir
    runDotNet publishArgs serverPath

    // Shell.copyDir publicDir clientDeployPath FileFilter.allFiles
)

let dockerUser = "rmunn"
let dockerImageName = "ldapi"
let dockerFullName = sprintf "%s/%s" dockerUser dockerImageName

Target.create "DeployLive" (fun _ ->
    if false then  // Don't run until deployment user on live server is ready
        deploy None "deploy@admin.languagedepot.org"
    ()
)

Target.create "DeployStaging" (fun _ ->
    runToolSimple "rsync" "server/Server/ " deployDir
)

Target.create "DeployTest" (fun _ ->
    vagrant ()
)

Target.create "Deploy" (fun _ ->
    let target = getRequiredArg "--upload-destination"
    Trace.tracefn "Deployment target: %A" target

    let restartFilename = getArg "--create-restart-file" |> Option.map (fun filename -> bundleDir @@ filename)

    match target with
    | "testing" -> failwith "Please run \"DeployTesting\" target instead"
    // | "staging" -> "deploy@admin.qa.languagedepot.org:upload_areas/ldapi"  // TODO: Activate once deploy user is ready on staging server
    | "staging" -> deploy restartFilename "rmunn@admin.qa.languagedepot.org:/usr/lib/"
    | "live" -> failwith "Don't run \"Deploy live\" until live server is actually ready"
    | dest -> deploy restartFilename dest
)

// NOTE: Before this will work, you must run "sudo mysql" and do something like:
// CREATE DATABASE testldapi;
// CREATE USER 'foo'@'localhost' IDENTIFIED BY '';
// GRANT ALL PRIVILEGES ON testldapi.* TO 'foo'@'localhost' IDENTIFIED BY '';
Target.create "RestoreSql" (fun _ ->
    let arguments = Arguments.Empty
    let cmd = __SOURCE_DIRECTORY__ @@ "restoretestdata.sh"
    let proc =
        Command.RawCommand (cmd, arguments)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory __SOURCE_DIRECTORY__
        |> CreateProcess.ensureExitCode
    Proc.run proc |> ignore
)

open Fake.Core.TargetOperators

"Clean"
    // ==> "InstallClient"
    ==> "SetApiToken"
    ==> "BuildServerOnly"
    ==> "Bundle"
    ==> "DeployTest"
    <=> "DeployStaging"
    <=> "DeployLive"
    <=> "Deploy"   // Uncomment if you want to run dependencies, comment out while testing this in solo (after dependencies have run once)


"Clean"
    ==> "InstallClient"
    ==> "RestoreSql"
    ==> "Run"

Target.runOrDefaultWithArguments "Build"
