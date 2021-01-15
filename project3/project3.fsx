#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit

let args = fsi.CommandLineArgs
let numNodesInput = args.[1] |> int
let numRequests = args.[2] |> int 
let mutable list = []
let mutable fullBuff = []

let system = System.create "system" (Configuration.defaultConfig())
        
let mutable logValue = System.Math.Log(numNodesInput |> float,2.0)
let logCeil = logValue |> ceil
let numNodesFloat = 2.0**logCeil
let numNodes = numNodesFloat |> int

let r = System.Random()

type IM =
    | InstigatorMessage of index: int * sum: int
    | Acknowledge of string

type PM =
    | InitMessage of conn: list<IActorRef>  * id: int
    | PeerMessage of peerid: int * conn: list<int>
    | PeerMessage1 of peerid: int * conn: list<int> * joinstr: string * currpeers:int * joinid:int
    | PeerMessage2 of peerid:int * sum:int * messagepass:string * m1:string * m2:string * index:int
    | Exit of string

let removeAt index input =
  input 
  |> List.mapi (fun i el -> (i <> index, el)) 
  |> List.filter fst |> List.map snd

let rec intToBinary i =
    match i with
    | 0 | 1 -> string i
    | _ ->
        let bit = string (i % 2)
        (intToBinary (i / 2)) + bit


