namespace Shared

module Settings =
    open DefaultHelpers

    [<CLIMutable>]
    type MySqlSettings = {
        Hostname : string
        Database : string
        Password : string
        Port : int
        User : string
    } with
        member this.SetDefaultValues() =
          printfn "Setting default values for %A" this
          { Hostname = this.Hostname |> defaultValue "default hostname"
            Database = this.Database |> defaultValue "default database"
            Password = this.Password |> defaultValue "no password given"
            Port = this.Port |> defaultEnvParsed System.Int32.Parse "PORT" 3306
            User = this.User |> defaultEnv "USER" "mysql" }
        member this.ConnString =
            sprintf "Server=%s;Database=%s;Uid=%s" this.Hostname this.Database this.User
