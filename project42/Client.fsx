#r "nuget: Suave" 

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
open System.Net
open System
open System.Collections.Generic


let conn:Connection =
    { socketBinding = SocketBinding.create IPAddress.IPv6Loopback 8080us
      transport     = null
      bufferManager = null
      lineBuffer    = ArraySegment<byte>()
      segments      = new LinkedList<BufferSegment>()
      lineBufferCount = 0 }

let uri = "ws://localhost:8080/websocket"
let webSocket = new WebSocket(conn,None)
let input = System.Console.ReadLine()
do! webSocket.send Text input false
