module FeedbacksTelegramBot.DomainTypes

type BotChatId = int64

type BotAction =
    | Subscribe
    | Unsubscribe
    
type BotActionResult =
    | Subscribed of BotChatId
    | Unsubscribed of BotChatId
    | AlreadySubscribed of BotChatId
    | AlreadyUnsubscribed of BotChatId
    | Nothing
    
type State = {
    Subscribers: BotChatId Set
}

type Changes = State * BotActionResult