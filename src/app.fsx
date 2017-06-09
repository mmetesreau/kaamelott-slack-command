#r "../packages/Suave/lib/net40/Suave.dll"

open System

open Suave
open Suave.Filters
open Suave.Operators

let getText (ctx: HttpContext) = 
    match ctx.request.formData "text" with
    | Choice1Of2 value -> Some value
    | _ -> None

let port = 8080

let commandHandler(ctx: HttpContext) =
    let response = 
        match getText ctx with
        | Some text ->  sprintf "valid command with %s" text
        | _ -> "invalid command"
    Successful.OK response ctx    

let app = 
    choose [
        GET >=>  path "/" >=> Successful.OK "hello world"
        POST >=> path "/command" >=> commandHandler
    ]

startWebServer { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" port ] } app

