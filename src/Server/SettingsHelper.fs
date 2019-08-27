namespace Shared

[<AutoOpen>]
module SettingsHelper =
    open Microsoft.Extensions.Configuration
    open Shared.Settings

    type ServerConfig = Map<string, obj>

    let getSectionName<'settings>() =
        let typ = typeof<'settings>
        let name = typ.Name
        if name.EndsWith "Settings" then
            name.Substring(0, name.Length - 8)
        else
            name

    let getSettingsValue<'settings> (config : IConfiguration) =
        let typ = typeof<'settings>
        let section = config.GetSection (getSectionName<'settings>())
        let settings = section.Get<'settings>()
        let method = typ.GetMethod "SetDefaultValues"
        if not (isNull method) then
            let fixedValue = method.Invoke(settings, [||])
            fixedValue :?> 'settings
        else
            settings

    let addSettings<'settings> (config : IConfiguration) =
        let name = getSectionName<'settings>()
        let settings = getSettingsValue<'settings> config
        Map.add name (box settings)

    let getSettings<'settings> (ctx : Microsoft.AspNetCore.Http.HttpContext) =
        let serverConfig = ctx.Items.["Configuration"] :?> ServerConfig
        let sectionName = getSectionName<'settings>()
        serverConfig |> Map.find sectionName :?> 'settings

    let buildConfig (c : IConfiguration) : ServerConfig =
        Map.empty<string, obj>
        |> addSettings<AudioSettings> c
