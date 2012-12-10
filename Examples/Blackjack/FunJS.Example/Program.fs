[<ReflectedDefinition>]
module Program

open System
open FunJS
//open FunJS.TypeScript
open FunJS.Core.Events

//type lib = FunJS.TypeScript.Api< @"..\..\Typings\lib.d.ts" >

[<FunJS.JSEmit("console.log ({0});")>]
let log (x : string) : unit = failwith "never"

//[<FunJS.JSEmit("return document.getElementById('body');")>]
//let getBody() : obj = failwith "X"   

type SomeFunc<'T> = delegate of 'T -> unit

let main() =
   let someLambda = fun (x:int) -> ()
   //someLambda 10
   let someFunc = SomeFunc<int>(fun x -> ())
   ()
   //someFunc.Invoke(1)
//   let body = getBody()
////   let body = lib.document.getElementsByTagName("body").item(0.)
//   let y = new DomEvent<obj>(body, "click") :> FunJS.Core.Events.IEvent<obj>
//   let observer = Core.Events.ActionObserver((fun x -> log "CLICKED!"),(fun e -> ()),(fun () -> ()))
//   //let observer = Core.Events.ActionObserver((fun x -> log "CLICKED!")) // stack overflow for some reason?
//   let sub = y.Subscribe(observer)




   //sub.Dispose()

//   Foo()
//   
//   ()

   

//   let event = FunJS.Core.Events.Event<int>()
//   
//   let pub1 = event.Publish
//   let pub2 = event.Publish   
//   let pub3 = event.Publish
//
//   let handler1 (x:int) = log ("ONE:" + x.ToString())
//
//   let handler = Handler<int> (fun sender e -> handler1 e)
//   pub1.AddHandler(handler)
//   
//
//
//   event.Trigger(123)

   

// Compile
let source = <@@ main() @@> |> Compiler.compileWithoutReturn 
let filename = "twitter-example.js"
System.IO.File.Delete filename
System.IO.File.WriteAllText(filename, source)
source|> printfn "%A"
System.Console.ReadLine() |> ignore