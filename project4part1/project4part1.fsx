#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"

open System
open Akka
open Akka.Actor
open Akka.FSharp
open System.Data

//Create Actor System and Handle Input
let system = System.create "system" (Configuration.defaultConfig())
let args = fsi.CommandLineArgs
let n = args.[1] |> int
let numTweets = args.[2] |> int
let mutable globalTweetIdTracker = 1

printfn "Starting Twitter Engine number of users = %d and number of requests = %d " n numTweets

let sleep200 = 200
let mutable userActorList = []
let mutable globalReTweetCount = 0
let mutable perfList = []

let hashTagList = ["#Trump"; "#Wimbledon"; "#CMIWorld";"#NYC"; "#London";"#Themes"; "#ThursdayThoughts";
"#Things"; "#coffee"; "#computers"; "#cars";"#golfing";"#cooking"; "#writing";"#socialmedia";"#tech";"#photography";"#election2020";
"#biden";"#blm";"#grammy";"#ItsComingHome";"#influencer";"#influencermarketing";"#fridayfeeling";"#MondayMotivation";"#CoronaVirus"]

let tweetList = ["BTS starts things off";"A political endorsement in Indonesia";"Free jerseys for all";"Stand behind BLM";"One decade of 1D";
"Raising money for Houston";"InMyFeelingsChallenge";"YouTuber giveaway";
"For every retweet this gets, Pedigree will donate one bowl of dog food to dogs in need!";"All the love as always";
"Asking you to believe—not in my ability to create change, but in yours.";"Always in my heart";"So often true";"Diego Maradona";"Covid"]


let userTable = new DataTable()

let dc1 = new DataColumn("userId")
let dc2 = new DataColumn("followers")
let dc3 = new DataColumn("following")
let dc4 = new DataColumn("userStatus")

userTable.Columns.Add(dc1)
userTable.Columns.Add(dc2)
userTable.Columns.Add(dc3)
userTable.Columns.Add(dc4)

let primaryKeyColumns1:DataColumn [] = [|userTable.Columns.["userId"]|]
userTable.PrimaryKey <- primaryKeyColumns1
userTable.AcceptChanges()


let tweetsTable = new DataTable()
let dc11 = new DataColumn("tweetId")
dc11.DataType = System.Type.GetType("System.Int32")
let dc12 = new DataColumn("tweetContent")
tweetsTable.Columns.Add(dc11)
tweetsTable.Columns.Add(dc12)
let primaryKeyColumns2:DataColumn [] = [|tweetsTable.Columns.["tweetId"]|]
tweetsTable.PrimaryKey <- primaryKeyColumns2
tweetsTable.AcceptChanges()

let hashtagsTable = new DataTable()
let dc21 = new DataColumn("hashtag")
let dc22 = new DataColumn("hashTweetId")
dc22.DataType = System.Type.GetType("System.Int32")
hashtagsTable.Columns.Add(dc21)
hashtagsTable.Columns.Add(dc22)
hashtagsTable.AcceptChanges()


let mentionsTable = new DataTable()
let dc31 = new DataColumn("mention")
let dc32 = new DataColumn("menTweetId")
dc32.DataType = System.Type.GetType("System.Int32")
mentionsTable.Columns.Add(dc31)
mentionsTable.Columns.Add(dc32)
mentionsTable.AcceptChanges()


let userTweetMappingTable = new DataTable()
let dc41 = new DataColumn("userId")
let dc42 = new DataColumn("tweetId")
dc42.DataType = System.Type.GetType("System.Int32")
userTweetMappingTable.Columns.Add(dc41)
userTweetMappingTable.Columns.Add(dc42)
userTweetMappingTable.AcceptChanges()


let userOfflineTweetMappingTable = new DataTable()
let dc51 = new DataColumn("userId")
let dc52 = new DataColumn("offTweetId")
dc52.DataType = System.Type.GetType("System.Int32")
userOfflineTweetMappingTable.Columns.Add(dc51)
userOfflineTweetMappingTable.Columns.Add(dc52)
userOfflineTweetMappingTable.AcceptChanges()

let userFollowerMappingTable = new DataTable()
let dc61 = new DataColumn("userId")
let dc62 = new DataColumn("followerId")
userFollowerMappingTable.Columns.Add(dc61)
userFollowerMappingTable.Columns.Add(dc62)
userFollowerMappingTable.AcceptChanges()

let containsStr str list = List.exists (fun elem -> elem.Equals(str)) list

