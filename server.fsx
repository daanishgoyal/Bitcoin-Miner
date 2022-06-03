#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.Remote"
printfn("hello from remote2")
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

let mutable CoinCount:int = 0


let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }
            remote {
                helios.tcp {
                    port = 6000
                    hostname = 192.168.0.234
                }
            }
        }")


let mysystem = ActorSystem.Create ("BitcoinMine", configuration)



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
                let(k, pfix, oldhash, minNonce, maxNonce) = unbox<Message> msg
                for i = minNonce to maxNonce do 
                    let bitcoin = pfix + oldhash + i.ToString()
                    if checkIfBitcoin bitcoin k then
                        let minedcoin = calculateHash bitcoin
                        printfn " ---------------BITCOIN FOUND------------------"
                        printfn "String : %s ----- Hash : %s -----" bitcoin minedcoin


       

let mutable numRemoteActors = 0
let mutable remoteActors =[]
let pfix = "daanishgoyal"
let inihash = calculateHash(pfix)
let v = 1
let pcount1 = Environment.ProcessorCount
let numActors = pcount1
let numZeros = fsi.CommandLineArgs.[1] |> int
//let numZeros = 3
let mutable max = 50000000
let work = (max - v)/(numActors)


let serverSupervisor = 
    spawn mysystem "server"
    <| fun mailbox ->
        let rec loop() =             
            actor {
                let! msg = mailbox.Receive()
                let sender = mailbox.Sender()

                match box msg with
                | :? Message as msg ->
                    let(k, pfix, oldhash, minNonce, maxNonce) = unbox<Message> msg
                    let worksize = (maxNonce - minNonce)/(numActors)
                    for i=1 to numActors do
                         mysystem.ActorOf(Props(typedefof<Worker>, [| string(i) :> obj |])) <! (k, pfix, oldhash, (i-1)*worksize, i*worksize)

                | :? Minedcoin as message ->
                                  let (str, hash) = unbox<Minedcoin> message
                                  printfn "%s %s " str hash  

                | :? initcon as message ->
                    let (clientStr, nofWorkers, myactors) = unbox<initcon> message
                    numRemoteActors <- nofWorkers
                    remoteActors <- remoteActors |> List.append myactors
                    let newmax = max
                    max <- max + work*numRemoteActors
                    if numRemoteActors > 0 then
                        for i=1 to numRemoteActors do
                            let rfix = inihash + (newmax |> string)
                            remoteActors.Item(i-1) <! (numZeros, pfix,rfix, (i-1)*work+1, i*work)

                return! loop() 
            }
        loop()
                    

serverSupervisor <! (numZeros, pfix, inihash, v, max)

Console.ReadLine()|>ignore
mysystem.Terminate()
mysystem.WhenTerminated.Wait()

0

