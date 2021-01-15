#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit

let system = ActorSystem.Create("FSharp")

//Worker Code

type WorkerActor =
    inherit Actor

    override x.OnReceive message =
        match message with
        | :? string as msg ->   let result = msg.Split ' '
                                let first = result.[0] |> int
                                let last = result.[1] |> int
                                let intk = result.[2] |> int
        
                                for i= first to last do
                                    let num = Decimal(i) 
                                    let mutable k = Decimal(intk)
                                    
                                    let y1 = Decimal.Add(num,k)
                                    let y = Decimal.Subtract(y1,Decimal.One)
                                    let two = Decimal(2)
                                    let six = Decimal(6)
                                    let intery1= Decimal.Multiply(two, y)
                                    let y2= Decimal.Add(intery1,Decimal.One) 
                                    let intersum1 = Decimal.Multiply(y,y1)
                                    let sum1 = Decimal.Multiply(intersum1,y2)
                                                                      
                                    let un = Decimal.Subtract(num,Decimal.One)
                                    let interun1= Decimal.Multiply(two, un)
                                    let un1= Decimal.Add(interun1,Decimal.One) 
                                    let intersum2 = Decimal.Multiply(un,num)
                                    let sum2 = Decimal.Multiply(intersum2,un1)                                  
                                    
                                    let sum3 =  Decimal.Subtract(sum1,sum2)
                                    
                                    let diffDecimal = Decimal.Divide(sum3,six)

                                    let diffString = diffDecimal.ToString()

                                    let diff = diffString |> float
                                                                               
                                    let root = sqrt (float diff)  
                                    let root1 = ceil root
                                    
                                    let epsilon = 1.0e-10
                                   
                                    let temp = abs (root - root1)

                                    if (temp < epsilon ) then
                                        printfn "\n%d\n" i
                                                                      
        | _ ->  failwith "unknown message"

//Boss code

let args = fsi.CommandLineArgs
let mutable startnum = args.[1] |> int
let startkString = args.[2]


let mutable noOfCores = pown Environment.ProcessorCount  4 

if( startnum < noOfCores) then
    noOfCores <- startnum

let subProblemSize = startnum/noOfCores  
let mutable firstNum = 1

while ( noOfCores > 0 && firstNum <= startnum) do
    let workerActor = system.ActorOf(Props(typedefof<WorkerActor>, Array.empty))   
    let mutable endNum = firstNum + subProblemSize
    let firstNumString = String.Concat("", firstNum)
    let endNumString = String.Concat("", endNum)
    let message = firstNumString + " " + endNumString + " " + startkString
    workerActor <! message
    firstNum <- endNum + 1  
    noOfCores <- noOfCores - 1      

system.Terminate()