let rand = Random()

let followerZipfDist : int array = Array.zeroCreate n
let mutable idFollowerCountTupList: list<int*int> = []


type MessageTypes = 
    | Exit of string
    | BuildFollowers of string*float
    | Register of string*string
    | RegisterMe of string
    | UpdateFollowing of string
    | SendTweets of int
    | ReceiveTweet of string*string
    | SendRetweets of int
    | Tweet of string*string
    | QueryHashtag of string
    | QueryMentions of string
    | GetFeed of string
    | ProcessHashtagQuery of string*string
    | ProcessMentionQuery of string
    | ProcessFeedQuery of string
    | RecieveQuery of string*string
    | RecieveMentionQuery of string*string
    | RecieveFeedQuery of string*string
    | BackOnline of string
    | FetchOffline of string

let twitterServer (mailbox: Actor<_>)=
    let mutable status = "Online"

    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with

        | Register(userId,activeStr) ->     userTable.Rows.Add(userId, "", "", activeStr) |> ignore
                                            if userTable.Rows.Count = n then
                                                printf "Registration for all users complete\n"
        
        | BuildFollowers(msg,percent) ->
                                            // printf "Building Followers for each user %f\n" percent                                           
                                            printf "Each user initially has random number of followers\n"
                                            printf "Building followers list for tweet simulation...\n"
                                            printf "Since follower assignment is randomized for the simulation, this step may take some time to complete for larger number of users\n"   
                                            for i in 0..n-1 do
                                                let mutable followersList = []
                                                // let mutable j = 1
                                                let idStr = string (i)
                                                let mutable numFollowers = 0

                                                if n > 100 then
                                                    numFollowers <- rand.Next(1,101)
                                                else
                                                    numFollowers <- rand.Next(1,n)

                                                let mutable followerIndex = rand.Next(0,n)
                                                let mutable selfFlag = 0
                                                for j in 1..numFollowers do

                                                    if(followerIndex = i) then
                                                        followerIndex <- followerIndex + 1

                                                    if(followerIndex = n) then
                                                        followerIndex <- 0
  
                                                    let strFollowerIndex = followerIndex |> string
                                                    followersList <- List.append followersList [strFollowerIndex]

                                                    userFollowerMappingTable.Rows.Add(idStr, followersList.[j-1]) |> ignore


                                                    followerIndex <- followerIndex + 1
                                                
                                                let tupleObj = (i,followersList.Length)
                                                idFollowerCountTupList <- List.append idFollowerCountTupList [tupleObj]

                                            printf "Followers Updated \n"

        | Tweet(userId,tweetMsg) -> let curTweetId = globalTweetIdTracker

                                    let ustr = String.Format("userId = '{0}'", userId)
                                    let existingRows:DataRow [] = userFollowerMappingTable.Select(ustr)
                                    let mutable followersList = []

                                    for i in 0..existingRows.Length-1 do
                                        let strFollowerID = existingRows.[i].[1] |> string
                                        followersList <- List.append followersList [strFollowerID]


                                    for i in 0..followersList.Length-2 do
                                        let ind = followersList.[i] |> int
                                        let innerqstr = String.Format("userId = '{0}'", followersList.[i])
                                        let followerRows:DataRow [] = userTable.Select(innerqstr)
                                        let statusStr = followerRows.[0].[3] |> string
                                        if statusStr.Equals("active") then
                                            userActorList.[ind] <! ReceiveTweet(userId,tweetMsg)
                                        else
                                            // printf "\n User id being inserted is %s with tweetID %d \n " userId curTweetId
                                            let offlineUserStr = followerRows.[0].[0] |> string
                                            userOfflineTweetMappingTable.Rows.Add(offlineUserStr, curTweetId) |> ignore 

                                    tweetsTable.Rows.Add( globalTweetIdTracker , tweetMsg) |> ignore
                                    
                                    userTweetMappingTable.Rows.Add(userId, curTweetId) |> ignore
                                    //Threading.Thread.Sleep(sleep200)

                                    let tweetWords = tweetMsg.Split [|' '|]

                                    for i in 0..tweetWords.Length-1 do
                                        if tweetWords.[i].[0] = '@' then
                                            //Threading.Thread.Sleep(sleep200)
                                            mentionsTable.Rows.Add(tweetWords.[i], curTweetId)  |> ignore
                                            //Threading.Thread.Sleep(sleep200)
                                        else if tweetWords.[i].[0] = '#' then
                                                //Threading.Thread.Sleep(sleep200)
                                                hashtagsTable.Rows.Add(tweetWords.[i], curTweetId)  |> ignore
                                                //Threading.Thread.Sleep(sleep200)


                                    globalTweetIdTracker <- globalTweetIdTracker+1
        
        | FetchOffline(userId) ->   printf "\n\nTweets for user %s from when they were offline\n" userId
                                    let qstr = String.Format("userId = '{0}'", userId)
                                    let existingRows:DataRow [] = userOfflineTweetMappingTable.Select(qstr)

                                    if existingRows.Length > 0 then
                                        //printf "existingRows.Length is %d\n" existingRows.Length
                                        let mutable tweetIdList = []
                                        let mutable rowCollection = userOfflineTweetMappingTable.Rows
                                        let mutable userIdTweetIdList: list<string*int> = []
                                        for i in 0..existingRows.Length-1 do
                                            let strTweetId = existingRows.[i].[1] |> string
                                            //printf "Blah ID is %s\n" blah
                                            let intTweetId = strTweetId |> int
                                            // printf "strTweet ID is %s\n" strTweetId
                                            // printf "Before Append list size is %d\n" tweetIdList.Length
                                            tweetIdList <- List.append tweetIdList [intTweetId]
                                            // printf "After Append list size is %d\n" tweetIdList.Length
                                        
                                        // printf "tweetIdList.Length is %d\n" tweetIdList.Length
                                        for i in 0..tweetIdList.Length-1 do
                                            let localQuery = String.Format("tweetId = '{0}'", tweetIdList.[i])
                                            let userIdRows:DataRow [] = userTweetMappingTable.Select(localQuery)
                                            let userIdStr = string userIdRows.[0].[0]
                                            let tupleObj = (userIdStr ,tweetIdList.[i])
                                            userIdTweetIdList <- List.append userIdTweetIdList [tupleObj]  
                                        
                                        for i in 0..userIdTweetIdList.Length-1 do
                                            let uid = fst (userIdTweetIdList.[i])
                                            let tid = snd (userIdTweetIdList.[i])
                                            let qstr = String.Format("tweetId = '{0}'", tid)
                                            let tweetRows:DataRow [] = tweetsTable.Select(qstr)
                                            let tcontent = tweetRows.[0].[1] |> string 
                                            mailbox.Sender() <! RecieveQuery(uid,tcontent)
                                           

        | ProcessHashtagQuery(userId, hashtag) ->
                                                    let ustr = String.Format("hashtag = '{0}'", hashtag)
                                                    let hashtagRows:DataRow [] = hashtagsTable.Select(ustr)
                                                    let mutable userIdTweetIdList: list<string*int> = []

                                                    //printf "Before IF statement hastag value is %s -- ustr is %s\n" hashtag ustr

                                                    if hashtagRows.Length > 0 then
                                                        let mutable tweetIdList = []
                                                        //printf "Before first for\n"
                                                        
                                                        for i in 0..hashtagRows.Length-1 do
                                                            //let tweetId = hashtagRows.[i].[1] |> int
                                                            let strTweetId = hashtagRows.[i].[1] |> string
                                                            //printf "Blah ID is %s\n" blah
                                                            let intTweetId = strTweetId |> int
                                                            //printf "strTweet ID is %d\n" strTweetId
                                                            //printf "Before Append list size is %d\n" tweetIdList.Length
                                                            tweetIdList <- List.append tweetIdList [intTweetId]
                                                            //printf "After Append list size is %d\n" tweetIdList.Length

                                                        

                                                        //printf "Before second for\n"                                                       
                                                        for i in 0..tweetIdList.Length-1 do
                                                            let localQuery = String.Format("tweetId = '{0}'", tweetIdList.[i])
                                                            let userIdRows:DataRow [] = userTweetMappingTable.Select(localQuery)
                                                            let userIdStr = string userIdRows.[0].[0]
                                                            let tupleObj = (userIdStr ,tweetIdList.[i])
                                                            userIdTweetIdList <- List.append userIdTweetIdList [tupleObj]  

                                                        //printf "Before third for \n"
                                                        for i in 0..userIdTweetIdList.Length-1 do
                                                            let uid = fst (userIdTweetIdList.[i])
                                                            let tid = snd (userIdTweetIdList.[i])
                                                            let qstr = String.Format("tweetId = '{0}'", tid)
                                                            let tweetRows:DataRow [] = tweetsTable.Select(qstr)
                                                            let tcontent = tweetRows.[0].[1] |> string 
                                                            mailbox.Sender() <! RecieveQuery(uid,tcontent)
                                                                 
                                    
        
        | ProcessMentionQuery(userId) ->
                                                let ustr = String.Format("mention = '@{0}'", userId)
                                                //printf "%s ustr\n" ustr
                                                let mentionRows:DataRow [] = mentionsTable.Select(ustr)
                                                let mutable userIdTweetIdList: list<string*int> = []
                                                if mentionRows.Length > 0 then
                                                    let mutable tweetIdList = []
                                                    for i in 0..mentionRows.Length-1 do
                                                        let tweetId = mentionRows.[i].[1] |> string
                                                        let intTweetId = tweetId |> int
                                                        tweetIdList <- List.append tweetIdList [intTweetId]
                                                    
                                                    for i in 0..tweetIdList.Length-1 do
                                                        let localQuery = String.Format("tweetId = '{0}'", tweetIdList.[i])
                                                        let userIdRows:DataRow [] = userTweetMappingTable.Select(localQuery)
                                                        let userIdStr = string userIdRows.[0].[0]
                                                        let tupleObj = (userIdStr ,tweetIdList.[i])
                                                        userIdTweetIdList <- List.append userIdTweetIdList [tupleObj]  

                                                    for i in 0..userIdTweetIdList.Length-1 do
                                                        let uid = fst (userIdTweetIdList.[i])
                                                        let tid = snd (userIdTweetIdList.[i])
                                                        let qstr = String.Format("tweetId = '{0}'", tid)
                                                        let tweetRows:DataRow [] = tweetsTable.Select(qstr)
                                                        let tcontent = tweetRows.[0].[1] |> string 
                                                        mailbox.Sender() <! RecieveQuery(uid,tcontent) 

        | ProcessFeedQuery(userId) ->   
                                        let ustr = String.Format("followerId = '{0}'", userId)
                                        //printf "%s ProcessFeedQuery ustr\n" ustr
                                        let followingRows:DataRow [] = userFollowerMappingTable.Select(ustr)
                                        
                                        if followingRows.Length > 0 then                                        
                                            //let followingStr = followingRows.[0].[2] |> string
                                            //let followingList = followingStr.Split [|'|'|]
                                            let mutable followingList = []

                                            for i in 0..followingRows.Length-1 do
                                                let strFollowingID = followingRows.[i].[0] |> string
                                                followingList <- List.append followingList [strFollowingID]

                                            //printf "User id %s -- followingStr %s-- followingList.Length%d\n" userId followingStr followingList.Length
                                            if followingList.Length > 0 then
                                                let mutable userIdTweetIdList: list<string*int> = []
                                                //printf "Before first for\n"
                                                for i in 0..followingList.Length-1 do
                                                    let tstr = String.Format("userId = '{0}'", followingList.[i])
                                                    let userIdStr = followingList.[i]
                                                    //printf "Before second for\n"
                                                    let followingTweetRows:DataRow [] = userTweetMappingTable.Select(tstr)                                                
                                                    for j in 0..followingTweetRows.Length-1 do
                                                        let tidStr = followingTweetRows.[j].[1] |> string
                                                        let intTid = tidStr |> int
                                                        let tupleObj = (userIdStr ,intTid)
                                                        userIdTweetIdList <- List.append userIdTweetIdList [tupleObj] 
                                                
                                                //printf "Before final for\n"
                                                for i in 0..userIdTweetIdList.Length-1 do
                                                        let uid = fst (userIdTweetIdList.[i])
                                                        let tid = snd (userIdTweetIdList.[i])
                                                        let qstr = String.Format("tweetId = '{0}'", tid)
                                                        let tweetRows:DataRow [] = tweetsTable.Select(qstr)
                                                        let tcontent = tweetRows.[0].[1] |> string 
                                                        mailbox.Sender() <! RecieveQuery(uid,tcontent)
                                           
        | _ -> printf "Unknown message Recieved"                
        
        return! loop()
    }   
    loop()

