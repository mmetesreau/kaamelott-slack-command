#r "../packages/Suave/lib/net40/Suave.dll"
#r "../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "../packages/Flurl/lib/netstandard1.4/Flurl.dll"
#r "../packages/Flurl.Http/lib/net45/Flurl.Http.dll"
#r "System.Net.Http.dll"

open System
open System.IO

open Suave
open Suave.Filters
open Suave.Operators

open Newtonsoft.Json
open Newtonsoft.Json.Serialization

open Flurl
open Flurl.Http

open FSharp.Data

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
        
    let unescapeString (str: string) =
        Uri.UnescapeDataString(str)

    let (|SoundFile|_|) (str: string) = 
        if str.Contains(".mp3") 
        then Some str
        else None 

[<AutoOpen>]
module Kaamelott =

    type Sound = {
        Filename: string
        Description: string
    }

    let private sounds = 
        JsonProvider<"./sounds/sounds.json">.Load("./sounds/sounds.json")
            |> Seq.map (fun sound -> { Filename = sound.File; Description = sound.Title })
            |> List.ofSeq
    
    let private shuffleSounds (sounds: Sound list) (limit: int) = 
        let rnd = System.Random()
        Seq.initInfinite (fun _ -> rnd.Next(0,  sounds.Length - 1))
            |> Seq.take limit
            |> List.ofSeq
            |> List.map (fun index -> List.item index sounds)

    let private soundDescriptionContains (text: string) (sound: Sound) =
        sound.Description.IndexOf(text, StringComparison.OrdinalIgnoreCase) <> -1
    
    let private searchSoundsByDescription (sounds: Sound list) (search: string) (limit: int) = 
        sounds 
            |> List.filter (soundDescriptionContains search)
            |> List.take limit

    let searchSounds (search: string) (limit: int) =
        match searchSoundsByDescription sounds search limit with
        | [] -> shuffleSounds sounds limit
        | filteredSounds -> filteredSounds

    let getSoundFilepath (file: string) =
        Path.Combine(__SOURCE_DIRECTORY__, "sounds", file)

[<AutoOpen>]
module Slack = 

    type SlackCommandRequest = {
        Text: string
        VerificationToken: string
    } with 
        static member FromHttpContext (ctx: HttpContext) =
            { 
                Text = getFromFormData "text" ctx 
                VerificationToken = getFromFormData "token" ctx 
            }

    type SlackActionRequest = {
        Action: string
        ChannelId: string
        User: string
        VerificationToken: string
    } with
        static member FromHttpContext (ctx: HttpContext) =
            let payload = getFromFormData "payload" ctx |> unescapeString
            let parsedPayload = payload |> JsonProvider<"slackActionRequest.json">.Parse
            { 
                Action = parsedPayload.Actions.[0].Value 
                ChannelId = parsedPayload.Channel.Id 
                VerificationToken = parsedPayload.Token
                User = sprintf "<@%s|%s>" parsedPayload.User.Id parsedPayload.User.Name
            }

    type SlackActionResponse = {
        delete_original: bool
    }

    type SlackCommandResponse = {
        text: string
        attachments: Attachment list
    }
    and Attachment = {
        text: string
        color: string
        fallback: string
        attachment_type: string
        callback_id: string
        actions: Action list
    }
    and Action = {
        ``type``: string
        text: string
        name: string
        value: string
    } 

    let toDefaultAttachment (text: string) (buttonValue: string) = 
        { 
            text = text
            color = "#3AA3E3"
            fallback = "Shame... buttons aren't supported in this land"
            attachment_type = "default"
            callback_id = "kaamelott"
            actions = 
                [ 
                    { 
                        ``type`` = "button"
                        text = "Send"
                        name = "Send"
                        value = buttonValue
                    }
                ] 
        }
    
    let createCommandResponseWithCancel (attachments: Attachment list) = 
        let cancelAttachment = toDefaultAttachment "Cancel" "cancel"
        {
            text = "Kaamelott soundtracks"
            attachments = attachments @ [ cancelAttachment ] 
        }
    
    let createActionResponse () =
        { 
            delete_original = true 
        }  
        
    let postFileInSlackChannel (accessToken: string) (channel: string) (comment: string) (filepath: string) = async {
        use file = new FileStream(filepath,FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        let response = 
            "https://slack.com/api/files.upload"
               .SetQueryParam("token", accessToken)
               .SetQueryParam("channels", channel)
               .SetQueryParam("initial_comment", comment)
               .PostMultipartAsync(fun content -> content.AddFile("file",  file, filepath) |> ignore)
               .Result

        let result = response.Content.ReadAsStringAsync().Result
        printfn "file upload result: %s" result
        ()
    }

[<AutoOpen>]
module Handlers =
    let homeHandler (ctx: HttpContext) = 
        Successful.OK "Arthur !" ctx

    let commandHandler (verificationToken: string) (ctx: HttpContext) =
        let request = SlackCommandRequest.FromHttpContext ctx
        
        if verificationToken <> request.VerificationToken then
            RequestErrors.FORBIDDEN "access forbidden" ctx
        else
            let soundsAttachments = 
                searchSounds request.Text 5
                |> List.map (fun sound -> toDefaultAttachment sound.Description sound.Filename)

            createCommandResponseWithCancel soundsAttachments |> toJson <| ctx

    let actionHandler (verificationToken: string) (accessToken: string) (ctx: HttpContext) =
        let request = SlackActionRequest.FromHttpContext ctx

        if verificationToken <> request.VerificationToken then
            RequestErrors.FORBIDDEN "access forbidden" ctx
        else
            match request.Action with
            | SoundFile filename ->
                let filepath = getSoundFilepath filename
                Async.Start <| postFileInSlackChannel accessToken request.ChannelId request.User filepath
            | _ -> ()

            createActionResponse() |> toJson <| ctx

let port = 8080
let accessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN")
let verificationToken = Environment.GetEnvironmentVariable("VERIFICATIONTOKEN")

let app = 
    choose [
        GET >=>  path "/" >=> homeHandler
        POST >=> path "/command" >=> commandHandler verificationToken
        POST >=> path "/action" >=> actionHandler verificationToken accessToken
    ]

startWebServer { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" port ] } app
