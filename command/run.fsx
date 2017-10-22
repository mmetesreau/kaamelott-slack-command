#r "System.Net.Http"
#r "Newtonsoft.Json"

open System
open System.Text
open System.Net
open System.Net.Http
open Newtonsoft.Json
open FSharp.Data

type Sound = {
    Filename: string
    Description: string
}

let shuffleSounds (sounds: Sound list) (limit: int) = 
    let rnd = System.Random()
    Seq.initInfinite (fun _ -> rnd.Next(0,  sounds.Length - 1))
        |> Seq.take limit
        |> List.ofSeq
        |> List.map (fun index -> List.item index sounds)

let soundDescriptionContains (text: string) (sound: Sound) =
    sound.Description.IndexOf(text, StringComparison.OrdinalIgnoreCase) <> -1

let searchSoundsByDescription (sounds: Sound list) (search: string) (limit: int) = 
    let filteredSounds = 
        sounds 
        |> List.filter (soundDescriptionContains search)
    
    filteredSounds |> List.take (min limit filteredSounds.Length)

let searchSounds (sounds: Sound list) (search: string) (limit: int) =
    match searchSoundsByDescription sounds search limit with
    | [] -> shuffleSounds sounds limit
    | filteredSounds -> filteredSounds

type SlackCommandRequest = {
    Text: string
    VerificationToken: string
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

let getFormData(req: HttpRequestMessage) = 
    async {
        let! content = Async.AwaitTask <| req.Content.ReadAsStringAsync() 
        let namevalues = content.Split('&') |> Seq.map (fun nameValue -> nameValue.Split('=').[0],nameValue.Split('=').[1] )
        return Map.ofSeq namevalues
    }

let toJson (value: obj) = 
    let jsonSerializerSettings = JsonSerializerSettings()
    JsonConvert.SerializeObject(value, jsonSerializerSettings)

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {
        let filepath = Path.Combine(__SOURCE_DIRECTORY__, "sounds.json")

        let sounds = 
                JsonProvider<"""[{"character": "Arthur - Le Roi Burgonde","episode": "Livre II, 03 - Le Dialogue de Paix","file": "interprete.mp3","title": "Interprète"}]""">.Load(filepath)
                    |> Seq.map (fun sound -> { Filename = sound.File; Description = sound.Title })
                    |> List.ofSeq
        
        let verificationToken = Environment.GetEnvironmentVariable("VERIFICATIONTOKEN", EnvironmentVariableTarget.Process)
        
        let! formData = getFormData(req)

        let request = { Text = formData.["text"] ; VerificationToken = formData.["token"] }

        if verificationToken <> request.VerificationToken then
            return req.CreateResponse(HttpStatusCode.BadRequest, "Verification token is missing")
        else
            let soundsAttachments = 
                searchSounds sounds request.Text 5
                |> List.map (fun sound -> toDefaultAttachment sound.Description sound.Filename)

            let json = createCommandResponseWithCancel soundsAttachments |> toJson 
        
            return req.CreateResponse(HttpStatusCode.OK, Content = StringContent(json, Encoding.UTF8, "application/json"))
    
    } |> Async.RunSynchronously
