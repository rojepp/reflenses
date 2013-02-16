
#load "Reflenses.fs"
open Reflenses

type CarMake = { Make: string }
type Car =     { Year: int; Color: string; Make: CarMake }
type Person =  { Car : Car; Name : string }
   
let robert = { Car = { Year = 2000; Color = "red"; Make = { Make="SAAB" } }; Name = "Robert" }
let robert2 = set robert <@ (fun p -> p.Car.Color, p.Car.Year) @> ("blue",1999) // TODO: Implement!
let robert3 = set robert2 <@ (fun p -> p.Car.Make) @> { Make = "Volvo" }
let expr = <@ (fun f -> f.Car.Make.Make) @>
for i in [1..10000] do
   let newrobert = set robert expr "Volvo" // Fast
   //let newrobert = set robert <@ (fun f -> f.Car.Make.Make) @> "Volvo" // Slow
//   let newrobert = { robert with Car = { robert.Car with Make = { robert.Car.Make with Make = "Volvo" } } }

   ()