let server = spawn system "twitterServer" twitterServer
               
let userActor (mailbox: Actor<_>)=
    let mutable userId = ""
    let mutable receivedTweetCount = 0
    let mutable localTweetMap:Map<int,string*string> = Map.empty


    let rec loop() = actor {
        let! msg = mailbox.Receive()   
        match msg with 
        
        | Exit msg -> printf "User Logout\n"        
        
        | RegisterMe(id) ->     userId <- id
                                // printf "Registering with server %s\n" id
                                //server <! Register(userId)

        | QueryHashtag hashtag ->   server <! ProcessHashtagQuery(userId, hashtag)
                                  

        | QueryMentions msg ->      server <! ProcessMentionQuery(userId)

        | GetFeed msg ->        server <! ProcessFeedQuery(userId)

        | SendTweets(tweetCount) ->      
                                    let uid = userId |> int
                                    let ustr = String.Format("userId <> '{0}'", uid)
                                    let mentionRows:DataRow [] = userTable.Select(ustr)
                                    let size1 = hashTagList.Length
                                    let size2 = mentionRows.Length

                                    //printf "TweetCount %d for user %d\n" tweetCount uid

                                    for i in 1..tweetCount do
                                        let hashtagRand = rand.Next(0,size1)
                                        let mentionRand = rand.Next(0,size2)
                                        let hashtagStr = hashTagList.[hashtagRand] |> string
                                        let mentionStr = mentionRows.[mentionRand].[0] |> string
                                        let tweetMsg = "This is a demo tweet mentioning "+ "@"+mentionStr + " and containing the hashtag " + hashtagStr
                                        // printf "tweetMsg %s\n" tweetMsg
                                        server <! Tweet(userId,tweetMsg)

        | SendRetweets(retweetCount) -> 
                                    if(localTweetMap.Count > 0) then
                                        for i in 1..retweetCount do

                                           let mutable retweetIndex = rand.Next(0,localTweetMap.Count)
                                           let mutable reTweetTuple = localTweetMap.[retweetIndex]
                                           let mutable reTweetUId = fst (reTweetTuple)
                                           let mutable  reTweetContent= snd (reTweetTuple)
                                           let tweetMsg = "RT:@"+ reTweetUId + " : " + reTweetContent
                                           //printf "User %s - tweetMsg %s\n" userId tweetMsg
                                           server <! Tweet(userId,tweetMsg)
                                    else
                                        globalReTweetCount <- globalReTweetCount - retweetCount                                                                               
        
        | BackOnline(msg) ->        server <! FetchOffline(userId)

        | ReceiveTweet(fromId,tweetMsg) ->  //let mutable placeholder = 0
        //                                     placeholder <- 1
                                            
                                            localTweetMap <- localTweetMap.Add(receivedTweetCount,(fromId,tweetMsg))
                                            receivedTweetCount <- receivedTweetCount + 1
                                            //printf "LocalTweet Map Size for actor %s is %d\n" userId localTweetMap.Count

                                            printf "\n@%s received tweet from @%s : %s" userId fromId tweetMsg

        | RecieveQuery(fromId,tweetMsg) ->  //let mutable placeholder = 0
        //                                     placeholder <- 1
                                            printf "\n@%s tweeted : %s" fromId tweetMsg                                                                                                                               
        
        | _ -> printf "Unknown message Recieved"

        return! loop()
    }   
    loop()

