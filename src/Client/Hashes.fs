module Hashes

open Fable.Core
open Fable.Core.JsInterop

let md5 (x : string) : string = importDefault "md5"
