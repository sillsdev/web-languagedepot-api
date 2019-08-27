namespace Shared

[<AutoOpen>]
module SettingsHelper =
    open Microsoft.Extensions.Configuration
    open Shared.Settings

    let buildConfig (c : IConfiguration) =
        let section = c.GetSection "Audio"
        section.Get<AudioSettings>().FixDefault()

