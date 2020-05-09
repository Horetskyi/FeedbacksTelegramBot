module FeedbacksTelegramBot.API 
    
open System.Net
open FeedbacksTelegramBot
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open FSharp.Data
open Serialization
open Telegram.Bot
open Telegram.Bot.Types.Enums

type Configs = JsonProvider<"app.settings.json">
type SendFeedbackBody = { Feedback: string }

[<EntryPoint>]
let main argv =
    
    // Configs
    let configs = Configs.GetSample()

    // IIS integration (optional)
    let path st = Suave.IIS.Filters.path argv st
    
    // Mongo
    let mongoStorage = Storage.MongoStorage(configs.MongoConnectionString, configs.MongoDbName)
    
    // Loading
    let mutable state = mongoStorage.ReadState()
    
    // Telegram message handler
    let handleTelegramMessage = fun args ->
        let changes = DomainService.handleTelegramMessage state configs.AdminPassword args
        let updatedState = mongoStorage.ApplyChanges changes
        if updatedState.IsSome then
            state <- updatedState.Value
            printfn "ChangesApplied %A" changes
        ()
        
    // TelegramBot    
    let botClient = TelegramBotClient(configs.BotToken)
    botClient.OnMessage.Add(handleTelegramMessage)
    botClient.StartReceiving(System.Array.Empty<UpdateType>())
    
    // Feedback request handler
    let handleSendFeedback body =
        DomainService.sendTelegramFeedback(state.Subscribers, botClient, body.Feedback)
        printfn "FeedbackSent %A" body
        OK "Feedback sent"
        
    // Routing
    let routing =
        choose [
            GET >=> choose [
                path "/status" >=> OK "Live"
            ]
            POST >=> choose [
                path "/send/feedback" >=> request (getPostBody<SendFeedbackBody> >> handleSendFeedback)
            ]
        ]

    // Run
    let config = { defaultConfig with bindings=[HttpBinding.create HTTP IPAddress.Any 8083us]; }
                 |> Suave.IIS.Configuration.withPort argv
    startWebServer config routing
    0