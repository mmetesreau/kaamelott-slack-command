#r "System.Net.Http"
#r "Newtonsoft.Json"

open System
open System.Text
open System.Net
open System.Net.Http
open Newtonsoft.Json
open FSharp.Data
open Flurl
open Flurl.Http

let unescapeString (str: string) =
    Uri.UnescapeDataString(str)

let getSoundFilepath (file: string) =
    Path.Combine(__SOURCE_DIRECTORY__, "sounds", file)

let (|SoundFile|_|) (str: string) = 
    if str.Contains(".mp3") 
    then Some str
    else None 

type SlackActionRequest = {
    Action: string
    ChannelId: string
    User: string
    VerificationToken: string
} 

type SlackActionResponse = {
    delete_original: bool
}

let createActionResponse () =
    { 
        delete_original = true 
    } 

let postFileInSlackChannel (accessToken: string) (channel: string) (comment: string) (filepath: string) (log: TraceWriter)  = 
    async {
        use file = new FileStream(filepath,FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        let response = 
            "https://slack.com/api/files.upload"
                .SetQueryParam("token", accessToken)
                .SetQueryParam("channels", channel)
                .SetQueryParam("initial_comment", comment)
                .PostMultipartAsync(fun content -> content.AddFile("file",  file, filepath) |> ignore)
                .Result

        let result = response.Content.ReadAsStringAsync().Result
        log.Info("file upload result: " + result)
        ()
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
        let verificationToken = Environment.GetEnvironmentVariable("VERIFICATIONTOKEN", EnvironmentVariableTarget.Process)
        let accessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN", EnvironmentVariableTarget.Process)

        let! formData = getFormData(req)

        let payload = formData.["payload"] |> unescapeString
        let parsedPayload = payload |> JsonProvider<"""{ "actions": [ { "name": "name", "type": "type", "value": "value" } ], "channel": { "id": "id", "name": "name" }, "user": { "id": "userid", "name": "name" }, "token":"token" }""">.Parse

        let request =
                { 
                    Action = parsedPayload.Actions.[0].Value 
                    ChannelId = parsedPayload.Channel.Id 
                    VerificationToken = parsedPayload.Token
                    User = sprintf "<@%s|%s>" parsedPayload.User.Id parsedPayload.User.Name
                }

        if verificationToken <> request.VerificationToken then
            return req.CreateResponse(HttpStatusCode.BadRequest, "Verification token is missing")
        else
            match request.Action with
            | SoundFile filename ->
                let filepath = getSoundFilepath filename
                Async.Start <| postFileInSlackChannel accessToken request.ChannelId request.User filepath log
            | _ -> ()

            let json = createActionResponse() |> toJson 
        
            return req.CreateResponse(HttpStatusCode.OK, Content = StringContent(json, Encoding.UTF8, "application/json"))
    
    } |> Async.RunSynchronously