let mutable i = 0
// let mutable idSpace = 0
let mutable globalTweetCount = 0

//spawn the user actors
userActorList <- [for i in 0..n-1 do yield (spawn system (sprintf "%i" i) userActor)]

let activeCutoff = 0.9*(n |> float)
let activeCutoffInt = activeCutoff |> int

//printf "activeCutoffInt--%d\n" activeCutoffInt
for i in 0..n-1 do 
    let id = string i
    //printf "Sending register Message to %s\n" id                                    
    //Threading.Thread.Sleep(sleep200)
    userActorList.[i] <! RegisterMe(id)
    if i < activeCutoffInt then    
        server <! Register(id,"active")
    else
        server <! Register(id,"inactive")

//Threading.Thread.Sleep(sleep200)
server <! BuildFollowers("",0.5)
//Threading.Thread.Sleep(sleep200*5)

while idFollowerCountTupList.Length < n do
    i <- i+1        

i <- 0

printf "Initialization complete!!!\n"

idFollowerCountTupList <- List.sortBy (fun (_, y) -> -y) idFollowerCountTupList

let mutable b = 1
let mutable currCount = snd (idFollowerCountTupList.[0])
// printf "idFollowerCountTupList.Length%d" idFollowerCountTupList.Length

for i in 0..idFollowerCountTupList.Length-1 do
    let userIndex = fst (idFollowerCountTupList.[i])

    if snd (idFollowerCountTupList.[i]) <> currCount then
        b <- b + 1
        currCount <- snd (idFollowerCountTupList.[i])
    followerZipfDist.[userIndex] <- numTweets/b


