# HexaEngine.Sappho

A high-performance, source-generated binary serialization library for .NET with support for polymorphism, cyclic references, and efficient memory management. Part of the [HexaEngine](https://github.com/HexaEngine/HexaEngine) game engine ecosystem.

## Overview

HexaEngine.Sappho is a specialized serialization library designed for game development scenarios where performance is critical. Originally developed as a submodule for the HexaEngine game engine, it provides efficient binary serialization suitable for:

- **Game State Persistence**: Save/load game states with complex object graphs
- **Network Communication**: Fast serialization for multiplayer game data
- **Asset Management**: Efficient binary formats for game assets
- **Scene Serialization**: Handling complex game object hierarchies with circular references

While designed for game engines, Sappho is a standalone library suitable for any .NET application requiring high-performance binary serialization.

## Features

- **🚀 High Performance**: Optimized for speed with unsafe code and minimal allocations
- **🔄 Cyclic References**: Automatic handling of circular object references
- **🎭 Polymorphism**: First-class support for polymorphic serialization and deserialization
- **⚙️ Source Generation**: Uses C# source generators for compile-time serialization code generation
- **🔧 Flexible**: Support for primitives, strings, enums, structs, and complex object graphs
- **💾 Efficient**: Binary format with endian-aware operations
- **🎯 Type-Safe**: Strongly-typed serialization with compile-time validation

## Installation

```bash
dotnet add package HexaEngine.Sappho
```

## Quick Start

### 1. Mark Your Classes

Use the `[SapphoObject]` attribute to mark classes or structs for serialization:

```csharp
using HexaEngine.Sappho;

[SapphoObject]
public partial class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public float Height { get; set; }
}
```

> **Note**: Classes must be marked as `partial` to allow the source generator to add serialization code.

### 2. Serialize and Deserialize

```csharp
// Create a serialization context
var context = new SapphoSerializationContext();

// Create an object
var person = new Person 
{ 
    Name = "John Doe", 
    Age = 30, 
    Height = 5.9f 
};

// Serialize
using var writer = new SapphoWriter(context);
writer.WriteObject(person);

// Deserialize
var reader = new SapphoReader(writer.Buffer, writer.Count, context);
var deserializedPerson = reader.ReadObject<Person>();
```

## Advanced Features

### Polymorphic Serialization

Use the `[SapphoPolymorphic]` attribute to enable polymorphic serialization:

```csharp
[SapphoObject]
public partial class Animal
{
    public string Name { get; set; }
}

[SapphoObject]
public partial class Dog : Animal
{
    public string Breed { get; set; }
}

[SapphoObject]
public partial class Cat : Animal
{
    public int LivesRemaining { get; set; }
}

[SapphoObject]
public partial class Zoo
{
    [SapphoPolymorphic]
    public Animal[] Animals { get; set; }
}
```

When using polymorphic types, register them with the serialization context:

```csharp
context.RegisterType<Animal>(Animal.SapphoTypeId);
context.RegisterType<Dog>(Dog.SapphoTypeId);
context.RegisterType<Cat>(Cat.SapphoTypeId);

var zoo = new Zoo
{
    Animals = new Animal[]
    {
        new Dog { Name = "Buddy", Breed = "Golden Retriever" },
        new Cat { Name = "Whiskers", LivesRemaining = 9 }
    }
};
```

### Cyclic References

The library automatically handles cyclic references:

```csharp
[SapphoObject]
public partial class Node
{
    public string Name { get; set; }
    public Node? Next { get; set; }
}

var node1 = new Node { Name = "Node 1" };
var node2 = new Node { Name = "Node 2" };
node1.Next = node2;
node2.Next = node1; // Cyclic reference

// Serialization will handle this correctly
```

### Ignoring Properties

Use `[SapphoIgnore]` to exclude properties from serialization:

```csharp
[SapphoObject]
public partial class CachedData
{
    public string Data { get; set; }
    
    [SapphoIgnore]
    public string CachedValue { get; set; } // Not serialized
}
```

### Custom Member Attributes

Use `[SapphoMember]` for fine-grained control over serialization:

```csharp
[SapphoObject]
public partial class CustomClass
{
    [SapphoMember]
    public string ImportantField { get; set; }
}
```

## Supported Types

- **Primitives**: `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `bool`, `char`
- **Strings**: UTF-8 encoded with automatic null handling
- **Enums**: All enum types with any underlying integral type
- **Structs**: Value types that implement `ISapphoSerializable<T>`
- **Classes**: Reference types with automatic reference tracking
- **Arrays**: Single-dimensional arrays of supported types
- **Nullable Types**: Full support for nullable reference and value types

## Performance

HexaEngine.Sappho is designed for high-performance scenarios:

```csharp
// From the Example project:
// Average serialization time: ~XXX ns (depends on object complexity)
```

The library uses:
- Unsafe code for direct memory access
- Stack allocations for small buffers
- Efficient buffer management with dynamic resizing
- Endian-aware binary format

## Architecture

### Core Components

- **SapphoWriter**: Handles serialization to a binary buffer
- **SapphoReader**: Handles deserialization from a binary buffer
- **SapphoSerializationContext**: Manages type registration and reference tracking
- **Source Generator**: Automatically generates serialization code at compile time

### Interfaces

- **`ISapphoSerializable<T>`**: Base interface for serializable types
- **`ISapphoSerializer<T>`**: Custom serializer interface
- **`ISapphoInstanceId`**: Optional interface for custom instance ID tracking

## Project Structure

```
HexaEngine.Sappho/
├── HexaEngine.Sappho/           # Core serialization library
├── HexaEngine.Sappho.Analyzer/  # Source generator
├── HexaEngine.Sappho.Tests/     # Unit tests
└── Example/                      # Example usage
```

## Requirements

- **.NET 9.0** or later (core library)
- **.NET Standard 2.0** or later (analyzer)
- **C# 12** or later (for source generation features)

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

The test suite includes comprehensive tests for:
- Primitive type serialization
- String handling (including Unicode and empty strings)
- Enum serialization
- Struct serialization
- Polymorphic serialization
- Cyclic reference handling
- Null value handling

## Contributing

Contributions are welcome! Please ensure:
- All tests pass
- Code follows existing conventions
- New features include appropriate tests

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/HexaEngine/HexaEngine.Sappho/blob/main/LICENSE.txt) file for more details.

## Credits

Part of the HexaEngine ecosystem.

Developed as a submodule for [HexaEngine](https://github.com/HexaEngine/HexaEngine) - a modern game engine for .NET.

## See Also

- [HexaEngine](https://github.com/HexaEngine/HexaEngine) - The main game engine project
- [Hexa.NET.Utilities](https://github.com/HexaEngine/Hexa.NET.Utilities) - Utility library used by Sappho

---

**Do you listen to girl in red?**