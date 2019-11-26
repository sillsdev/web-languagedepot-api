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

Target.initEnvironment ()

let serverPath = Path.getFullName "./src/Server"
let clientPath = Path.getFullName "./src/Client"
let clientDeployPath = Path.combine clientPath "deploy"
let deployDir = Path.getFullName "./deploy"
let bundleDir = Path.combine deployDir "server"

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
let yarnTool = platformTool "yarn" "yarn.cmd"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    // Process.setEnvironmentVariable "ASPNETCORE_ENVIRONMENT" "Development"
    Environment.setEnvironVar "ASPNETCORE_ENVIRONMENT" "Development"  // TODO: Set this from some external variable, or in TeamCity, or something
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
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
    runTool nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    runTool yarnTool "--version" __SOURCE_DIRECTORY__
    runTool yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
)

Target.create "Build" (fun _ ->
    runDotNet "build" serverPath
    Shell.regexReplaceInFileWithEncoding
        "let app = \".+\""
       ("let app = \"" + release.NugetVersion + "\"")
        System.Text.Encoding.UTF8
        (Path.combine clientPath "Version.fs")
    runTool yarnTool "webpack-cli -p" __SOURCE_DIRECTORY__
)

Target.create "Run" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }
    let client = async {
        runTool yarnTool "webpack-dev-server" __SOURCE_DIRECTORY__
    }
    let browser = async {
        do! Async.Sleep 5000
        openBrowser "http://localhost:8080"
    }

    let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"
    let safeClientOnly = Environment.hasEnvironVar "safeClientOnly"

    let tasks =
        [ if not safeClientOnly then yield server
          yield client
          if not vsCodeSession then yield browser ]

    tasks
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

let buildDocker tag =
    let args = sprintf "build -t %s ." tag
    runTool "docker" args __SOURCE_DIRECTORY__

let ansible limit skipTags =
    let args =
        if String.IsNullOrEmpty limit
            then "-i hosts main.yaml"
            else sprintf "-i hosts main.yaml --limit '%s' --skip-tags '%s'" limit skipTags
    runTool "ansible-playbook" args deployDir

let vagrant() =
    runTool "vagrant" "up" deployDir

Target.create "Bundle" (fun _ ->
    let serverDir = Path.combine bundleDir "Server"
    let clientDir = Path.combine bundleDir "Client"
    let publicDir = Path.combine clientDir "public"

    let publishArgs = sprintf "publish -c Release -o \"%s\"" serverDir
    runDotNet publishArgs serverPath

    Shell.copyDir publicDir clientDeployPath FileFilter.allFiles
)

let dockerUser = "rmunn"
let dockerImageName = "ldapi"
let dockerFullName = sprintf "%s/%s" dockerUser dockerImageName

Target.create "DeployLive" (fun _ ->
    ansible "live" "db,testing"
)

Target.create "DeployStaging" (fun _ ->
    ansible "staging" "db,testing"
)

Target.create "DeployTest" (fun _ ->
    vagrant ()
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

Target.create "CopyNeededMySqlDlls" (fun _ ->
    let dllsNeeded = [
        "packages/sql/MySqlConnector/lib/netstandard2.0/MySqlConnector.dll"
        "packages/sql/System.Buffers/lib/netstandard2.0/System.Buffers.dll"
        "packages/sql/System.Runtime.InteropServices.RuntimeInformation/lib/netstandard1.1/System.Runtime.InteropServices.RuntimeInformation.dll"
        "packages/sql/System.Threading.Tasks.Extensions/lib/netstandard2.0/System.Threading.Tasks.Extensions.dll"
        "packages/sql/System.Memory/lib/netstandard2.0/System.Memory.dll"
        "packages/sql/System.Runtime.CompilerServices.Unsafe/lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll"
    ]
    dllsNeeded
    // |> List.map (fun dllName -> __SOURCE_DIRECTORY__ @@ sprintf "packages/sql/%s/lib/netstandard2.0/%s.dll" dllName dllName)
    |> Shell.copy serverPath
)

open Fake.Core.TargetOperators

"Clean"
    ==> "InstallClient"
    // ==> "CopyNeededMySqlDlls"
    ==> "Build"
    ==> "Bundle"
    ==> "DeployTest"
    <=> "DeployStaging"
    <=> "DeployLive"


"Clean"
    ==> "InstallClient"
    ==> "RestoreSql"
    ==> "Run"

Target.runOrDefaultWithArguments "Build"
