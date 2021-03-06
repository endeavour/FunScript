﻿module (*internal*) FunJS.Quote

open System.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Reflection

type CallType =
   | MethodCall
   | UnionCaseConstructorCall
   | ConstructorCall

let private flags =
   BindingFlags.Public |||  
   BindingFlags.NonPublic |||
   BindingFlags.Instance |||
   BindingFlags.Static

module PatternsExt = 
   // In qutoations generated by type provider code, records and
   // objects are mixed up a bit (???) so this corrects the behaviour.

   let (|NewObject|_|) = function
      | Patterns.NewObject(ctor, args) ->
         if FSharpType.IsRecord ctor.DeclaringType then None
         else Some(ctor, args)
      | _ -> None
   
   let (|NewRecord|_|) = function
      | Patterns.NewRecord(typ, args) -> Some(typ, args)
      | Patterns.NewObject(ctor, args) ->
         if FSharpType.IsRecord ctor.DeclaringType then 
           Some(ctor.DeclaringType, args)
         else None
      | _ -> None

let getCaseMethodInfo (uci:UnionCaseInfo) =
   let unionType = uci.DeclaringType
   match unionType.GetMember(uci.Name, flags) with
   | [| :? System.Type as caseType |] -> 
      caseType.GetConstructors(flags).[0]
      :> MethodBase, caseType
   | [| :? MethodInfo as mi |] -> mi :> MethodBase, unionType
   | [| :? PropertyInfo as pi |] -> pi.GetGetMethod(true) :> MethodBase, unionType
   | _ ->
      match unionType.GetMember("New" + uci.Name, flags) with
      | [| :? MethodInfo as mi |] -> mi :> MethodBase, unionType
      | [| :? PropertyInfo as pi |] -> pi.GetGetMethod(true) :> MethodBase, unionType
      | _ -> failwith "never"

let specialOp (mb:MethodBase) =
  if mb.IsStatic && mb.IsGenericMethod && mb.Name.StartsWith "op_" then
      let declaringType = mb.GetGenericArguments().[0]
      let flags = BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Static
      let methods = declaringType.GetMethods(flags)
      methods |> Array.tryFind(fun mi -> mi.Name = mb.Name) 
   else None

let tryToMethodBase = function
   | Patterns.Call(obj,mi,args) ->
      Some(obj, mi :> MethodBase, args, MethodCall)
   | Patterns.PropertyGet(obj,pi,args) -> 
      Some(obj, pi.GetGetMethod(true) :> MethodBase, args, MethodCall) 
   | Patterns.PropertySet(obj,pi,args,v) -> 
      Some(obj, pi.GetSetMethod(true) :> MethodBase, List.append args [v], MethodCall)
   | Patterns.NewUnionCase(uci, exprs) -> 
      Some(None, fst <| getCaseMethodInfo uci, exprs, UnionCaseConstructorCall)
   | PatternsExt.NewObject(ci, exprs) ->
      Some(None, ci :> MethodBase, exprs, ConstructorCall)
   | _ -> None

let (|MethodBase|_|) = tryToMethodBase

let tryToMethodBaseFromLambdas expr =
   match expr with
   | MethodBase mb
   | DerivedPatterns.Lambdas(_, MethodBase mb) -> Some mb
   | _ -> None

let private mostGeneric(mi:MethodInfo)  = 
   if mi.IsGenericMethod then mi.GetGenericMethodDefinition()
   else mi

let toMethodInfoFromLambdas expr =
   match tryToMethodBaseFromLambdas expr with
   | Some (_, (:? MethodInfo as mi), _, callType) -> mostGeneric mi, callType
   | Some _ | None -> failwith "Expected a method/property call/get/set wrapped in a lambda"

let toMethodBaseFromLambdas expr =
   match tryToMethodBaseFromLambdas expr with
   //| Some (_, (:? MethodInfo as mi), _, callType) -> mostGeneric mi :> MethodBase, callType
   | Some (_, mi, _, callType) -> mi, callType
   | None -> failwith "Expected a method/property call/get/set wrapped in a lambda"