let pastryActor instigatorFellow binarysize (mailbox: Actor<_>) = 
    let reactWithinTime = 1000 // 1 Second
    let mutable myID = 0
    let mutable myPeerID = 0;
    let mutable isPeer = 0;
    let mutable lesserLeaf = []
    let mutable greaterLeaf= []
    let mutable connectionList= []
    // var binarysize:Int = 7
    //type Row = ArrayBuffer[Int]
    //type conn_type = ArrayBuffer[PastryNode]
    //var myRowOfStrings = new Row
    //val RoutingTable = new Array[Row](binarysize)
    let routingTable = Array.create binarysize []
    let mutable forwardLeaf = []
    let mutable rearLeaf = []
    let mutable totNumPeers = 127
    let mutable myBinary = ""
    let mutable maxLeaf = -1
    let mutable minLeaf = -1
    let mutable currPeers = 0
    let mutable conn = []
 // var full_buff : Row

    let mutable i = 0

    // while i<binarysize){
    //     RoutingTable(i) = new ArrayBuffer[Int]
    //     i += 1
    // }

    let binaryfy x =   
        let mutable binarycode = intToBinary x
        if (binarycode.Length)<binarysize then
            let mutable diff = binarysize - binarycode.Length
            while diff >0 do
               binarycode <- "0" + binarycode
               diff <- diff- 1
        binarycode

    let flipModify (binarycode : string) loc = 
        let length = binarycode.Length
        let mutable modBinary = ""
        let mutable locFlag = 0
        let mutable inc = 0
        while inc<length do
            if loc=(inc + 1) then
                if binarycode.[inc] = '1' then
                    modBinary <- modBinary + "0"
                else 
                    modBinary <- modBinary + "1"
                
                locFlag <- 1
            else 
                if locFlag = 0 then
                    let charString = binarycode.[inc] |> string
                    modBinary <- modBinary + charString
                else 
                    modBinary <- modBinary + "0"               
            inc <- inc + 1    
        modBinary
      

    let returnMatchingBits (binarycodeOne: string) (binarycodeTwo: string) = 
        let mutable i = 0
        let mutable flag = 0
        let mutable placeholder = 0
        let length = binarycodeOne.Length
        while i<length do
            try
                if binarycodeOne.[i] = binarycodeTwo.[i] then
                    i <- i+1
                else
                    flag <- 1
                    placeholder <- i
                    i <- length            
            with
                | :?  System.IndexOutOfRangeException -> printfn "$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$4 "

        if flag=1 then
            placeholder
        else
            i
      

    let checkifWithinLeafs target = 
        let mutable nearest = 1000000000
        let mutable i = 0
        let mutable diff = 1000
        let mutable flag = 0
        let mutable placeholder = 0
        while i<4 do // num of leaf nodes
            if forwardLeaf.[i] = target then
                flag <- 1
                placeholder <- forwardLeaf.[i]
                i <- 4
            else
                diff <- target - forwardLeaf.[i]
                let absDiff =  abs diff
                if absDiff<diff then
                    nearest <- i
                    diff <- absDiff
                            
            i <- i+1

        if flag=1 then
            placeholder
        else
            if diff >= 2 then
                placeholder <- -999
                placeholder
            else 
               placeholder <- forwardLeaf.[nearest]
               placeholder
            
            
        
    let rec loop () = actor {
        let! message = mailbox.Receive ()
        match message with
        |  Exit msg -> printf "Exit"
        |  InitMessage (inconn,id) ->   myID <- id
                                        conn <- inconn
        |  PeerMessage (peerid,inconn) ->   myPeerID <- peerid
                                            connectionList <- inconn
                                            isPeer <- 1
                                            myBinary <- binaryfy peerid   

                            
        |  PeerMessage1 (peerid, inconn,joinstr,incurrpeers,joinid) ->    if joinstr = "Join1" then //do routine joins
                                                                            currPeers <- incurrpeers
                                                                            let mutable curBit = 0
                                                                            while curBit<binarysize do
                                                                                let temp = flipModify myBinary (curBit + 1)
                                                                                let temp1 =  List.append routingTable.[0] [Convert.ToInt32(temp, 2)]
                                                                                routingTable.[0] <- temp1
                                                                                curBit <- curBit + 1 
                                                                            totNumPeers <- 7           // setting the total number of peers to be 8, so old join code works fine.
                                                                            if (myPeerID + 1) > (totNumPeers - 1) then
                                                                                forwardLeaf <- List.append forwardLeaf [0]
                                                                                forwardLeaf <- List.append forwardLeaf [1]
                                                                            elif (myPeerID + 2) > (totNumPeers - 1) then
                                                                                forwardLeaf <- List.append forwardLeaf [(myPeerID + 1)] 
                                                                                forwardLeaf <- List.append forwardLeaf [0]
                                                                            else
                                                                                forwardLeaf <- List.append forwardLeaf [(myPeerID + 1)] 
                                                                                forwardLeaf  <- List.append forwardLeaf [(myPeerID + 2)]
                                                                                maxLeaf <- (myPeerID + 2)
                                                                            

                                                                            if (myPeerID - 1) < 0  then
                                                                                forwardLeaf <- List.append forwardLeaf [totNumPeers - 2]
                                                                                forwardLeaf <- List.append forwardLeaf [totNumPeers - 1]
                                                                            elif (myPeerID - 2) < 0 then
                                                                                forwardLeaf <- List.append forwardLeaf [(myPeerID - 1)] 
                                                                                forwardLeaf <- List.append forwardLeaf [(totNumPeers - 1)]
                                                                            else
                                                                                forwardLeaf <- List.append forwardLeaf [(myPeerID - 1)] 
                                                                                forwardLeaf  <- List.append forwardLeaf [(myPeerID - 2)]
                                                                                minLeaf <- (myPeerID - 2)

                                                                            mailbox.Sender() <! "initiate" 
                                                                            instigatorFellow <! Acknowledge "acknowledged"
                                                                           else    
                                                                                totNumPeers <- incurrpeers
                                                                                if peerid = joinid then
                                                                                    let mutable curBit = 0
                                                                                    while curBit<binarysize do
                                                                                        let temp = flipModify myBinary (curBit + 1)
                                                                                        let temp1 =  List.append routingTable.[0] [Convert.ToInt32(temp, 2)]
                                                                                        routingTable.[0] <- temp1
                                                                                        curBit <- curBit + 1
                                                                                totNumPeers <- currPeers
                                                                                //clear ForwardLeaf.clear
                                                                                if (myPeerID + 1) > (totNumPeers - 1) then
                                                                                    forwardLeaf <- List.append forwardLeaf [0]
                                                                                    forwardLeaf <- List.append forwardLeaf [1]
                                                                                elif (myPeerID + 2) > (totNumPeers - 1) then
                                                                                    forwardLeaf <- List.append forwardLeaf [(myPeerID + 1)] 
                                                                                    forwardLeaf <- List.append forwardLeaf [0]
                                                                                else
                                                                                    forwardLeaf <- List.append forwardLeaf [(myPeerID + 1)] 
                                                                                    forwardLeaf  <- List.append forwardLeaf [(myPeerID + 2)]
                                                                                    maxLeaf <- (myPeerID + 2)

                                                                                if (myPeerID - 1) < 0  then
                                                                                    forwardLeaf <- List.append forwardLeaf [totNumPeers - 2]
                                                                                    forwardLeaf <- List.append forwardLeaf [totNumPeers - 1]
                                                                                elif (myPeerID - 2) < 0 then
                                                                                    forwardLeaf <- List.append forwardLeaf [(myPeerID - 1)] 
                                                                                    forwardLeaf <- List.append forwardLeaf [(totNumPeers - 1)]
                                                                                else
                                                                                    forwardLeaf <- List.append forwardLeaf [(myPeerID - 1)] 
                                                                                    forwardLeaf  <- List.append forwardLeaf [(myPeerID - 2)]
                                                                                    minLeaf <- (myPeerID - 2)

                                                                                if( peerid = joinid) then
                                                                                    mailbox.Sender() <! "initiate" 
                                                                                    instigatorFellow <! Acknowledge "acknowledged"

        | PeerMessage2 (peerid,sum,messagepass,m1,m2,index) ->  if messagepass  = "MessagePassing" then
                                                                    if peerid = myPeerID then
                                                                        printf "Reached destination from %d in %d hops" index sum
                                                                        instigatorFellow <! InstigatorMessage(index, sum)
                                                                    else
                                                                        let nextPeer = checkifWithinLeafs peerid
                                                                        if nextPeer <> (-999) then 
                                                                            conn.[connectionList.[nextPeer]] <! PeerMessage2(peerid,(sum + 1),"MessagePassing","abcd","abcd",index)

                                                                        else
                                                                            let len = routingTable.[0].Length
                                                                            let mutable  i = 0
                                                                            let curr = binaryfy myPeerID
                                                                            let temp = binaryfy peerid
                                                                            i <- returnMatchingBits curr temp
                                                                            if i < len then
                                                                                conn.[connectionList.[(routingTable.[0]).[i]]] <! PeerMessage2 (peerid, (sum + 1),"MessagePassing","abcd","abcd",index)

        
      
                        
        | _ -> failwith "unknown message"
        return! loop ()
    }
    loop ()


