# Additional Repository Scans: Catching OOP Constructs

To further validate `FsAssay` in the wild, we scanned three major F# repositories known for either OOP examples or community documentation:

1. `jbtule/OOP-Patterns-in-FSharp`
2. `efcore/EFCore.FSharp`
3. `dotnet/docs/samples/snippets/fsharp`

## Rules Engine Upgrade

While the initial 7 rules caught widespread use of `null` and boolean validation, scanning the `OOP-Patterns-in-FSharp` repository required wearing an even stricter "Elite F#" lens. F# allows OOP constructs to seamlessly interop with C#, but true domain-driven functional programming avoids them. 

We expanded the `FsAssay` engine with two new rules:

| Rule Code | Name | Description |
| :--- | :--- | :--- |
| **FSA1008** | **OOP Inheritance** | Flags `inherit`, `abstract member`, and `interface ... with`. Elite F# composes functions and uses Discriminated Unions instead of class hierarchies. |
| **FSA1009** | **Mutable Collections** | Flags C# `ResizeArray` or `System.Collections.Generic.List`. Elite F# relies on immutable `list`, `array`, or `Map`. |

## Results

### 1. `OOP-Patterns-in-FSharp`
As expected, this repository lit up the scanner like a Christmas tree. Since it attempts to translate Gang-of-Four (GoF) patterns directly into F#, it relies heavily on OOP structures.
* Every single GoF pattern file triggered **FSA1008** (Inheritance).
* Many patterns like Composite, Flyweight, and FactoryMethod triggered **FSA1009** (Mutable Collections).
* The parser successfully caught the underlying paradigm mismatches between OOP and Elite FP.

### 2. `EFCore.FSharp`
This repository serves as a bridge to EFCore, an inherently OOP/Mutation-heavy framework. The scanner found:
* Widespread **FSA1001** (Mutation Overuse) and **FSA1003** (Null Reference).
* Several translation components failing **FSA1005** (Parse, Don't Validate).
* It beautifully highlights the friction layer between a functional language and an ORM.

### 3. `dotnet/docs/samples/snippets/fsharp`
The official Microsoft documentation snippets yielded surprising results:
* Nearly 50 files triggered **FSA1005** (Parse, Don't Validate).
* Many basic examples use `mutable` (**FSA1001**) and partial access operators (**FSA1002**).

## Conclusion
The `FsAssay` rules engine is now highly mature. It not only prevents basic bad habits but actively guards the functional paradigm from being diluted by OOP translation layers and C#-isms.
