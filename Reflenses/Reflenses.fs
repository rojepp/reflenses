﻿//module Reflenses 
module Reflenses

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.ExprShape
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Reflection
open System.Reflection

let private memoize f = 
    let cache = System.Collections.Generic.Dictionary<_, _>()
    fun x -> 
        let mutable res = Unchecked.defaultof<_>
        match cache.TryGetValue(x, &res) with
        | true -> res
        | false -> let res = f x
                   cache.Add(x, res)
                   res

// Never flushed from cache. Should not be a problem for most apps.
let private recordMakers  = memoize FSharpValue.PreComputeRecordConstructor
let private recordReaders = memoize FSharpValue.PreComputeRecordReader
let private recordFields  = memoize FSharpType.GetRecordFields
let private tupleReader   = memoize FSharpValue.PreComputeTupleReader
let private recordTypes   = memoize FSharpType.IsRecord
let private optionTypes   = memoize (fun (t: System.Type) -> let n = t.FullName 
                                                             n.IndexOf("FSharpOption") >= 0) // Such a hack!

let inline private (|OptionValue|_|) (owner:obj, prop:PropertyInfo) =
   if optionTypes(prop.DeclaringType) then Some (owner, prop) else None

let inline private (|Record|_|) (owner:obj, prop:PropertyInfo) = 
   if recordTypes prop.DeclaringType then Some (owner,prop) else None

let getLambdaProps (expr: Expr) =
   let rec loop expr acc =
      match expr with
      | ShapeLambda (_,expr) -> loop expr []
      | NewTuple exprs -> exprs |> List.map (fun x -> loop x [] |> List.head)
      | PropertyGet(expr, propOrValInfo, _) ->
         let acc = propOrValInfo :: acc
         match expr with | Some x -> loop x acc | None -> [acc]
      | x -> [acc]
   loop expr []

let inline getValues (props: PropertyInfo list) (owner:obj) = 
   let rec loop (props: PropertyInfo list) owner acc= 
      match props with 
      | x :: xs -> 
         let newowner = if owner = null then (None :> obj) else x.GetValue(owner, null)
         loop xs newowner ((owner, x) :: acc)
      | []      -> acc
   loop props owner []

/// <summary>Make a copy of an F# Record type</summary>
/// <param name="root">The record to make a copy of</param>
/// <param name="expr">An F# Quotation expression that returns the properties to be changed. Has the signature <c>'r -> 't</c>
/// where <c>'r</c> is the record type and <c>'t</c> is the property type. <c>'t</c> may also be tupled and
/// contain several properties</param>
/// <param name="root">The value(s) to set for this Expr</param>
/// <returns>A new record with updated values</returns>
let set<'r,'t> (root:'r) (expr:Expr<'r -> 't>) (value:'t) = 
   let rec loop (props: (obj*PropertyInfo) list) (setval: obj) =
      match props with 
      | OptionValue(owner, prop) :: xs -> 
          let ty = prop.DeclaringType
          let v = System.Activator.CreateInstance(ty, setval)
          loop xs v
      | Record(owner,prop) :: xs ->
         let ownertype = prop.DeclaringType
         let newval = 
            let vals = recordReaders(ownertype)(owner)
            let fields = recordFields(ownertype)
            let idx = fields |> Array.findIndex (fun f -> f.Name = prop.Name)
            vals.[idx] <- setval
            let r = recordMakers(ownertype)(vals)
            r
         loop xs newval
      | _ -> setval

   let tuplevalues = match FSharpType.IsTuple typeof<'t> with
                     | true -> tupleReader(typeof<'t>)(value) |> List.ofArray
                     | _    -> [value]
   let props = getLambdaProps expr |> List.zip tuplevalues
   
   // Tupled stuff: Kind of a brute force approach. Per tuple, create a new record, use that as seed for next tuple until done. 
   
   let mutable v = root
   for value, proplist in props do
      let withValues = getValues proplist v
      v <- loop withValues value :?> 'r
   v
