#r "../packages/Suave/lib/net40/Suave.dll"
#r "../packages/Newtonsoft.Json/lib/net40/Newtonsoft.Json.dll"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"

open System

open Suave
open Suave.Filters
open Suave.Operators

open Newtonsoft.Json
open Newtonsoft.Json.Serialization

open FSharp.Data

[<AutoOpen>]
module Kaamelott =
    type Sounds = JsonProvider<"""./sounds/sounds.json""">

    let sounds = Sounds.GetSamples() |> List.ofArray

    let searchSounds (search: string) (limit: int) =
        let foundSounds = 
            if String.IsNullOrEmpty(search) then
                sounds
            else
                match sounds |> List.filter (fun x -> x.Title.IndexOf(search, StringComparison.OrdinalIgnoreCase) <> -1) with
                | [] -> sounds
                | filteredSounds -> filteredSounds
        
        foundSounds |> List.take (min foundSounds.Length limit) 
     
    let getSoundFromFile (file: string) =
        sounds |> List.tryFind (fun x -> x.File = file)

[<AutoOpen>]
module Helpers =
    let fromJson<'a> json =
        JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a

    let getResourceFromReq<'a> (req : HttpRequest) =
        let getString rawForm =
            System.Text.Encoding.UTF8.GetString(rawForm)
        req.rawForm |> getString |> fromJson<'a>

    let toJson (value: obj) = 
      let jsonSerializerSettings = JsonSerializerSettings()
      jsonSerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
      JsonConvert.SerializeObject(value, jsonSerializerSettings)
      |> Successful.OK
      >=> Writers.setMimeType "application/json; charset=utf-8"

[<AutoOpen>]
module Handlers =
    type SlackCommandRequest = {
        text: string
    }

    type SlackCommandResponse = {
        text: string
        attachments: Attachment list
    }
    and Attachment = {
        text: string
        actions: SendButton list
    }
        with
            member this.fallback = "Shame... buttons aren't supported in this land"
            member this.callback_id = "kaamelott"
            member this.color = "#3AA3E3"
            member this.attachment_type = "default"
    and SendButton = {
        value: string
    } with 
        member this.``type`` = "button"
        member this.text = "Send"
        member this.name = "Send"

    type SlackActionRequest = {
        actions: Action list
    }
    and Action = {
        name: string
        value: string
    }

    type SlackActionResponse = {
        text: string
        replace_original: bool
    }

    let homeHandler (ctx: HttpContext) = 
        Successful.OK "Arthur ! Cuillère..." ctx

    let commandHandler (ctx: HttpContext) =
        let resource = getResourceFromReq<SlackCommandRequest> ctx.request
        let attachments = 
            searchSounds resource.text 5
            |> List.map (fun sound -> 
                { 
                    text = sound.Title
                    actions = 
                        [ 
                            { value = sound.File } 
                        ] 
                })
        toJson {   
                text = "Kaamelott soundtracks" 
                attachments = attachments
               }
               ctx  

    let actionHandler (ctx: HttpContext) =
        let resource = getResourceFromReq<SlackActionRequest> ctx.request
        toJson { 
                text = sprintf "action %s" resource.actions.[0].value
                replace_original = false
               } 
               ctx   

let app = 
    choose [
        GET >=>  path "/" >=> homeHandler
        POST >=> path "/command" >=> commandHandler
        POST >=> path "/action" >=> actionHandler
    ]

let port = 8080

startWebServer { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" port ] } app