printf "\n\nSimulating live circulation of tweets and re-tweets\n"
let mutable stopWatch = System.Diagnostics.Stopwatch.StartNew()

for i in 0..n-1 do
    let mutable totalTweets = followerZipfDist.[i] |> float
    let mutable retweetCount = (0.2 * totalTweets) 
    let mutable tweetCount = totalTweets - retweetCount
    let intTweetCount = tweetCount|>int
    if i < activeCutoffInt then
        globalTweetCount <- globalTweetCount + intTweetCount 
        userActorList.[i] <! SendTweets(intTweetCount)

// printf "Rows.count-- %d globalTweetCount-- %d\n" tweetsTable.Rows.Count globalTweetCount
    
while tweetsTable.Rows.Count < globalTweetCount do
    i <- i+1  

for i in 0..n-1 do
    let mutable totalTweets = followerZipfDist.[i] |> float
    let mutable retweetCount = (0.2 * totalTweets)
    let intReTweetCount = retweetCount|>int
    if i < activeCutoffInt then
        globalReTweetCount <- globalReTweetCount + intReTweetCount 
        userActorList.[i] <! SendRetweets(retweetCount|>int)        

while tweetsTable.Rows.Count < (globalTweetCount+globalReTweetCount) do
    //printf "Number of rows is %d\n" tweetsTable.Rows.Count
    i <- i+1 