let instigatorActor numPeers (fullBuff: _ list) (conn: _ list) numRequests (mailbox: Actor<_>) =     
    let mutable readyPeers = 0
    let totNumPeers = numPeers
    let mutable i = 0
    let sum = [| for i in 1 .. numPeers+1 -> i*0 |]
    let mutable tempRand = r.Next(0,totNumPeers)
    let totalmessages = numRequests * numPeers
    let mutable rxmessages = 0
    let mutable j = 0
    let mutable count = 0.0

    let rec loop () = actor {
        let! message = mailbox.Receive ()
        match message with
        |  Acknowledge msg->    readyPeers <- readyPeers + 1
                                if readyPeers = totNumPeers then
                                    printfn "*************************************************************************"
                                    j <- 0
                                    while j<numRequests do
                                        i <- 0
                                        while i<totNumPeers do
                                        //conn(full_buff(i)) ! (full_buff(tempRand),1,"MessagePassing","MessagePassing",full_buff(i))
                                            conn.[fullBuff.[i]] <! (fullBuff.[tempRand],1,"MessagePassing","abcd","abcd",i)
                                            tempRand <- r.Next(0, totNumPeers-1)
                                            i <- i + 1
                                        
                                        j <- j+1
                                    
        |  InstigatorMessage(index,s)-> sum.[index] <- sum.[index] + s
                                        rxmessages <- rxmessages + 1
                                        if rxmessages = totalmessages then
                                            j <- 0
                                            while j<(totNumPeers+1) do
                                                let mutable tempSum = sum.[j] |> float                                              
                                                count <- count + tempSum
                                                j <- j+1

                                        let denom = (totNumPeers+1)*numRequests
                                        let floatDenom = denom |> float
                                        let avg = count/floatDenom

                                        printfn "Done!!!!Average number of hops for passing %d messages for each node is : %f" numRequests avg
                                          
                                        j <- 0
                                        while j<totNumPeers do
                                            conn.[fullBuff.[j]] <! "exit"
                                            j <- j+1
                                          
                                        printfn "Exit"

        | _ -> failwith "unknown message"
        return! loop ()
    }
    loop ()


