//module Reflenses 
module Reflenses

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.ExprShape
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Reflection
open System.Reflection
// Old, broken syntax
//let rec getProps (expr: Expr) acc =
//   match expr with
//   | PropertyGet(expr, propOrValInfo, exli) ->
//      let acc = propOrValInfo :: acc
//      exli |> List.iter (fun l -> printfn "%A" l)
//      match expr with | Some x -> getProps x acc | None -> acc
//   | x -> acc

// Before tuple support
//let getLambdaProps (expr: Expr) =
//   let rec loop expr acc =
//      match expr with
//      | ShapeLambda (v,expr) -> loop expr []
//      | PropertyGet(expr, propOrValInfo, exli) ->
//         let acc = propOrValInfo :: acc
//         match expr with | Some x -> loop x acc | None -> acc
//      | x -> acc
//   loop expr []


let rec getLambdaProps (expr: Expr) =
   let rec loop expr acc =
      match expr with
      | ShapeLambda (v,expr) -> loop expr []
      | NewTuple exprs -> exprs |> List.map (fun x -> loop x [] |> List.head)
      | PropertyGet(expr, propOrValInfo, exli) ->
         let acc = propOrValInfo :: acc
         match expr with | Some x -> loop x acc | None -> [acc]
      | x -> [acc]
   loop expr []

let getValues (props: PropertyInfo list) (owner:obj) = 
   let rec loop (props: PropertyInfo list) owner acc= 
      match props with 
      | x :: xs -> 
         let newowner = x.GetValue owner
         loop xs newowner ((owner, x) :: acc)
      | []      -> acc
   loop props owner []

let private memoize f = 
    let cache = System.Collections.Generic.Dictionary<_, _>()
    fun x -> 
        match cache.TryGetValue x with
        | true, v  -> v
        | false, _ -> let res = f x
                      cache.Add(x, res)
                      res

// Never flushed from cache. Should not be a problem for most apps.
let private recordMakers  = memoize FSharpValue.PreComputeRecordConstructor
let private recordReaders = memoize FSharpValue.PreComputeRecordReader
let private recordFields  = memoize FSharpType.GetRecordFields
let private tupleReader   = memoize FSharpValue.PreComputeTupleReader

let set<'r,'t> (root:'r) (expr:Expr<'r -> 't>) (value:'t) = 
   let rec loop (props: (obj*PropertyInfo) list) setval =
      match props with 
      //| (owner,prop) :: [] -> setval
      | (owner,prop) :: xs ->
         let ownertype = owner.GetType()
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

      
