namespace Shared

module Settings =
    let defaultValue (def : 'a) (value : 'a) =
        if value = Unchecked.defaultof<'a> then def else value

    [<CLIMutable>]
    type AudioSettings = {
        FfmpegPath : string
        Development : string
    } with
        member this.FixDefault() =
            { this with
                FfmpegPath = this.FfmpegPath |> defaultValue "default path"
                Development = this.Development |> defaultValue "default dev" }
