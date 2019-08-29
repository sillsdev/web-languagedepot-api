module GuessProjectType

open Shared

let guessesFromName =
    [
        "flex", Flex
        "wesay", Lift
        "dictionary", Lift
        "ourword", OurWord
        "story", OneStory
        "ose", OneStory
        "translation", OurWord
        "test", Test
        "adapt", AdaptIt
        "gial", School
        "gillbt", School
        "pyu", School
        "ausil", School
        "tcnn", School
        "payap", School
    ]

let regexForGuessesFromName =
    guessesFromName
    |> List.map (fun (guess, _) -> sprintf @"\b(%s)\b" guess)
    |> String.concat "|"
    |> System.Text.RegularExpressions.Regex

let guessTypeFromId (identifier : string) =
    let idParts = identifier.Split '-'
    // TODO: Code below is original logic, but I think a project ID starting with "Test" should trump the logic below... at least when it comes to identifying test projects
    if idParts.Length > 0 then
        match Array.last idParts with
        | "wesay"
        | "lift"
        | "dictionary" ->
            Lift
        | "lex"
        | "flex" ->
            Flex
        | "test"
        | "demo" ->
            Test
        | "adapt" ->
            AdaptIt
        | "gial"
        | "training"
        | "practise" ->
            School
        | _ ->
            match idParts.[0] with
            | "pyu"
            | "ltl" ->
                School
            | "snwmtn"
            | "waves"
            | "tides" ->
                OneStory
            | _ -> Unknown
    else
        Unknown

let guessTypeFromNameOrDescription (nameOrDesc : string) =
    let lower = nameOrDesc.ToLowerInvariant()
    let m = regexForGuessesFromName.Match lower
    if m.Success then
        let foundText = m.Value
        guessesFromName
        |> List.tryPick (fun (guess, typ) -> if guess = foundText then Some typ else None)
        |> Option.defaultValue Unknown
    else
        Unknown

let guessType (identifier : string option) (name : string) (description : string option) =
    let guessFromId = guessTypeFromId (defaultArg identifier "")
    if guessFromId <> Unknown then
        // printfn "Guessed %A from id %s" guessFromId (defaultArg identifier "(no id)")
        guessFromId
    else
        let guessFromName = guessTypeFromNameOrDescription name
        if guessFromName <> Unknown then
            // printfn "Guessed %A from name %s" guessFromName name
            guessFromName
        else
            // printfn "Guessed %A from description %s" (guessTypeFromNameOrDescription (defaultArg description "")) (defaultArg description "")
            guessTypeFromNameOrDescription (defaultArg description "")
