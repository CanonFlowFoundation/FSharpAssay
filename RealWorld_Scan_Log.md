# FsAssay `fsharp-realworld-example-app` Output Log

The following is the raw terminal output of executing the `FsAssay.Runner` project against the cloned [gothinkster/fsharp-realworld-example-app](https://github.com/gothinkster/fsharp-realworld-example-app) repository.

```text
Scanning repository: /root/fsharp-realworld

❌ /root/fsharp-realworld/suaveio_dotnetcore2/lib/Convert.fs
   └── [FSA1003] Null Reference: Avoid 'null'. Use 'Option' types to represent missing values.

❌ /root/fsharp-realworld/suaveio_dotnetcore2/lib/BsonDocConverter.fs
   └── [FSA1002] Partial Access: Do not use Option.get, .Value, or .Head. Use pattern matching.

❌ /root/fsharp-realworld/suaveio_dotnetcore2/lib/Actions.fs
   └── [FSA1002] Partial Access: Do not use Option.get, .Value, or .Head. Use pattern matching.

❌ /root/fsharp-realworld/suaveio_dotnetcore2/lib/Auth.fs
   └── [FSA1005] Parse, Don't Validate: Functions should return Result<ParsedType, Error> rather than a boolean validity flag.

❌ /root/fsharp-realworld/suaveio_dotnetcore2/lib/JsonWebToken.fs
   └── [FSA1003] Null Reference: Avoid 'null'. Use 'Option' types to represent missing values.
   └── [FSA1005] Parse, Don't Validate: Functions should return Result<ParsedType, Error> rather than a boolean validity flag.

❌ /root/fsharp-realworld/suaveio_dotnetcore2/lib/Effects.fs
   └── [FSA1002] Partial Access: Do not use Option.get, .Value, or .Head. Use pattern matching.

Scan complete!
```
