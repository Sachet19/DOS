namespace project42
module Program =
    open Suave
    open Suave.Http
    open Suave.Operators
    open Suave.Filters
    open Suave.Successful
    open Suave.Files
    open Suave.RequestErrors
    open Suave.Logging
    open Suave.Utils

    open System
    open System.Net

    open Suave.Sockets
    open Suave.Sockets.Control
    open Suave.WebSocket

    open System.Data
    
    let mutable globalTweetIdTracker = 1

    let userTable = new System.Data.DataTable()

    let dc1 = new DataColumn("userId")
    let dc2 = new DataColumn("followers")
    let dc3 = new DataColumn("following")
    let dc4 = new DataColumn("userStatus")
    let dc5 = new DataColumn("password") //SACHET NEW

    userTable.Columns.Add(dc1)
    userTable.Columns.Add(dc2)
    userTable.Columns.Add(dc3)
    userTable.Columns.Add(dc4)
    userTable.Columns.Add(dc5) //SACHET NEW

    let primaryKeyColumns1:DataColumn [] = [|userTable.Columns.["userId"]|]
    userTable.PrimaryKey <- primaryKeyColumns1
    userTable.AcceptChanges()


    let tweetsTable = new System.Data.DataTable()
    let dc11 = new DataColumn("tweetId")
    dc11.DataType = System.Type.GetType("System.Int32") |> ignore
    let dc12 = new DataColumn("tweetContent")
    tweetsTable.Columns.Add(dc11)
    tweetsTable.Columns.Add(dc12)
    let primaryKeyColumns2:DataColumn [] = [|tweetsTable.Columns.["tweetId"]|]
    tweetsTable.PrimaryKey <- primaryKeyColumns2
    tweetsTable.AcceptChanges()

    let hashtagsTable = new System.Data.DataTable()
    let dc21 = new DataColumn("hashtag")
    let dc22 = new DataColumn("hashTweetId")
    dc22.DataType = System.Type.GetType("System.Int32") |> ignore
    hashtagsTable.Columns.Add(dc21)
    hashtagsTable.Columns.Add(dc22)
    hashtagsTable.AcceptChanges()


    let mentionsTable = new System.Data.DataTable()
    let dc31 = new DataColumn("mention")
    let dc32 = new DataColumn("menTweetId")
    dc32.DataType = System.Type.GetType("System.Int32") |> ignore
    mentionsTable.Columns.Add(dc31)
    mentionsTable.Columns.Add(dc32)
    mentionsTable.AcceptChanges()


    let userTweetMappingTable = new System.Data.DataTable()
    let dc41 = new DataColumn("userId")
    let dc42 = new DataColumn("tweetId")
    dc42.DataType = System.Type.GetType("System.Int32") |> ignore
    userTweetMappingTable.Columns.Add(dc41)
    userTweetMappingTable.Columns.Add(dc42)
    userTweetMappingTable.AcceptChanges()


    let userOfflineTweetMappingTable = new System.Data.DataTable()
    let dc51 = new DataColumn("userId")
    let dc52 = new DataColumn("offTweetId")
    dc52.DataType = System.Type.GetType("System.Int32") |> ignore
    userOfflineTweetMappingTable.Columns.Add(dc51)
    userOfflineTweetMappingTable.Columns.Add(dc52)
    userOfflineTweetMappingTable.AcceptChanges()

    let userFollowerMappingTable = new System.Data.DataTable()
    let dc61 = new DataColumn("userId")
    let dc62 = new DataColumn("followerId")
    userFollowerMappingTable.Columns.Add(dc61)
    userFollowerMappingTable.Columns.Add(dc62)
    userFollowerMappingTable.AcceptChanges()

    type Simple = FSharp.Data.JsonProvider<""" { "Command":"John", "Param1":"text", "Param2":"hello"  } """>


    let ws (webSocket : WebSocket) (context: HttpContext) =
      socket {
        // if `loop` is set to false, the server will stop receiving messages
        let mutable loop = true

        while loop do
          // the server will wait for a message to be received without blocking the thread
          let! msg = webSocket.read()
          //let cstr = context.ToString()
          //printf "Inside While Loop\n"
          
          match msg with
          // the message has type (Opcode * byte [] * bool)
          //
          // Opcode type:
          //   type Opcode = Continuation | Text | Binary | Reserved | Close | Ping | Pong
          //
          // byte [] contains the actual message
          //
          // the last element is the FIN byte, explained later
          | (Text, data, true) ->
            // the message can be converted to a string
            let str = UTF8.toString data
            //let response = sprintf "response to %s" str
            //printfn "%s\n" response
            //let modifiedStr = "\"\" " + str + " \"\""
            //printfn "%s\n" modifiedStr
            let simple = Simple.Parse(str)
            let command = simple.Command
            //let userId = simple.Param1
            //let param2 = simple.Param2


            // let firstSpace = str.IndexOf " "
            // let command = str.[..(firstSpace-1)] //TODO - Extract the command identifier from the received string and match it
            // let parameters = str.[(firstSpace+1)..]

            printfn "New message received with command %s\n" command 

            match command with

            
            // Tweet with user id formatting
            | "Register" ->   //let paramSpace1 = parameters.IndexOf " "
                              let userId = simple.Param1 //TODO - Extract userId section from str
                              let password = simple.Param2

                              //printf "User table row count before %d\n" userTable.Rows.Count
                              userTable.Rows.Add(userId, "", "", "active", password) |> ignore
                              //printf "User table row count after %d\n" userTable.Rows.Count

                              let registerResponse =
                                "{\"Response\" : \"Registeration Successful!\"}"
                                |> string
                                |> System.Text.Encoding.ASCII.GetBytes
                                |> ByteSegment
                              
                              // the `send` function sends a message back to the client
                              do! webSocket.send Text registerResponse true

            | "Login" ->   //let paramSpace1 = parameters.IndexOf " "
                          //let paramSpace1 = parameters.IndexOf " "
                          let userId = simple.Param1 //TODO - Extract userId section from str
                          let password = simple.Param2

                          let ustr = String.Format("userId = '{0}'", userId)
                          let existingRows:DataRow [] = userTable.Select(ustr)
                          
                          if existingRows.Length = 0 then
                            let invliadUserResponse =
                              "{\"Response\" : \"NoUser\"}"
                              |> string
                              |> System.Text.Encoding.ASCII.GetBytes
                              |> ByteSegment

                            do! webSocket.send Text invliadUserResponse true
                          else
                            let storedPass = existingRows.[0].[4] |> string

                            if storedPass.Equals(password) then
                              let loginResponse =
                                "{\"Response\" : \"LoginSuccesful\"}"
                                |> string
                                |> System.Text.Encoding.ASCII.GetBytes
                                |> ByteSegment

                              do! webSocket.send Text loginResponse true

                              let mutable updatedRow:obj [] = [|existingRows.[0].[0] ; existingRows.[0].[1]; existingRows.[0].[2]; "active" ;existingRows.[0].[4]|]
                              userTable.BeginLoadData()
                              let row = userTable.LoadDataRow(updatedRow,LoadOption.Upsert)
                              userTable.EndLoadData()
                              userTable.AcceptChanges()


                              let qstr = String.Format("userId = '{0}'", userId)
                              let existingRows:DataRow [] = userOfflineTweetMappingTable.Select(qstr)

                              //printf "existingRows.Length is %d\n" existingRows.Length

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

                                  let temp = userIdTweetIdList.Length |> string
                                  let tempResp = "{\"Response\" :\"" + temp + "\"}" 
                                  //let tempResp =  tempResp1 + "\"}" 

                                  let offlineTweetCountResponse =
                                    tempResp
                                    |> string
                                    |> System.Text.Encoding.ASCII.GetBytes
                                    |> ByteSegment

                                  do! webSocket.send Text offlineTweetCountResponse true
                                  
                                  for i in 0..userIdTweetIdList.Length-1 do
                                      let uid = fst (userIdTweetIdList.[i])
                                      let tid = snd (userIdTweetIdList.[i])
                                      let qstr = String.Format("tweetId = '{0}'", tid)
                                      let tweetRows:DataRow [] = tweetsTable.Select(qstr)
                                      let tweetcontent = tweetRows.[0].[1] |> string
                                      let strUID = uid |> string
                                      let ucontent = "@" + strUID + ": "
                                      let tcontent =  ucontent + tweetcontent
                                      let offlineTweetResponse =
                                        "{\"Response\" :\"" + tcontent + "\"}"
                                        |> System.Text.Encoding.ASCII.GetBytes
                                        |> ByteSegment

                                      do! webSocket.send Text offlineTweetResponse true 
                              else 
                                  //let temp = 0 |> string
                                  let tempResp = "{\"Response\" :\"0\"}"
                                  let offlineTweetNullResponse =
                                    tempResp
                                    |> string
                                    |> System.Text.Encoding.ASCII.GetBytes
                                    |> ByteSegment

                                  do! webSocket.send Text offlineTweetNullResponse true                                          
                            else
                              let invalidPasswordResponse =
                                "{\"Response\" : \"WrongPassword\"}"
                                |> System.Text.Encoding.ASCII.GetBytes
                                |> ByteSegment

                              do! webSocket.send Text invalidPasswordResponse true                                                                        

            | "Logout" ->   
                          let userId = simple.Param1
                          //printfn " Number of users in table before= %d" userTable.Rows.Count

                          // let pass = str //TODO - Extract password section from str //SACHET NEW
                          let ustr = String.Format("userId = '{0}'", userId)
                          let existingRows:DataRow [] = userTable.Select(ustr)
                          let mutable updatedRow:obj [] = [|existingRows.[0].[0] ; existingRows.[0].[1]; existingRows.[0].[2]; "inactive" ;existingRows.[0].[4]|]
                          userTable.BeginLoadData()
                          let row = userTable.LoadDataRow(updatedRow,LoadOption.Upsert)
                          userTable.EndLoadData()
                          userTable.AcceptChanges()

                          let logoutResponse =
                            "{\"Response\" : \"LoggedOut\"}"
                            |> string
                            |> System.Text.Encoding.ASCII.GetBytes
                            |> ByteSegment
                          
                          // the `send` function sends a message back to the client
                          do! webSocket.send Text logoutResponse true


            | "Tweet" ->  let curTweetId = globalTweetIdTracker

                          //let firstParamSpace = parameters.IndexOf " "
                          let userId = simple.Param1 //TODO - Extract the command identifier from the received string and match it
                          let tweetMsg = simple.Param2

                          //printf "Tweet Message is - %s" tweetMsg                

                          let ustr = String.Format("userId = '{0}'", userId)
                          let existingRows:DataRow [] = userFollowerMappingTable.Select(ustr)
                          let mutable followersList = []

                          //printf "Select Clause - %s\n" ustr
                          //printf "Number of total rows in mapping table = %d \n Number of rows in the filtered fetch =  %d\n" userFollowerMappingTable.Rows.Count existingRows.Length

                          for i in 0..existingRows.Length-1 do
                              let strFollowerID = existingRows.[i].[1] |> string
                              followersList <- List.append followersList [strFollowerID]


                          for i in 0..followersList.Length-1 do
                              //let ind = followersList.[i] |> int
                              let innerqstr = String.Format("userId = '{0}'", followersList.[i])
                              let followerRows:DataRow [] = userTable.Select(innerqstr)
                              let statusStr = followerRows.[0].[3] |> string
                              if statusStr.Equals("inactive") then
                                  //userActorList.[ind] <! ReceiveTweet(userId,tweetMsg) //SACHET
                              
                                  //TODO - Send tweet to all followers who are currently connected - Maybe we still leverage the "active" status? Might have to somehow check the websocket for active connection and see if the client is a followers or not
                                  // On the python side for this functionality we need to be able to listen for an incoming message without initialtion by the pyhton client as well
                                  
                             // else
                                //printf "\n User id being inserted is %s with tweetID %d \n " followersList.[i] curTweetId
                                let offlineUserStr = followerRows.[0].[0] |> string
                                userOfflineTweetMappingTable.Rows.Add(followersList.[i], curTweetId) |> ignore 

                          tweetsTable.Rows.Add( curTweetId , tweetMsg) |> ignore
                          
                          userTweetMappingTable.Rows.Add(userId, curTweetId) |> ignore
                          //Threading.Thread.Sleep(sleep200)

                          let tweetWords = tweetMsg.Split [|' '|]

                          //printfn " Number of tweets in hashtag table after = %d" hashtagsTable.Rows.Count

                          for i in 0..tweetWords.Length-1 do
                              if tweetWords.[i].[0] = '@' then
                                  //Threading.Thread.Sleep(sleep200)
                                  mentionsTable.Rows.Add(tweetWords.[i], curTweetId)  |> ignore
                                  //Threading.Thread.Sleep(sleep200)
                              else if tweetWords.[i].[0] = '#' then
                                      //Threading.Thread.Sleep(sleep200)
                                      hashtagsTable.Rows.Add(tweetWords.[i], curTweetId)  |> ignore
                                      //Threading.Thread.Sleep(sleep200)
                          //printfn " Number of tweets in hashtag table after = %d" hashtagsTable.Rows.Count


                          globalTweetIdTracker <- globalTweetIdTracker+1

                          let tweetByteResponse =
                                "{\"Response\" : \"Tweet Sent\"}"
                                |> System.Text.Encoding.ASCII.GetBytes
                                |> ByteSegment

                          do! webSocket.send Text tweetByteResponse true //SACHET NEW - Maybe send an acknowledgement back?
                                               
            | "HashtagQuery" ->   //let firstParamSpace = parameters.IndexOf " "
                                  let userId = simple.Param1 //TODO - Extract the command identifier from the received string and match it
                                  let hashtag = simple.Param2                        

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


                                      let temp = userIdTweetIdList.Length |> string
                                      let tempResp = "{\"Response\" :\"" + temp + "\"}" 

                                      let hashTagQueryTweetCountResponse =
                                        tempResp
                                        |> string
                                        |> System.Text.Encoding.ASCII.GetBytes
                                        |> ByteSegment

                                      do! webSocket.send Text hashTagQueryTweetCountResponse true



                                      //printf "Before third for \n"
                                      for i in 0..userIdTweetIdList.Length-1 do
                                          let uid = fst (userIdTweetIdList.[i])
                                          let tid = snd (userIdTweetIdList.[i])
                                          let qstr = String.Format("tweetId = '{0}'", tid)
                                          let tweetRows:DataRow [] = tweetsTable.Select(qstr)
                                          let tweetcontent = tweetRows.[0].[1] |> string
                                          let strUID = uid |> string
                                          let ucontent = "@" + strUID + ": "
                                          let tcontent =  ucontent + tweetcontent

                                          let hashTagQueryTweetResponse =
                                            "{\"Response\" :\"" + tcontent + "\"}"
                                            |> System.Text.Encoding.ASCII.GetBytes
                                            |> ByteSegment

                                          do! webSocket.send Text hashTagQueryTweetResponse true

                                  else  
                                        let tempResp = "{\"Response\" :\"0\"}"
                                        let hashTagQueryTweetNullResponse =
                                          tempResp
                                          |> string
                                          |> System.Text.Encoding.ASCII.GetBytes
                                          |> ByteSegment

                                        do! webSocket.send Text hashTagQueryTweetNullResponse true
 //TODO Change Message return to webSocket way
                                                                     
                                        
            
            | "MentionQuery" ->   let userId = simple.Param1 //TODO - Extract the command identifier from the received string and match it
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

                                      let temp = userIdTweetIdList.Length |> string
                                      let tempResp = "{\"Response\" :\"" + temp + "\"}" 

                                      let mentionQueryTweetCountResponse =
                                        tempResp
                                        |> string
                                        |> System.Text.Encoding.ASCII.GetBytes
                                        |> ByteSegment

                                      do! webSocket.send Text mentionQueryTweetCountResponse true                                            

                                      for i in 0..userIdTweetIdList.Length-1 do
                                          let uid = fst (userIdTweetIdList.[i])
                                          let tid = snd (userIdTweetIdList.[i])
                                          let qstr = String.Format("tweetId = '{0}'", tid)
                                          let tweetRows:DataRow [] = tweetsTable.Select(qstr)
                                          let tweetcontent = tweetRows.[0].[1] |> string
                                          let strUID = uid |> string
                                          let ucontent = "@" + strUID + ": "
                                          let tcontent =  ucontent + tweetcontent

                                          let mentionQueryTweetResponse =
                                            "{\"Response\" :\"" + tcontent + "\"}"
                                            |> System.Text.Encoding.ASCII.GetBytes
                                            |> ByteSegment

                                          do! webSocket.send Text mentionQueryTweetResponse true

                                  else 
                                      let tempResp = "{\"Response\" :\"0\"}" 
                                      let mentionQueryTweetNullResponse =
                                        tempResp
                                        |> string
                                        |> System.Text.Encoding.ASCII.GetBytes
                                        |> ByteSegment

                                      do! webSocket.send Text mentionQueryTweetNullResponse true                                          

            | "GetFeed" ->  let userId = simple.Param1
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

                                        let temp = userIdTweetIdList.Length |> string
                                        let tempResp = "{\"Response\" :\"" + temp + "\"}"

                                        let feedTweetCountResponse =
                                          tempResp
                                          |> string
                                          |> System.Text.Encoding.ASCII.GetBytes
                                          |> ByteSegment

                                        do! webSocket.send Text feedTweetCountResponse true                                                 
                                    
                                    printf "Number of tweeets in feed %d \n" userIdTweetIdList.Length
                                    for i in 0..userIdTweetIdList.Length-1 do
                                        let uid = fst (userIdTweetIdList.[i])
                                        let tid = snd (userIdTweetIdList.[i])
                                        let qstr = String.Format("tweetId = '{0}'", tid)
                                        let tweetRows:DataRow [] = tweetsTable.Select(qstr)
                                        let tweetcontent = tweetRows.[0].[1] |> string
                                        let strUID = uid |> string
                                        let ucontent = "@" + strUID + ": "
                                        let tcontent =  ucontent + tweetcontent
                                        
                                        let feedTweetResponse =
                                          "{\"Response\" :\"" + tcontent + "\"}"
                                          |> System.Text.Encoding.ASCII.GetBytes
                                          |> ByteSegment

                                        do! webSocket.send Text feedTweetResponse true
                            else  
                                let tempResp = "{\"Response\" :\"0\"}" 
                                let feedNullResponse =
                                  tempResp
                                  |> string
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment

                                do! webSocket.send Text feedNullResponse true

                            printf "Exiting get feed \n"

            | "FollowQuery" ->  let userId = simple.Param1 
                                let ustr = String.Format("followerId = '{0}'", userId)
                                //printf "%s ProcessFeedQuery ustr\n" ustr
                                let followingRows:DataRow [] = userFollowerMappingTable.Select(ustr)

                                let notUstr = String.Format("userId <> '{0}'", userId)
                                let userRows:DataRow [] = userTable.Select(notUstr)


                                //printf "Number of followers right now %d \n Number of total users apart from current user himself %d\n" followingRows.Length userRows.Length

                                
                                if userRows.Length > 0 then        
                                  //printf "Inside first if \n"                                 
                                  //let followingStr = followingRows.[0].[2] |> string
                                  //let followingList = followingStr.Split [|'|'|]
                                  let mutable followingList = []
                                  let mutable candidateList = []

                                  for i in 0..followingRows.Length-1 do
                                      let strFollowingID = followingRows.[i].[0] |> string
                                      followingList <- List.append followingList [strFollowingID]

                                  for i in 0..userRows.Length-1 do
                                      let strCandidateID = userRows.[i].[0] |> string
                                      let mutable flag = 1
                                      for j in 0..followingList.Length-1 do 
                                          if followingList.[j] = strCandidateID then
                                              flag <- 0

                                      if flag = 1 then
                                          candidateList <- List.append candidateList [strCandidateID]

                                  //printf "Candidate List length - %d\n" candidateList.Length
                                  if candidateList.Length > 0 then

                                    //printf "Inside second if \n"                                    
                                    let temp = candidateList.Length |> string
                                    let tempResp = "{\"Response\" :\"" + temp + "\"}"

                                    let candidateCountResponse =
                                      tempResp
                                      |> string
                                      |> System.Text.Encoding.ASCII.GetBytes
                                      |> ByteSegment

                                    do! webSocket.send Text candidateCountResponse true                                                 
                                    
                                    //printf "Before final for\n"
                                    for i in 0..candidateList.Length-1 do
                                        let uid = candidateList.[i]                                                                              

                                        let tempResp = "{\"Response\" :\"" + uid + "\"}"

                                        let candidateResponse =
                                          tempResp
                                          |> System.Text.Encoding.ASCII.GetBytes
                                          |> ByteSegment

                                        do! webSocket.send Text candidateResponse true
                                  else 
                                    let tempResp = "{\"Response\" :\"0\"}" 
                                    //printf "Inside second else \n" 
                                    let followNullResponse =
                                      tempResp
                                      |> string
                                      |> System.Text.Encoding.ASCII.GetBytes
                                      |> ByteSegment

                                    do! webSocket.send Text followNullResponse true

                                else
                                    let tempResp = "{\"Response\" :\"0\"}" 
                                    //printf "Inside first else \n"
                                    let followNullResponse =
                                      tempResp
                                      |> string
                                      |> System.Text.Encoding.ASCII.GetBytes
                                      |> ByteSegment

                                    do! webSocket.send Text followNullResponse true 

            | "FollowRequest" ->  //let paramSpace = parameters.IndexOf " "
                                  let userId = simple.Param1 //TODO - Extract userId section from str
                                  let followingId = simple.Param2

                                  //printf "Following ID %s UserId %s\n" followingId userId

                                  //printf "Number of entries in follower mapping before = %d\n" userFollowerMappingTable.Rows.Count
                                  userFollowerMappingTable.Rows.Add(followingId, userId) |> ignore
                                  //printf "Number of entries in follower mapping after = %d\n" userFollowerMappingTable.Rows.Count
                                      
                                  let tempResp = "{\"Response\" :\"Follow Successful\"}" 
                                  let followResponse =
                                    tempResp
                                    |> string
                                    |> System.Text.Encoding.ASCII.GetBytes
                                    |> ByteSegment
                                  
                                  // the `send` function sends a message back to the client
                                  do! webSocket.send Text followResponse true
                                               
            | _ -> printf "Unknown message Recieved"                

          | (Close, _, _) ->
            let emptyResponse = [||] |> ByteSegment
            do! webSocket.send Close emptyResponse true


          | _ -> ()
        }

    let app : WebPart = handShake ws      
      
    [<EntryPoint>]
    let main _ =
      startWebServer { defaultConfig with logger = Targets.create Verbose [||] } app
      0

//
// The FIN byte:
//
// A single message can be sent separated by fragments. The FIN byte indicates the final fragment. Fragments
//
// As an example, this is valid code, and will send only one message to the client:
//
// do! webSocket.send Text firstPart false
// do! webSocket.send Continuation secondPart false
// do! webSocket.send Continuation thirdPart true
//
// More information on the WebSocket protocol can be found at: https://tools.ietf.org/html/rfc6455#page-34
//