[<FunJS.JS>]
module FunJS.Core.Events
open System

[<AllowNullLiteral>]
type IObserver<'T> =
    abstract OnNext : value : 'T -> unit
    abstract OnError : error : exn -> unit
    abstract OnCompleted : unit -> unit

[<AllowNullLiteral>]
type IObservable<'T> =
    abstract Subscribe : observer : IObserver<'T> -> System.IDisposable

type ActionObserver<'T> (onNext : 'T -> unit, onError : exn -> unit, onCompleted : unit -> unit) =    
   new(onNext : 'T -> unit) = ActionObserver<'T>(onNext, (fun e -> ()), (fun () -> ()))

   interface IObserver<'T> with
     member this.OnNext v = onNext v
     member this.OnError e = onError e
     member this.OnCompleted() = onCompleted()

type IDelegateEvent<'Delegate when 'Delegate :> System.Delegate > =
   abstract AddHandler: handler:'Delegate -> unit
   abstract RemoveHandler: handler:'Delegate -> unit 

type IEvent<'Delegate,'Args when 'Delegate : delegate<'Args,unit> and 'Delegate :> System.Delegate > =
   inherit IDelegateEvent<'Delegate>
   inherit IObservable<'Args>

type Handler<'Args> = delegate of sender:obj * args:'Args -> unit 

type IEvent<'Args> = IEvent<Handler<'Args>, 'Args>

[<FunJS.JSEmit("return {0} !== {1};"); FunJS.Inline>]
let inline (!==) a b = failwith "never";

type ActionDisposable(f) =
   interface IDisposable with
      member this.Dispose() = f()

type private PublishedEvent<'T>(delegates:(Handler<'T>)[] ref) =
   let addHandler handler =
      let newArray = !delegates |> Array.append [|handler|]
      delegates := newArray

   let removeHandler handler = 
      let newArray = !delegates |> Array.filter (fun x -> handler !== x)
      delegates := newArray

   interface IEvent<'T> with      
      member this.AddHandler (f : Handler<'T>) = addHandler f
      member this.RemoveHandler (f : Handler<'T>) = removeHandler f

      member this.Subscribe(observer) =
         let f = new Handler<_>(fun sender args -> observer.OnNext args)
         addHandler f
         let unsubscribe = fun () -> removeHandler f
         upcast new ActionDisposable(unsubscribe)

and Event<'T>() =
   let delegates = ref Array.empty<Handler<'T>>   

   member this.Trigger args =
      !delegates |> Array.iter(fun f -> f.Invoke(null, args))

   member this.Publish = PublishedEvent<'T>(delegates) :> IEvent<'T>

[<FunJS.JSEmit("{0}.addEventListener({1},{2});")>]
let private addEventListener (element:obj) (eventName:string) (handler:Handler<'T>) : unit = failwith "never"

[<FunJS.JSEmit("{0}.removeEventListener({1},{2});")>]
let private removeEventListener (element:obj) (eventName:string) (handler:Handler<'T>) : unit = failwith "never"

///// A wrapper around DOM Events
//type DomEvent<'T> (element, eventName) as this =   
//   interface IEvent<'T> with
//      member this.Subscribe (observer:IObserver<'T>) : IDisposable =
//         let f = Handler<'T>(fun sender e -> observer.OnNext(e))
//         addEventListener element eventName (f)
//         let unsubscribe = fun() -> removeEventListener element eventName f
//         upcast new ActionDisposable(unsubscribe)
//      member this.AddHandler f = addEventListener element eventName f
//      member this.RemoveHandler f = removeEventListener element eventName f
//
//type private ActionEvent<'T>(addHandler,removeHandler,subscribe) =
//   interface IEvent<'T> with
//      member this.AddHandler handler = addHandler handler
//      member this.RemoveHandler handler = removeHandler handler
//      member this.Subscribe observer = subscribe observer
//
//[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
//[<RequireQualifiedAccess>]
//module Event =
//    [<CompiledName("Create")>]
//    let create<'T>() = 
//        let ev = new Event<'T>() 
//        ev.Trigger, ev.Publish
//
//    [<CompiledName("Map")>]
//    let map f (w: IEvent<'Delegate,'T>) =
//        let ev = new Event<_>() 
//        w.Add(fun x -> ev.Trigger(f x));
//        ev.Publish
//
//    [<CompiledName("Filter")>]
//    let filter f (w: IEvent<'Delegate,'T>) =
//        let ev = new Event<_>() 
//        w.Add(fun x -> if f x then ev.Trigger x);
//        ev.Publish
//
//    [<CompiledName("Partition")>]
//    let partition f (w: IEvent<'Delegate,'T>) =
//        let ev1 = new Event<_>() 
//        let ev2 = new Event<_>() 
//        w.Add(fun x -> if f x then ev1.Trigger x else ev2.Trigger x);
//        ev1.Publish,ev2.Publish
//
//    [<CompiledName("Choose")>]
//    let choose f (w: IEvent<'Delegate,'T>) =
//        let ev = new Event<_>() 
//        w.Add(fun x -> match f x with None -> () | Some r -> ev.Trigger r);
//        ev.Publish
//
//    [<CompiledName("Scan")>]
//    let scan f z (w: IEvent<'Delegate,'T>) =
//        let state = ref z
//        let ev = new Event<_>() 
//        w.Add(fun msg ->
//             let z = !state
//             let z = f z msg
//             state := z; 
//             ev.Trigger(z));
//        ev.Publish
//
//    [<CompiledName("Add")>]
//    let add f (w: IEvent<'Delegate,'T>) = w.Add(f)
//
//    [<CompiledName("Pairwise")>]
//    let pairwise (inp : IEvent<'Delegate,'T>) : IEvent<'T * 'T> = 
//        let ev = new Event<'T * 'T>() 
//        let lastArgs = ref None
//        inp.Add(fun args2 -> 
//            (match !lastArgs with 
//             | None -> () 
//             | Some args1 -> ev.Trigger(args1,args2));
//            lastArgs := Some args2); 
//
//        ev.Publish
//
//    [<CompiledName("Merge")>]
//    let merge (w1: IEvent<'Del1,'T>) (w2: IEvent<'Del2,'T>) =
//        let ev = new Event<_>() 
//        w1.Add(fun x -> ev.Trigger(x));
//        w2.Add(fun x -> ev.Trigger(x));
//        ev.Publish
//
//    [<CompiledName("Split")>]
//    let split (f : 'T -> Choice<'U1,'U2>) (w: IEvent<'Delegate,'T>) =
//        let ev1 = new Event<_>() 
//        let ev2 = new Event<_>() 
//        w.Add(fun x -> match f x with Choice1Of2 y -> ev1.Trigger(y) | Choice2Of2 z -> ev2.Trigger(z));
//        ev1.Publish,ev2.Publish
