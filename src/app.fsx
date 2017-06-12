#r "../packages/Suave/lib/net40/Suave.dll"
#r "../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "../packages/Flurl/lib/netstandard1.4/Flurl.dll"
#r "../packages/Flurl.Http/lib/net45/Flurl.Http.dll"
#r "System.Net.Http.dll"

open System
open System.IO
open System.Net.Http

open Suave
open Suave.Filters
open Suave.Operators

open Newtonsoft.Json
open Newtonsoft.Json.Serialization

open Flurl
open Flurl.Http

open FSharp.Data

let port = 8080
let token = Environment.GetEnvironmentVariable("TOKEN")

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
     
    let getSoundPathfile (file: string) =
        Path.Combine(__SOURCE_DIRECTORY__, "sounds", file)

[<AutoOpen>]
module Helpers =
    let getFromFormData (key: string) (ctx : HttpContext) =
        match ctx.request.formData key with
        | Choice1Of2 x  -> x
        | _             -> ""

    let toJson (value: obj) = 
        let jsonSerializerSettings = JsonSerializerSettings()
        JsonConvert.SerializeObject(value, jsonSerializerSettings)
        |> Successful.OK
        >=> Writers.setMimeType "application/json; charset=utf-8"

    let postFile (token: string) (channel: string) (filepath: string) =
        "https://slack.com/api/files.upload"
           .SetQueryParam("token", token)
           .SetQueryParam("channels", channel)
           .PostMultipartAsync(fun content -> content.AddFile("file",  new FileStream(filepath,FileMode.Open), filepath) |> ignore);

[<AutoOpen>]
module Handlers =
    type SlackCommandRequest = {
        text: string
    }
    with 
        static member FromHttpContext (ctx : HttpContext) =
            {
                text = getFromFormData "text" ctx
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
        filename: string
        channelid: string
    }
    with
        static member FromHttpContext (ctx : HttpContext) =
            let parsedPayload = 
                Uri.UnescapeDataString(getFromFormData "payload" ctx)
                |> JsonProvider<"""{"actions":[{"name":"Send","type":"button","value":"file.mp3"}], "channel":{"id":"id","name":"name"}}""">.Parse
            { filename = parsedPayload.Actions.[0].Value; channelid = parsedPayload.Channel.Id }

    type SlackActionResponse = {
        delete_original: bool
    }

    let homeHandler (ctx: HttpContext) = 
        Successful.OK "Arthur !" ctx

    let commandHandler (ctx: HttpContext) =
        let resource = SlackCommandRequest.FromHttpContext ctx
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
        let resource = SlackActionRequest.FromHttpContext ctx
        let pathfile = getSoundPathfile resource.filename
        postFile token resource.channelid pathfile |> ignore
        toJson { 
                delete_original = true
               } 
               ctx   

let app = 
    choose [
        GET >=>  path "/" >=> homeHandler
        POST >=> path "/command" >=> commandHandler
        POST >=> path "/action" >=> actionHandler
    ]

startWebServer { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" port ] } app
