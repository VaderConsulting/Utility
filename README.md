# Utility

A lightweight, cross-platform extension methods library for .NET Standard 2.0 and .NET Core. Provides generic serialization helpers, XML file persistence with file-locking, reflection utilities, and type-conversion extensions.

**Source last updated:** 2020-12-20
**Initiated:** 2018-12-29 · **Target Frameworks:** .NET Standard 2.0, .NET Core 2.0/2.1/3.1

---

## API Reference

```csharp
// Deep copy via binary serialization
T copy = myObject.DeepCopy<T>();

// XML serialize to file (with cooperative .Flag file-locking)
bool ok = myObject.SaveToFile<T>("data.xml");

// XML deserialize from file
T result = default(T).LoadFromFile<T>("data.xml");

// Remove all invocations from a named event
control.ClearEventInvocations("Click");

// Safe string-to-type conversion
int n  = "42".As<int>();
bool b = "true".As<bool>();
int len = maybeNullString.LengthOf();  // returns 0 for null
```

---

## Dependencies

No external NuGet packages. BCL only.

## Solution structure

| Project | Language | Type | Purpose |
|---------|----------|------|---------|
| `Utility.Core` (`Utility.Core.csproj`) | C# | Class library (netstandard2.0; netcoreapp2.0 target left commented out) | `Extensions.cs` generic, serialization, reflection, and string helpers |

## How to open

Open `Utility.Core.csproj` in Visual Studio 2017 or later and build. There is no `.sln` in this tree.

## Requirements

- Visual Studio 2017 or later, .NET Standard 2.0 (.NET Core SDK 2.x or later)

## Attribution and provenance

Working copy from my Historical Dev folder.

Working copy from my Development folder `Utility`.

## License

MIT © 2026 VaderConsulting for Dave Robinson's code. See `LICENSE`.

