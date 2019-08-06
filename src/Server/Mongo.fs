module Mongo

open MongoDB.FSharp
open MongoDB.Driver

let client = MongoClient("mongodb://localhost/test").GetDatabase("test")
Serializers.Register()

let getCollectionNames dbName =
    let db = client.Client.GetDatabase(dbName)
    let cursor = db.ListCollectionNames()
    cursor.ToListAsync()
