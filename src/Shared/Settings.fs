namespace Shared

module Settings =
    open DefaultHelpers

    [<CLIMutable>]
    type MySqlSettings = {
        Hostname : string
        Database : string
        Port : int
        User : string
    } with
        member this.SetDefaultValues() =
          { Hostname = this.Hostname |> defaultValue "default hostname"
            Database = this.Database |> defaultValue "default database"
            Port = this.Port |> defaultEnvParsed System.Int32.Parse "PORT" 3306
            User = this.User |> defaultEnv "USER" "mysql" }
        member this.ConnString =
            sprintf "Server=%s;Database=%s;User=%s" this.Hostname this.Database this.User
