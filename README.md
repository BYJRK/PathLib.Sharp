# PathLib.Sharp

[中文文档](README.zh-CN.md)

## Overview

PathLib.Sharp is a C# library that mimics the functionality and API design of Python's `pathlib` module. It provides an object-oriented approach to filesystem path operations, making path handling more intuitive and safer than traditional string manipulation.

## Features

### 1. Path Construction and Representation

- Multiple path construction options
- Automatic handling of platform-specific path separators
- Path join operator (`/`)
- Implicit string conversion

### 2. Path Properties

- `Parts` - Array of path components
- `Name` - File name (including extension)
- `Stem` - File name without extension
- `Suffix` - File extension
- `Suffixes` - All extensions (supports compound extensions like `.tar.gz`)
- `Parent` - Parent directory
- `Parents` - All ancestor directories
- `Drive` - Drive letter (Windows)
- `Root` - Root path
- `Anchor` - Drive + root path

### 3. Path Manipulation Methods

- `JoinPath()` - Concatenate path segments
- `WithName()` - Return path with a different file name
- `WithStem()` - Return path with a different stem
- `WithSuffix()` - Return path with a different extension
- `Absolute()` - Get the absolute path
- `Resolve()` - Resolve the path (handles symlinks)
- `IsAbsolute` - Whether the path is absolute

### 4. Filesystem Queries

- `Exists` - Whether the path exists
- `IsFile` - Whether the path is a file
- `IsDirectory` - Whether the path is a directory
- `IsSymlink` - Whether the path is a symbolic link
- `Stat()` - Get filesystem metadata

### 5. File Operations

- `Open()` - Open a file stream
- `ReadText()` / `WriteText()` - Read/write text files
- `ReadBytes()` / `WriteBytes()` - Read/write binary files
- `Touch()` - Create an empty file or update its modification time

### 6. Directory Operations

- `IterateDirectory()` - Iterate over directory contents
- `Glob()` - Pattern-based file search
- `RecursiveGlob()` - Recursive pattern matching
- `MakeDirectory()` - Create a directory
- `RemoveDirectory()` - Remove an empty directory

### 7. Filesystem Operations

- `Rename()` - Rename / move a path
- `Replace()` - Replace a path (force overwrite)
- `Unlink()` - Delete a file
- `SymlinkTo()` - Create a symbolic link

### 8. Static Members

- `SharpPath.CurrentDirectory` - Current working directory
- `SharpPath.Home` - User home directory

## Usage Examples

### Basic Path Operations

```csharp
using PathLib;

// Create a path
var path = new SharpPath("documents", "projects", "myfile.txt");
Console.WriteLine(path); // documents\projects\myfile.txt (Windows)

// Path properties
Console.WriteLine($"Name:      {path.Name}");    // myfile.txt
Console.WriteLine($"Stem:      {path.Stem}");    // myfile
Console.WriteLine($"Suffix:    {path.Suffix}");  // .txt
Console.WriteLine($"Parent:    {path.Parent}");  // documents\projects
```

### Joining Paths

```csharp
var basePath = new SharpPath("C:", "Users");
var fullPath = basePath / "username" / "Documents" / "file.txt";
// C:\Users\username\Documents\file.txt
```

### Modifying Paths

```csharp
var originalPath = new SharpPath("document.pdf");
var withNewName = originalPath.WithName("report.pdf");   // report.pdf
var withNewStem = originalPath.WithStem("backup");       // backup.pdf
var withNewExt  = originalPath.WithSuffix(".txt");       // document.txt
```

### File Operations

```csharp
var textFile = new SharpPath("example.txt");

// Write to file
textFile.WriteText("Hello, PathLib.Sharp!");

// Read from file
string content = textFile.ReadText();

// Inspect the file
if (textFile.Exists && textFile.IsFile)
{
    Console.WriteLine($"File size: {textFile.Stat()?.Length} bytes");
}
```

### Directory Operations

```csharp
var directory = new SharpPath("my_folder");

// Create directory
directory.MakeDirectory();

// Iterate directory
foreach (var item in directory.IterateDirectory())
{
    if (item.IsFile)
        Console.WriteLine($"File:      {item.Name}");
    else if (item.IsDirectory)
        Console.WriteLine($"Directory: {item.Name}");
}

// Pattern matching
var txtFiles = directory.Glob("*.txt");
```

### Advanced Features

```csharp
// Recursively search for all C# files
var projectDir = new SharpPath("my_project");
var csFiles = projectDir.Glob("**/*.cs");

// Create a symbolic link
var target = new SharpPath("original_file.txt");
var link   = new SharpPath("link_to_file.txt");
link.SymlinkTo(target);

// Safe file deletion
var tempFile = new SharpPath("temp.log");
tempFile.Unlink(missingOk: true); // Does not throw if file is missing
```

## Platform Compatibility

- ✅ Windows (.NET 8.0+)
- ✅ Linux (.NET 8.0+)
- ✅ macOS (.NET 8.0+)

## Comparison with Python's pathlib

| Python pathlib | PathLib.Sharp | Description |
|----------------|---------------|-------------|
| `Path('a', 'b')` | `new SharpPath("a", "b")` | Path construction |
| `path / 'subdir'` | `path / "subdir"` | Path joining |
| `path.name` | `path.Name` | File name |
| `path.stem` | `path.Stem` | File stem |
| `path.suffix` | `path.Suffix` | Extension |
| `path.parent` | `path.Parent` | Parent directory |
| `path.exists()` | `path.Exists` | Existence check |
| `path.is_file()` | `path.IsFile` | File check |
| `path.read_text()` | `path.ReadText()` | Read text |
| `path.glob('*.txt')` | `path.Glob("*.txt")` | Pattern matching |

## Installation

Install via NuGet:

```bash
dotnet add package CodeWine.PathLib.Sharp
```

Or build from source:

```bash
# Clone the repository
git clone https://github.com/BYJRK/PathLib.Sharp
cd PathLib.Sharp

# Build
dotnet build

# Run tests
dotnet test

# Pack NuGet package
dotnet pack
```

## Test Coverage

The project contains 66 test cases covering:

- ✅ Constructor tests (4 tests)
- ✅ Property access tests (11 tests)
- ✅ Operator overload tests (4 tests)
- ✅ Path manipulation tests (6 tests)
- ✅ Filesystem query tests (4 tests)
- ✅ File operation tests (6 tests)
- ✅ Directory operation tests (6 tests)
- ✅ Filesystem operation tests (5 tests)
- ✅ Static method tests (2 tests)
- ✅ Equality and comparison tests (5 tests)
- ✅ Wildcard matching tests (5 tests)
- ✅ Edge cases and error handling tests (8 tests)

## License

[MIT License](LICENSE)

## Contributing

Contributions are welcome! Feel free to open a Pull Request or create an Issue to report bugs or suggest new features.

## Notes

- Path operations follow the conventions of the current operating system
- Path comparisons are case-insensitive on Windows and case-sensitive on Unix/Linux
- Symbolic link operations require appropriate system permissions
- Some operations may fail due to filesystem permissions — handle exceptions accordingly
