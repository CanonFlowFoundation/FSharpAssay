# Real-World Scan Results: `fsharp-realworld-example-app`

We cloned the official [gothinkster/fsharp-realworld-example-app](https://github.com/gothinkster/fsharp-realworld-example-app) repository and unleashed `FsAssay` v0.1 on it.

## Findings

Despite being an idiomatic example application, `FsAssay` successfully detected several "C#-ish" anti-patterns and violations of the Elite F# ruleset deep within the project:

### 1. Partial Access Violations (FSA1002)
The use of `.Value`, `Option.get`, or `.Head` without pattern matching safety.

* `suaveio_dotnetcore2/lib/BsonDocConverter.fs`
* `suaveio_dotnetcore2/lib/Actions.fs`
* `suaveio_dotnetcore2/lib/Effects.fs`

### 2. Parse, Don't Validate Violations (FSA1005)
Functions acting as boolean gates (`isValid -> bool`) instead of structural parsing boundaries (`Result<Parsed, Error>`).

* `suaveio_dotnetcore2/lib/Auth.fs`
* `suaveio_dotnetcore2/lib/JsonWebToken.fs`

### 3. Null Reference Violations (FSA1003)
Explicit use of `null` assignments instead of F#'s native `Option<'T>`.

* `suaveio_dotnetcore2/lib/Convert.fs`
* `suaveio_dotnetcore2/lib/JsonWebToken.fs`

## Conclusion

The scanner proves that even in highly visible, community-curated "Real World" examples, F# developers inadvertently fallback to imperative C# paradigms like `null` and boolean validation. `FsAssay` successfully pinpoints exactly where the domain can be refactored into a purer, safer functional core!
