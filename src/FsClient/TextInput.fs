module TextInput

open Elmish
open Fable.Core.JsInterop
open Fable.React
open Fulma

type TextInputProps = { placeholder : string; value : string; btn : ReactElement; dispatch : Dispatch<string> }
let TextInputComponent =
    FunctionComponent.Of (fun (props : TextInputProps) ->
        let state = Hooks.useState(props.value)
        Control.div
           [ Control.HasIconRight ]
           [ Field.div
               [ Field.HasAddons ]
               [ Input.text
                   [ Input.Placeholder props.placeholder
                     Input.Value state.current
                     Input.OnChange (fun ev -> state.update (!!ev.target?value : string))
                     Input.Props [ Props.OnKeyPress (fun evt -> if evt.key = "Enter" then props.dispatch state.current) ] ]
                 Button.a
                   [ Button.Color IsInfo
                     Button.OnClick (fun _ -> props.dispatch state.current) ]
                   [ props.btn ] ] ]
    )

let textInputComponent placeholder initialValue btn dispatch =
    TextInputComponent { placeholder = placeholder; value = initialValue; btn = btn; dispatch = dispatch; }
