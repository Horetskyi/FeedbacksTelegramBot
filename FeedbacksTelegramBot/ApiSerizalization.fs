module FeedbacksTelegramBot.Serialization

open Newtonsoft.Json
open Suave

let fromJson<'a> json = JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a

let getPostBody<'a> (req : HttpRequest) =
    let getString (rawForm: byte[]) =
        System.Text.Encoding.UTF8.GetString(rawForm)
    req.rawForm |> getString |> fromJson<'a>