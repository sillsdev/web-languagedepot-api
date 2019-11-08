module PasswordHashing

open System

let createSalt (guid : Guid) =
    guid.ToString().Replace("-", "").ToLowerInvariant()  // TODO: Look up format codes for GUIDs without hyphens since there's probably a slightly faster way to do this

let sha1 (s : string) =
    let utf8 = Text.UTF8Encoding(false)
    let bytes = utf8.GetBytes(s)
    let sha1 = Security.Cryptography.SHA1.Create()
    let hashBytes = sha1.ComputeHash(bytes)
    let result = Text.StringBuilder(hashBytes.Length * 2)
    hashBytes |> Array.iter (fun byte -> result.Append(byte.ToString("x2")) |> ignore)
    result.ToString()

let hashPassword (salt : string) (password : string) =
    let firstHash = sha1 password
    if String.IsNullOrEmpty salt then firstHash else sha1 (salt + firstHash)
