namespace Shared

module Settings =
    open DefaultHelpers

    [<CLIMutable>]
    type MySqlSettings = {
        Hostname : string
        Database : string
        DatabasePrivate : string
        Password : string
        PasswordPrivate : string
        Port : int
        User : string
    } with
        member this.SetDefaultValues() =
          printfn "Setting default values for %A" this
          { Hostname = this.Hostname |> defaultValue "default hostname"
            Database = this.Database |> defaultValue "default database"
            DatabasePrivate = this.DatabasePrivate |> defaultValue "default private database"
            Password = this.Password |> defaultValue ""
            PasswordPrivate = this.PasswordPrivate |> defaultValue (this.Password |> defaultValue "")
            Port = this.Port |> defaultEnvParsed System.Int32.Parse "PORT" 3306
            User = this.User |> defaultEnv "USER" "mysql" }
        member this.ConnString =
            if System.String.IsNullOrEmpty this.Password then
                sprintf "Server=%s;Database=%s;Uid=%s" this.Hostname this.Database this.User
            else
                sprintf "Server=%s;Database=%s;Uid=%s;Pwd=%s" this.Hostname this.Database this.User this.Password
        member this.ConnStringPrivate =
            if System.String.IsNullOrEmpty this.PasswordPrivate then
                sprintf "Server=%s;Database=%s;Uid=%s" this.Hostname this.DatabasePrivate this.User
            else
                sprintf "Server=%s;Database=%s;Uid=%s;Pwd=%s" this.Hostname this.DatabasePrivate this.User this.PasswordPrivate
