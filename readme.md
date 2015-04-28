Reflenses
-----------

Experimental lenses using reflection to avoid boilerplate. 


The syntax for making a copy of F# immutable records is a bit verbose when 
you have nested records. This code is an attempt to change that. 
Inspired by [Mauricio Scheffers post][mausch].

```fsharp
type Pet = { Animal: string; Name: string }
type Person = { FirstName: string; LastName: string; FavoriteColor: string; Pet: Pet option }
let original = { FirstName = "Robert" 
                 LastName = "Jeppesen" 
                 FavoriteColor = "Blue"
                 Pet = Some { Animal = "Dog"; Name = "Raggedy" } }
// Flat hierarchy, not bad at all.
let newversion = { original with FavoriteColor = "Yellow" }
// Nested copy, more cumbersome, and how does it 
// really work with option types?
// You have to replace all of it. 
// Better pray that newversion.Pet was a 'Some'!
let newversion2 = { newversion with Pet = Some { newversion.Pet with Name = "ABC" } }

With Reflenses, you can create a new record by doing this: 
```fsharp
// The simple case is not a win, more complex than 
// the built-in method. 
let newversion = set original <@ (fun p -> p.FavoriteColor) @> "Yellow"
// Replace all of the pet
let newversion2 = set newversion <@ (fun p -> p.Pet) @> (Some { Animal = "Monkey"; Name = "Spot" })
// Just replace the pet name
let renamedpet = set newversion <@ (fun p -> p.Pet.Value.Name) @> "Spot (still a dog)"
```

All of these are type-checked, thanks to the power of F# quotations. 
I'm not quite happy with the syntax for setting partial options,
at the moment. Share your ideas!

You can also set several options at once. This is done as a tuple
in the quotation and input value:

```fsharp
let newversion = set original <@ (fun p -> p.FavoriteColor, p.Pet.Value.Animal) @> "Black","Elephant"
```

This is also fully type checked. 

## Performance

This is way slower than the built-in way of creating new records. 
Before optimizing, a test run of 10.000 records would take ~12 seconds
on my 4 year old laptop. After optmizing the most obvious bits, 
it's down to ~3 seconds, where the native F# version is ~20 ms.
If you hoist the expression out of the loop, you get a massive speedup. 
This quotation literal doesn't seem to get cached by F#, even
when it does not capture any locals. 

```fsharp
let expr = <@ (fun f -> f.Car.Make.Make) @>
  let inline time f iterations = 
    let sw = System.Diagnostics.Stopwatch()
    sw.Start()
    for i in 1 .. iterations do
       f () |> ignore
    sw.Stop()
    printfn "%A" sw.ElapsedMilliseconds
 
time (fun () -> set robert expr "Volvo")  10000 // Expression out of loop, fast
time (fun () -> set robert <@ (fun f -> f.Car.Make.Make) @> "Volvo" ) 10000// Slow
time (fun () -> { robert with Car = { robert.Car with Make = { robert.Car.Make with Make = "Volvo" } } } ) 10000 // Fastest
     
104 ms
2314 ms
20 ms
```

Code is in F# with no other dependencies for runtime. Tested by XUnit.

[mausch]: http://bugsquash.blogspot.se/2011/11/lenses-in-f.html