let networkBuilderActor nodeCount numRequests (mailbox: Actor<_>) = 
    let numNodes = nodeCount - 1
    let numPeers = nodeCount
    let mutable k = numNodes - 1
    let binarySizeFloat = System.Math.Log((numPeers+1) |> float,2.0)
    let binarySize = binarySizeFloat |> int
    let mutable currPeers = 0

    printfn "The size = %d\n" binarySize
    
    let mutable m = 0
    
    for i = 0 to k do
        list <- List.append list [i]
    
    let mutable tempRand = r.Next(0,k)
    //let stopWatch = System.Diagnostics.Stopwatch.StartNew()

    while k >= 0 do
        if k <> 0  then
            tempRand <- r.Next(0,k)
        else
            tempRand <- 0

        fullBuff <- List.append fullBuff [list.[tempRand]]
        list <- removeAt list.[tempRand] list
        k <- k-1

    let mutable conn = []

    k <- 0

    let instigator = spawn system "InstigatorActor" <| instigatorActor numPeers fullBuff conn numRequests

    // initialize all the connections (actors)
    while k<numNodes do
        let newNode = spawn system ("PastryActor" + (k.ToString())) <| pastryActor instigator binarySize
        conn <- List.append conn [newNode]
        k <- k + 1
  
    //instigator ! conn //ITS HANDLING SEEMS TO BE COMMENTED
    k <- 0
    while k<numNodes do
       conn.[k] <! InitMessage(conn,k) //sending node Id
       k <- k+1
  
    k <- 0
    while k<numNodes do
        conn.[fullBuff.[k]] <! PeerMessage(k,fullBuff)
        k <- k+1
  
    k <- 0

    let rec loop () = actor {
        let! message = mailbox.Receive ()
        match message with
        |  "initiate" ->    if k<8 then//initialize 8 actors normally// each actor will return here after initialize so that next one can initialize
                                conn.[fullBuff.[k]] <! PeerMessage1(k,fullBuff,"Join1",currPeers,currPeers) // we need to change this message to join so that apart from the calculations it is doing noe, it also does join.
                                k <- k + 1
                                currPeers <- currPeers + 1
                            else if k<numNodes then //now do the join// each actor will return here after initialize so that next one can initialize
                                tempRand <- r.Next(0, k-1)
                                m <- 0
                                while m<=currPeers do
                                    conn.[fullBuff.[m]] <! PeerMessage1(m,fullBuff,"Join2",currPeers,k) // we need to change this message to join so that apart from the calculations it is doing noe, it also does join.
                                    m <- m+1
                                
                                k <- k+1
                                currPeers <- currPeers+1
                            
                            if k=numNodes then
                                printfn "Exit"


        | _ -> failwith "unknown message"
        return! loop ()
    }
    loop ()

let networkBuilder = spawn system "NetworkBuilderActor" <| networkBuilderActor numNodes numRequests
networkBuilder <! "initiate"

System.Console.ReadLine()
system.Terminate()