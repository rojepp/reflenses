module Reflenses.Tests

open Reflenses
open Xunit
open System

type CarMake = { Make: string }
type Car =     { Year: int; Color: string; Make: CarMake }
type Person =  { Name: string; BirthDate: DateTime option; Car: Car }

let testperson = { Name = "Robert"
                   BirthDate = None
                   Car  = { Year = 2000
                            Color = "Red" 
                            Make = { Make = "SAAB" } } }

type RecRecordA = { Name: string; Record: RecRecordB option }
and  RecRecordB = { Age:  int;    Record: RecRecordA option }

let recursivedata = { Name = "Test"; Record = Some { Age = 99; Record = Some { Name = "Test2"; Record = None } } }

let (<=>)  (expected:'t) (actual:'t) = Assert.Equal<'t>(expected, actual) 
let (<!=>) (expected:'t) (actual:'t) = Assert.NotEqual<'t>(expected, actual) 
   
[<Fact>]
let ``Lambda properties 1`` () = 
   let expr = <@ (fun (p:Person) -> p.Car.Make.Make) @>
   let properties = getLambdaProps expr
   1 <=> properties.Length
   let first = properties.Head
   3 <=> first.Length
   "Car"  <=> first.[0].Name
   "Make" <=> first.[1].Name
   "Make" <=> first.[2].Name

[<Fact>]
let ``Lambda properties 2`` () = 
   let expr = <@ (fun (p:Person) -> p.Car.Year) @>
   let properties = getLambdaProps expr
   1 <=> properties.Length
   let first = properties.Head
   2 <=> first.Length
   "Car"  <=> first.[0].Name
   "Year" <=> first.[1].Name

[<Fact>]
let ``Lambda tupled properties`` () = 
   let expr = <@ (fun (p:Person) -> p.Car.Year, p.Name, p.Car) @>
   let properties = getLambdaProps expr
   3 <=> properties.Length
   let first = properties.[0]
   2 <=> first.Length
   "Car"  <=> first.[0].Name
   "Year" <=> first.[1].Name

   let second = properties.[1]
   1 <=> second.Length
   "Name" <=> second.[0].Name

   let third = properties.[2]
   1 <=> third.Length
   "Car" <=> third.[0].Name

[<Fact>]
let ``Set single first-level string value`` () = 
   let result = set testperson <@ (fun p -> p.Name) @> "Someone else"
   { testperson with Name = "Someone else" } <=> result

[<Fact>]
let ``Set single third-level string value`` () = 
   let result = set testperson <@ (fun p -> p.Car.Make.Make) @> "Volvo"
   let expected = { testperson with Car = { testperson.Car with Make = {testperson.Car.Make with Make = "Volvo" } } }
   expected <=> result


[<Fact>]
let ``Set single record value`` () = 
   let result = set testperson <@ (fun p -> p.Car.Make) @> { Make = "Volvo" }
   let expected = { testperson with Car = { testperson.Car with Make = { Make = "Volvo" } } }
   expected <=> result

[<Fact>] // Doesn't work at the moment, trouble with option types (Record.Value)
let ``Set single record value recursive`` () = 
   let result = set recursivedata <@ (fun r -> r.Record.Value.Record.Value.Name ) @> "Changed"
   let expected = { Name = "Test"; Record = Some { Age = 99; Record = Some { Name = "Changed"; Record = None } } }
   expected <=> result

[<Fact>]
let ``Set 2-tuple values`` () = 
   let result = set testperson <@ (fun p -> p.Car.Make.Make, p.Car.Year) @> ("Volvo",2012)
   let expected = { testperson with Car = { testperson.Car with Make = { Make = "Volvo" }
                                                                Year = 2012 } }
   expected <=> result

[<Fact>]
let ``Set 3-tuple values`` () = 
   let result = set testperson <@ (fun p -> p.Car.Make.Make, p.Car.Year, p.Name) @> ("Volvo", 2012, "Someone")
   let expected = { testperson with Name = "Someone" 
                                    Car = { testperson.Car with Make = { Make = "Volvo" }
                                                                Year = 2012 } }
   expected <=> result



