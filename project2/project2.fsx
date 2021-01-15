#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit

let args = fsi.CommandLineArgs
let numNodes = args.[1]
let topology = args.[2]
let algorithm = args.[3]

let system = System.create "system" (Configuration.defaultConfig())
        
let mutable register = []
let mutable n = numNodes|> int
if topology = "2D" || topology = "imp2D" then
    let floatNumNode = numNodes |> float
    let floatRoot = sqrt(floatNumNode)
    let floorFloatRoot = floatRoot |> floor
    let squareValue = floorFloatRoot * floorFloatRoot
    if squareValue <> floatNumNode then 
        let root = (floorFloatRoot |> int) + 1 
        n <- root*root

let r = System.Random()
    
let lineNeighbours i=   
    if i = 0 then
      let listOfNeighb = [1]
      listOfNeighb
    else if i = n-1 then
      let listOfNeighb = [n-2]
      listOfNeighb
    else
      let listOfNeighb = [i-1; i+1]
      listOfNeighb

let twoDNeighbours i = 
    let floatRoot = sqrt(numNodes |> float)
    let root = floatRoot |> int
    if i = 0 then
        let listOfNeighb = [ i+1; i+root ]
        listOfNeighb
    elif i= root-1 then
        let listOfNeighb =  [ root-2; root+root-1 ] 
        listOfNeighb
    elif i = n-root then
        let listOfNeighb = [ n-(root*2); n-root+1 ]
        listOfNeighb
    elif i = n-1  then
        let listOfNeighb = [ n-2; n-root-1 ]
        listOfNeighb
    elif i<root then
        let listOfNeighb = [ i-1; i+1; i+root ]
        listOfNeighb
    elif i % root = 0 then
        let listOfNeighb = [ i+1; i+root; i-root ]
        listOfNeighb
    elif i > n-root  then
        let listOfNeighb = [ i-1; i+1; i-root ]
        listOfNeighb
    elif  i % root = root-1 then
        let listOfNeighb = [ i-1; i+root; i-root ]
        listOfNeighb
    else
        let listOfNeighb = [ i-1; i+1; i-root; i+root ]
        listOfNeighb

let containsNumber number list = List.exists (fun elem -> elem = number) list

let findNeighbours i =
    if topology = "line" then 
        lineNeighbours i
    elif topology = "2D" then 
        twoDNeighbours i
    elif topology = "imp2D" then 
        let twoDlist = twoDNeighbours i        
        let mutable randomNeighbour = r.Next(0, n)
        while containsNumber randomNeighbour twoDlist && randomNeighbour<>i do
            randomNeighbour <- r.Next(0,n)
        let randomList = [randomNeighbour]
        List.append twoDlist randomList
    else 
        []

type SW =
    | PushSumMessage of sum: float * weight: float
    | Dummy of string
    | Gossip of string
    | PushSum of string
    | PushSumRelay of string

let bossActor num top algo (mailbox: Actor<_>) = 
    let n = num |> int
    let t = top
    let a = algo
    let mutable count = 0
    let stopWatch = System.Diagnostics.Stopwatch.StartNew()
    let rec loop () = actor {
        let! message = mailbox.Receive ()
        match message with
        |  "initiate" ->    if register.Length = n then
                                let first = register.[r.Next(0,n)]
                                printfn "Number of Nodes = %d\nTopology = %s \nAlgorithm = %s" n t a
                                stopWatch.Restart()
                                if a = "gossip" then
                                    first <! Gossip "gossip"
                                else if a = "push-sum" then
                                    first <! PushSum "pushsum"

        |  "done"  ->   count <- count+1
                        //printfn "Done with %d" count
                        
                        if count = n then
                            stopWatch.Stop()
                            printfn "Converged in %f ms" stopWatch.Elapsed.TotalMilliseconds
        | _ -> failwith "unknown message"
        return! loop ()
    }
    loop ()