Threading.Thread.Sleep(sleep200*(50))
stopWatch.Stop()
printfn "\nTweet/Retweet Time - %f ms\n" (stopWatch.Elapsed.TotalMilliseconds - 10000.0)
perfList <- List.append perfList [stopWatch.Elapsed.TotalMilliseconds - 10000.0]

stopWatch <- System.Diagnostics.Stopwatch.StartNew()
printf "\n\nSimulating users going from offline to online"
for i in activeCutoffInt..n-1 do
    userActorList.[i] <! BackOnline("")

//Querying Hashtag

Threading.Thread.Sleep(sleep200*(20))
let size1 = hashTagList.Length
//printf "TweetCount %d for user %d\n" tweetCount uid
let hashtagRand = rand.Next(0,size1)
let hashtagStr = hashTagList.[hashtagRand] |> string
stopWatch.Stop()
printfn "\n \nBackOnline Time - %f ms\n" (stopWatch.Elapsed.TotalMilliseconds - 4000.0)
perfList <- List.append perfList [stopWatch.Elapsed.TotalMilliseconds - 4000.0]

stopWatch <- System.Diagnostics.Stopwatch.StartNew()
printf "\n\nSimulating searching for a hashtag\nHashtag is %s" hashtagStr
let mutable userIndex = rand.Next(0,n)
userActorList.[userIndex] <! QueryHashtag(hashtagStr)

//Querying Mention

Threading.Thread.Sleep(sleep200)

//printf "TweetCount %d for user %d\n" tweetCount uid

let userRand = rand.Next(0,n)
let randStr = userRand |> string
let userId = "@" + randStr
printf "\n\nSimulating getting mentions for a user\nUser is @%s\n" userId
userActorList.[userRand] <! QueryMentions(userId)


//Active/Inactive
Threading.Thread.Sleep(sleep200)

let userRand1 = rand.Next(0,n)
let randStr1 = userRand1 |> string
printf "\n\nSimulating the feed for a user\nCurrent feed for user @%s is\n" randStr1
userActorList.[userRand1] <! GetFeed(randStr1)

Threading.Thread.Sleep(sleep200)
stopWatch.Stop()
printfn "\nQuerying Time - %f ms\n" (stopWatch.Elapsed.TotalMilliseconds - 600.0)
perfList <- List.append perfList [stopWatch.Elapsed.TotalMilliseconds - 600.0]


Threading.Thread.Sleep(sleep200*(50))

printfn "\nExecution Times For Users = %d and Max Tweets = %d :-\nTweet Simulation - %f ms\nOffline Simulation - %f ms\nQuery Simulation - %f ms" n numTweets perfList.[0] perfList.[1] perfList.[2]

System.Console.ReadLine()

// dotnet fsi --langversion:preview project4part1.fsx 10 5 > output.txt