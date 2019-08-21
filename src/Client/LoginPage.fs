module LoginPage

open Elmish
open Fable.React
open Fable.React.Props
open Fulma

open Shared

type Msg =
    | LoginInputChanged of string

type Model = { LoginInput : string }

let init rootModel = { LoginInput = "" }, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | LoginInputChanged username ->
        let nextModel = { currentModel with LoginInput = username }
        nextModel, Cmd.none

let LoginView = FunctionComponent.Of (fun (props : {|model : Model; dispatch : Msg -> unit|}) ->
    let currentUser, setUser = Hooks.useContext RootPage.userCtx

    Column.column
        [ Column.Width (Screen.All, Column.Is4)
          Column.Offset (Screen.All, Column.Is4) ]
        [ Heading.h3
            [ Heading.Modifiers [ Modifier.TextColor IsGrey ] ]
            [ str "Login" ]
          Heading.p
            [ Heading.Modifiers [ Modifier.TextColor IsGrey ] ]
            [ str "Please login to proceed." ]
          Box.box' [ ]
            [ figure [ Class "avatar" ]
                [ img [ Src "https://placehold.it/128x128" ] ]
              form [ OnSubmit (fun _ -> setUser (Some { Name = props.model.LoginInput.Split('@').[0]; Email = props.model.LoginInput })) ]
                [ Field.div [ ]
                    [ Control.div [ ]
                        [ Input.email
                            [ Input.Size IsLarge
                              Input.Placeholder "Your Email"
                              Input.Props
                                [ OnChange (fun ev -> ev.Value |> LoginInputChanged |> props.dispatch)
                                  AutoFocus true ] ] ] ]
                  Field.div [ ]
                    [ Control.div [ ]
                        [ Input.password
                            [ Input.Size IsLarge
                              Input.Disabled true
                              Input.Placeholder "No password needed for demo" ] ] ]
                  Field.div [ ]
                    [ Checkbox.checkbox [ ]
                        [ input [ Type "checkbox" ]
                          str "Remember me" ] ]
                  Button.button
                    [ Button.Color IsInfo
                      Button.IsFullWidth
                      Button.CustomClass "is-large is-block" ]
                    [ str "Login" ] ] ]
          Text.p [ Modifiers [ Modifier.TextColor IsGrey ] ]
            [ a [ ] [ str "Sign Up" ]
              str "\u00A0·\u00A0"
              a [ ] [ str "Forgot Password" ]
              str "\u00A0·\u00A0"
              a [ ] [ str "Need Help?" ] ]
          br [ ] ]

    // div [ ]
    //     [ input [ OnChange (fun ev -> ev.Value |> LoginInputChanged |> props.dispatch) ]
    //       button [ OnClick (fun _ -> setUser (Some { Name = props.model.LoginInput; Email = "rmunn@pobox.com" })) ] [ str "Log in"]
    //       button [ OnClick (fun _ -> setUser None) ] [ str "Log out"] ]
)

let loginView model dispatch = LoginView {| model = model; dispatch = dispatch |}

let view (model : Model) (dispatch : Msg -> unit) =
    Hero.hero
        [ Hero.Color IsSuccess
          Hero.CustomClass "login"
          Hero.IsFullHeight ]
        [ Hero.body [ ]
            [ Container.container
                [ Container.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                [ loginView model dispatch ] ] ]