let workerActor index bossActor (mailbox: Actor<_>) = 
    let mutable gossipCount = 0
    let mutable flag = 0
    let mutable dummyCount = 0
    let mutable s = index + 1  |>float
    let mutable w = 1 |> float
    let mutable sumEstimate = s/w
    let mutable changeCount = 0
    let listNeighbours = findNeighbours index
    let rec loop () = actor {
        let! message = mailbox.Receive ()
        match message with
        |  Dummy msg -> dummyCount <- dummyCount + 1
                        if dummyCount%10 = 0 then
                            let mutable neighbourIndex = 0
                            if listNeighbours.IsEmpty then
                                neighbourIndex <- r.Next(0,n)
                                while neighbourIndex = index do
                                    neighbourIndex <- r.Next(0,n)
                            else 
                                if listNeighbours.Length = 2 then 
                                    let randomValue = r.Next(0,100)
                                    if randomValue%2 = 0 then
                                        neighbourIndex <- listNeighbours.Item(0)
                                    else
                                        neighbourIndex <- listNeighbours.Item(1)
                                else
                                    let listIndex = r.Next(0, listNeighbours.Length)
                                    neighbourIndex <- listNeighbours.Item(listIndex)
                                while neighbourIndex = index do
                                    let listIndex = r.Next(0, listNeighbours.Length)
                                    neighbourIndex <- listNeighbours.Item(listIndex)
                            register.[neighbourIndex] <! Gossip "gossip"
                        else
                            mailbox.Self <! Dummy "dummy"
                            
        |  Gossip msg-> gossipCount <- gossipCount+1
                        if gossipCount < 10 then
                            let mutable neighbourIndex = 0
                            if listNeighbours.IsEmpty then
                                neighbourIndex <- r.Next(0,n)
                                while neighbourIndex = index do
                                    neighbourIndex <- r.Next(0,n)
                            else
                                if listNeighbours.Length = 2 then 
                                    let randomValue = r.Next(0,100)
                                    if randomValue%2 = 0 then
                                        neighbourIndex <- listNeighbours.Item(0)
                                    else
                                        neighbourIndex <- listNeighbours.Item(1)
                                else
                                    let listIndex = r.Next(0, listNeighbours.Length)                                
                                    neighbourIndex <- listNeighbours.Item(listIndex)

                            register.[neighbourIndex] <! Gossip "gossip"
                        else 
                            if flag = 0 then
                                flag <- 1
                                bossActor <! "done"
                            mailbox.Self <! Dummy "dummy" 
                      
        |  PushSum msg  ->  //printfn "PushSum Message received by Worker from Boss"
                            s <- s/2.0
                            w <- w/2.0
                            sumEstimate <- s/w
                            let mutable neighbourIndex = 0
                            if listNeighbours.IsEmpty then
                                neighbourIndex <- r.Next(0,n)
                                while neighbourIndex = index do
                                    neighbourIndex <- r.Next(0,n)
                            else
                                if listNeighbours.Length = 2 then 
                                    let randomValue = r.Next(0,100)
                                    if randomValue%2 = 0 then
                                        neighbourIndex <- listNeighbours.Item(0)
                                    else
                                        neighbourIndex <- listNeighbours.Item(1)
                                else
                                    let listIndex = r.Next(0, listNeighbours.Length)
                                    neighbourIndex <- listNeighbours.Item(listIndex)

                            register.[neighbourIndex] <! PushSumMessage(s,w)

        |   PushSumRelay msg -> //printfn "Worker %d received a relay message from itself" index
                                dummyCount <- dummyCount + 1
                                if dummyCount%10 = 0 then   
                                    //printfn "Worker %d is realying message at random" index
                                    s <- s + s/2.0
                                    w <- w + w/2.0
                                    s <- s/2.0
                                    w <- w/2.0
                                    let mutable neighbourIndex = 0
                                    if listNeighbours.IsEmpty then
                                        neighbourIndex <- r.Next(0,n)
                                        while neighbourIndex = index do
                                            neighbourIndex <- r.Next(0,n)
                                    else
                                        if listNeighbours.Length = 2 then 
                                            let randomValue = r.Next(0,100)
                                            if randomValue%2 = 0 then
                                                neighbourIndex <- listNeighbours.Item(0)
                                            else
                                                neighbourIndex <- listNeighbours.Item(1)
                                        else
                                            let listIndex = r.Next(0, listNeighbours.Length)
                                            neighbourIndex <- listNeighbours.Item(listIndex)
                                    register.[neighbourIndex] <! PushSumMessage(s, w)
                                else
                                    mailbox.Self <! PushSumRelay "relay"                                        

        |  PushSumMessage(is, iw) ->    if changeCount < 3 then
                                            s <- s+is
                                            w <- w+iw
            
                                            let newSumEstimate = s/w

                                            let diff = newSumEstimate - sumEstimate
                                            let absDiff =  abs diff
                                            let powDiff = 0.0000000001
                                            let floatPowDiff = powDiff |> float

                                            //printfn "Sum Estimate Value is %f New Sum Estimate is %f Absolute Differnce is %f Float Power is %f" sumEstimate newSumEstimate absDiff floatPowDiff

                                            if absDiff < floatPowDiff then                            
                                                changeCount <- changeCount+1
                                            else
                                                changeCount <- 0

                                            if changeCount < 3 then
                                                s <- s/2.0
                                                w <- w/2.0
                                                sumEstimate <- s/w
                                                let mutable neighbourIndex = 0
                                                if listNeighbours.IsEmpty then
                                                    neighbourIndex <- r.Next(0,n)
                                                    while neighbourIndex = index do
                                                        neighbourIndex <- r.Next(0,n)
                                                else
                                                    if listNeighbours.Length = 2 then 
                                                        let randomValue = r.Next(0,100)
                                                        if randomValue%2 = 0 then
                                                            neighbourIndex <- listNeighbours.Item(0)
                                                        else
                                                            neighbourIndex <- listNeighbours.Item(1)
                                                    else
                                                        let listIndex = r.Next(0, listNeighbours.Length)
                                                        neighbourIndex <- listNeighbours.Item(listIndex)

                                                register.[neighbourIndex] <! PushSumMessage(s,w)
                                            else
                                                if flag = 0 then
                                                    flag <- 1 
                                                    bossActor <! "done"
                                                //printfn "Worker %d has reach count of 3 in this iteration" index
                                                mailbox.Self <! PushSumRelay "relay"                      
                                        else
                                            if flag = 0 then
                                                flag <- 1 
                                                bossActor <! "done"
                                            //printfn "Worker %d alredy has count of 3" index
                                            mailbox.Self <! PushSumRelay "relay"                      
                        
       //| _ -> failwith "unknown message"
        return! loop ()
    }
    loop ()


let boss = spawn system "BossActor" <| bossActor n topology algorithm

for i = 0 to n-1 do
    let worker = spawn system ("WorkerActor" + (i.ToString())) <| workerActor i boss
    register <- List.append register [worker]

boss <! "initiate"

System.Console.ReadLine()
system.Terminate()