namespace Shared

module DefaultHelpers =
    let defaultValue (def : 'a) (value : 'a) =
        if value = Unchecked.defaultof<'a> then def else value

    let defaultEnv (env : string) (def : string) (value : string) =
#if FABLE_COMPILER
        // Javascript can't access environment variables
        defaultValue def value
#else
        if isNull value then
            let result = System.Environment.GetEnvironmentVariable env
            if System.String.IsNullOrEmpty result then def else result
        else
            value
#endif

    let defaultEnvParsed (parse : string -> 'a) (env : string) (def : 'a) (value : 'a) =
#if FABLE_COMPILER
        defaultValue def value
#else
        if value = Unchecked.defaultof<'a> then
            let result = System.Environment.GetEnvironmentVariable env
            if System.String.IsNullOrEmpty result then def else parse result
        else
            value
#endif
