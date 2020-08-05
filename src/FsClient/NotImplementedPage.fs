module NotImplementedPage

open Browser
open Elmish
open Fable.React
open Fable.React.Props
open Fulma

open Shared

type Msg =
    | ReturnToWhenceYouCame

type Model = { nothing : unit }

let init() = { nothing = () }, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | ReturnToWhenceYouCame ->
        currentModel, [fun _ -> history.go -1]

let view (model : Model) (dispatch : Msg -> unit) =
    Hero.hero
        [ Hero.Color IsSuccess
          Hero.CustomClass "login"
          Hero.IsFullHeight ]
        [ Hero.body [ ]
            [ Container.container
                [ Container.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                [ Column.column
                      [ Column.Width (Screen.All, Column.Is4)
                        Column.Offset (Screen.All, Column.Is4) ]
                      [ Heading.h3
                          [ Heading.Modifiers [ Modifier.TextColor IsGrey ] ]
                          [ str "Not implemented" ]
                        Heading.p
                          [ Heading.Modifiers [ Modifier.TextColor IsGrey ] ]
                          [ str "Sorry, this feature is not yet implemented." ]
                        Button.button
                          [ Button.IsFullWidth
                            Button.Color IsInfo
                            Button.Props [ OnClick (fun _ -> dispatch ReturnToWhenceYouCame) ] ]
                          [ str "Go back" ] ] ] ] ]
