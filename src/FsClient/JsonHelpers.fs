module JsonHelpers

open Fable.Core
open Shared
open Browser

let success data = { ok = true; data = data; message = "" }
let failure failData msg = { ok = false; data = failData; message = msg }

let toResult (jsonResult : JsonResult<'a>) =
    match jsonResult with
    | { ok = true; data = data } -> Ok data
    | { ok = false; message = msg } -> Error msg

let unpackJsonResult currentModel jsonResult fn =
        match toResult jsonResult with
        | Ok newData ->
            fn newData
        | Error msg ->
            currentModel, Notifications.notifyError msg

// TODO: Consider whether the version below with separate ofSuccess and ofError is worth restoring, or whether we should just delete it
(*
let handleResult ofSuccess ofError (model,jsonResult) =
    match (toResult jsonResult) with
    | Ok data -> ofSuccess data
    | Error msg -> model, ofError msg

let handleSuccess ofSuccess jsonResult =
    jsonResult
    |> handleResult ofSuccess Notifications.notifyError
*)
