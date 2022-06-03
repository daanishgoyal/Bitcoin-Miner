#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote"
printfn("hello from remote1")
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.Remote
open System
open System.Security.Cryptography
open System.Diagnostics
open System.IO
open System.Text

type Message = int*string*string*int*int
type Minedcoin = string*string
type initcon = string*int*List<IActorRef>


let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                
            }
            remote {
                helios.tcp {
                    port = 6001
                    hostname = localhost
                }
            }
        }")

let mysystem = ActorSystem.Create ("BitcoinMine", configuration)
let server_ip = fsi.CommandLineArgs.[1] |> string
let addr = "akka.tcp://RemoteFSharp@" + server_ip + ":6000/user/server"
let numActors = Environment.ProcessorCount


let randomStr = 
    let chars = "abcdefghijklmnopqrstuvwxtzABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789"
    let charsLen = chars.Length
    let random = System.Random()

    fun len -> 
        let randomChars = [|for i in 0..len -> chars.[random.Next(charsLen)]|]
        new System.String(randomChars)

let calculateHash = fun (input_string: string) ->
    let cryp = SHA256Managed.Create()
    let myhsh = StringBuilder()
    let crypt = cryp.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input_string))
    for currByte in crypt do
        myhsh.Append(currByte.ToString("x2"))
    myhsh.ToString()

let calculateHash1 (inputString: string) =
    Encoding.UTF8.GetBytes(inputString)
    |> (new SHA256Managed()).ComputeHash


let fpEncode (bytes:byte[]): string =
    let byteToHex (b:byte) = b.ToString("x2")
    Array.map byteToHex bytes |> String.concat ""
   
  


let checkIfBitcoin = fun (inputstring: string) (nofzeros: int) ->
    let mutable count = 0
    let mutable looperbool = true
    let myhsh = calculateHash(inputstring)
    while looperbool do
        let mychar = myhsh.Chars(count)
        if mychar = '0' && count < myhsh.Length then
            count <- count + 1
        else
            looperbool <- false
    (count >= nofzeros)

type Worker(name) =
    inherit Actor()
        override this.OnReceive message =
            match message with
            | :? Message as msg ->
                let(k, pfix,oldhash, minNonce, maxNonce) = unbox<Message> msg
                for i = minNonce to maxNonce do 
                    let bitcoin = pfix + oldhash + i.ToString()
                    if checkIfBitcoin bitcoin k then
                        let minedcoin = calculateHash bitcoin
                        this.Sender <! (bitcoin, minedcoin)

                  
                      
            | _ -> failwith "unknown message"


// remote actors
let mutable remActors = []
for i in 1..numActors do
    remActors <- List.append remActors [mysystem.ActorOf(Props(typedefof<Worker>, [| string(id) :> obj |]))]



let clientSupervisor = 
    spawn mysystem "client"
    <| fun mailbox ->
        let rec loop() =             
            actor {
                let! msg = mailbox.Receive()
                let sender = mailbox.Sender()
                let echoServer = mysystem.ActorSelection(addr)
                match box msg with
                | :? initcon as message ->
                    echoServer.Tell(message)
                | :? Minedcoin as message ->
                    printfn "got a bitcoin sending to server: %A" message
                    echoServer.Tell(message)
                | _ -> failwith "invalid message passed to the supervisor"
                return! loop() 
            }
        loop()

clientSupervisor <! ("client-1", numActors, remActors)

Console.ReadLine()|>ignore
mysystem.Terminate()
mysystem.WhenTerminated.Wait()
