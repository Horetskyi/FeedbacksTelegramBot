module FeedbacksTelegramBot.DomainService

open DomainTypes
open Telegram.Bot
open Telegram.Bot.Args
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums

type private BotMessage =
    | TextMessage of string * BotChatId
    | UnknownMessage

let private subscribe state chatId =
    let isAlreadySubscribed = state.Subscribers |> Set.contains chatId
    if not(isAlreadySubscribed) then
        let newState = { Subscribers = state.Subscribers |> Set.add chatId }
        (newState, Subscribed chatId)
    else
        (state, AlreadySubscribed chatId)

let private unsubscribe state chatId =
    let isSubscribed = state.Subscribers |> Set.contains chatId
    if isSubscribed then
        let newState = { Subscribers = state.Subscribers |> Set.remove chatId }
        (newState, Unsubscribed chatId)
    else
        (state, AlreadyUnsubscribed chatId)
        
let private contains: string -> string -> bool = fun text source -> source.Contains(text) 
        
let private executeBotAction state action chatId =
    match action with
    | Some Subscribe -> subscribe state chatId
    | Some Unsubscribe -> unsubscribe state chatId
    | _ -> state, Nothing
        
let private convertMessageTextToAction text adminPassword =
    match text with
    | text when text |> contains ("/subscribe " + adminPassword) -> Some Subscribe
    | text when text |> contains ("/unsubscribe " + adminPassword) -> Some Unsubscribe
    | _ -> None
    
let private parseBotMessage (messageArgs: MessageEventArgs) =
    match messageArgs.Message with
    | message when
        message <> null &&
        message.Type = MessageType.Text &&
        message.Text <> null
        -> TextMessage (message.Text, message.Chat.Id)
    | _ -> UnknownMessage

let public handleTelegramMessage state adminPassword messageArgs =
    match parseBotMessage messageArgs with
    | TextMessage (messageText, chatId) ->  
        let action = convertMessageTextToAction messageText adminPassword
        executeBotAction state action chatId
    | UnknownMessage -> (state, Nothing)

let public sendTelegramFeedback (subscribers: BotChatId Set, botClient: TelegramBotClient, feedback: string) =
    for chatId in subscribers do 
        botClient.SendTextMessageAsync(ChatId(chatId), feedback).GetAwaiter().GetResult()
        |> ignore
