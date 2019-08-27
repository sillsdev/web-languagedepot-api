namespace Shared

module Settings =
    let defaultValue (def : 'a) (value : 'a) =
        if value = Unchecked.defaultof<'a> then def else value

    let defaultEnv (env : string) (def : string) (value : string) =
#if FABLE_COMPILER
        // Javascript can't access environment variables
        defaultValue def value
#else
        if isNull value then
            let result = System.Environment.GetEnvironmentVariable env
            if System.String.IsNullOrEmpty result then def else result
        else
            value
#endif

    let defaultEnvParsed (parse : string -> 'a) (env : string) (def : 'a) (value : 'a) =
#if FABLE_COMPILER
        defaultValue def value
#else
        if value = Unchecked.defaultof<'a> then
            let result = System.Environment.GetEnvironmentVariable env
            if System.String.IsNullOrEmpty result then def else parse result
        else
            value
#endif

    [<CLIMutable>]
    type AudioSettings = {
        FfmpegPath : string
        Development : string
        Path : string
    } with
        member this.SetDefaultValues() =
            { this with
                FfmpegPath = this.FfmpegPath |> defaultValue "default path"
                Development = this.Development |> defaultValue "default dev"
                Path = this.Path |> defaultEnv "PATH" "default path" }

    [<CLIMutable>]
    type MySqlSettings = {
        Hostname : string
        Database : string
        Port : int
        User : string
    } with
        member this.SetDefaultValues() =
            { this with
                Hostname = this.Hostname |> defaultValue "default hostname"
                Database = this.Database |> defaultValue "default database"
                Port = this.Port |> defaultEnvParsed System.Int32.Parse "PORT" 3306
                User = this.User |> defaultEnv "USER" "mysql"}
        member this.ConnString =
            sprintf "Server=%s;Database=%s;User=%s" this.Hostname this.Database this.User
