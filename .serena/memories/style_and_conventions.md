# Style and conventions

- C# with file-scoped namespaces, 4-space indentation, CRLF per `.editorconfig`.
- Public APIs use PascalCase; interfaces are prefixed with `I`.
- Managers wrap ETABS COM objects and generally catch non-`EtabsException` exceptions, rethrowing as domain-specific `EtabsException` with context.
- Logging uses `Microsoft.Extensions.Logging`; nullable reference types are enabled in test projects and likely expected in newer code.
- Library targets both `net8.0` and `net10.0`.
- ETABS COM calls generally follow the pattern: allocate `ref` inputs, call COM method, check non-zero return code, throw `EtabsException`, return typed model/result.
- Tests that require live ETABS use skip guards such as `Skip.If(!ETABSWrapper.IsRunning(), ...)`.