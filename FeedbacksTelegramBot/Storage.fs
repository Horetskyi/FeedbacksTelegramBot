module FeedbacksTelegramBot.Storage

open MongoDB.Bson.Serialization.Attributes
open MongoDB.Driver
open DomainTypes

type MongoSubscriberDocument = {
    [<BsonId>]
    ChatId: int64
}

type MongoStorage(connectionString: string, dbName: string) =
    
    member this.Client = MongoClient(connectionString)
    
    member this.Database = this.Client.GetDatabase(dbName)
    
    member this.SubscribersCollection = this.Database.GetCollection<MongoSubscriberDocument>("Subscribers")
    
    member this.ReadState () =
        let subscriberDocuments = this.SubscribersCollection.AsQueryable().ToList()
        let subscribers = subscriberDocuments |> Seq.map<_, BotChatId> (fun x -> x.ChatId) |> Set.ofSeq
        { Subscribers = subscribers }
        
    member this.ApplyChanges (updatedState: State, actionResult: BotActionResult): Option<State> =
        match actionResult with
        
        | Subscribed chatId ->
            try
                this.SubscribersCollection.InsertOne({ ChatId = chatId })
                Some updatedState
            with
            | _ -> None
            
        | Unsubscribed chatId ->
            try
                let deleteResult = this.SubscribersCollection.DeleteOne(fun x -> x.ChatId = chatId)
                if (deleteResult.DeletedCount = 1L) then
                    Some updatedState
                else
                    None
            with
            | _ -> None
            
        | AlreadySubscribed _ -> None
        | AlreadyUnsubscribed _ -> None
        | Nothing -> None
