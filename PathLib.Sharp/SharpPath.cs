using System.Text;

namespace PathLib;

/// <summary>
/// A C# implementation of Python's pathlib functionality
/// Provides object-oriented filesystem path operations
/// </summary>
public class SharpPath : IEquatable<SharpPath>, IComparable<SharpPath>
{
    private readonly string _path;
    
    #region Constructors
    
    /// <summary>
    /// Initialize a new SharpPath with the given path segments
    /// </summary>
    /// <param name="pathSegments">Path segments to combine</param>
    public SharpPath(params string[] pathSegments)
    {
        if (pathSegments == null || pathSegments.Length == 0)
        {
            _path = ".";
        }
        else
        {
            _path = NormalizePath(Path.Combine(pathSegments));
        }
    }
    
    /// <summary>
    /// Initialize a new SharpPath from another SharpPath
    /// </summary>
    /// <param name="other">Another SharpPath instance</param>
    public SharpPath(SharpPath other)
    {
        _path = other?._path ?? ".";
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The path parts as a read-only list
    /// </summary>
    public IReadOnlyList<string> Parts
    {
        get
        {
            if (_path == "." || string.IsNullOrEmpty(_path))
                return new[] { "." };
                
            var parts = new List<string>();
            var root = Path.GetPathRoot(_path);
            
            if (!string.IsNullOrEmpty(root))
            {
                parts.Add(root);
                
                // Get the part after root
                var relativePart = _path.Substring(root.Length);
                if (!string.IsNullOrEmpty(relativePart))
                {
                    parts.AddRange(relativePart.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Where(p => !string.IsNullOrEmpty(p)));
                }
            }
            else
            {
                // Relative path
                parts.AddRange(_path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Where(p => !string.IsNullOrEmpty(p)));
            }
            
            return parts.AsReadOnly();
        }
    }
    
    /// <summary>
    /// The final component of the path
    /// </summary>
    public string Name => Path.GetFileName(_path) ?? "";
    
    /// <summary>
    /// The final component without its suffix
    /// </summary>
    public string Stem => Path.GetFileNameWithoutExtension(_path) ?? "";
    
    /// <summary>
    /// The file extension of the final component
    /// </summary>
    public string Suffix => Path.GetExtension(_path) ?? "";
    
    /// <summary>
    /// All suffixes of the final component
    /// </summary>
    public IReadOnlyList<string> Suffixes
    {
        get
        {
            var name = Name;
            var suffixes = new List<string>();
            var dotIndex = name.IndexOf('.');
            
            while (dotIndex >= 0 && dotIndex < name.Length - 1)
            {
                var suffix = name.Substring(dotIndex);
                var nextDot = suffix.IndexOf('.', 1);
                if (nextDot > 0)
                {
                    suffix = suffix.Substring(0, nextDot);
                }
                suffixes.Add(suffix);
                dotIndex = name.IndexOf('.', dotIndex + suffix.Length);
            }
            
            return suffixes.AsReadOnly();
        }
    }
    
    /// <summary>
    /// The logical parent of the path
    /// </summary>
    public SharpPath Parent => new SharpPath(Path.GetDirectoryName(_path) ?? _path);
    
    /// <summary>
    /// Logical ancestors of the path
    /// </summary>
    public IEnumerable<SharpPath> Parents
    {
        get
        {
            var current = Parent;
            while (current._path != _path && !string.IsNullOrEmpty(current._path))
            {
                yield return current;
                var next = current.Parent;
                if (next._path == current._path) break;
                current = next;
            }
        }
    }
    
    /// <summary>
    /// The drive letter or name, if any
    /// </summary>
    public string Drive => Path.GetPathRoot(_path)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? "";
    
    /// <summary>
    /// The root of the path, if any
    /// </summary>
    public string Root
    {
        get
        {
            var root = Path.GetPathRoot(_path);
            if (string.IsNullOrEmpty(root)) return "";
            
            // On Windows, return just the separator part
            if (OperatingSystem.IsWindows() && root.Length > 1)
            {
                return root.Substring(root.Length - 1);
            }
            return root;
        }
    }
    
    /// <summary>
    /// The concatenation of drive and root
    /// </summary>
    public string Anchor => Path.GetPathRoot(_path) ?? "";
    
    #endregion
    
    #region Operators
    
    /// <summary>
    /// Combine paths using the / operator
    /// </summary>
    public static SharpPath operator /(SharpPath left, string right)
    {
        return left.JoinPath(right);
    }
    
    /// <summary>
    /// Combine paths using the / operator
    /// </summary>
    public static SharpPath operator /(SharpPath left, SharpPath right)
    {
        return left.JoinPath(right._path);
    }
    
    /// <summary>
    /// Implicit conversion from string
    /// </summary>
    public static implicit operator SharpPath(string path)
    {
        return new SharpPath(path);
    }
    
    /// <summary>
    /// Implicit conversion to string
    /// </summary>
    public static implicit operator string(SharpPath path)
    {
        return path._path;
    }
    
    #endregion
    
    #region Path Operations
    
    /// <summary>
    /// Join path segments to this path
    /// </summary>
    public SharpPath JoinPath(params string[] pathSegments)
    {
        if (pathSegments == null || pathSegments.Length == 0)
            return this;
            
        var segments = new string[pathSegments.Length + 1];
        segments[0] = _path;
        Array.Copy(pathSegments, 0, segments, 1, pathSegments.Length);
        
        return new SharpPath(Path.Combine(segments));
    }
    
    /// <summary>
    /// Return whether this path is absolute
    /// </summary>
    public bool IsAbsolute => Path.IsPathRooted(_path);
    
    /// <summary>
    /// Make the path absolute
    /// </summary>
    public SharpPath Absolute()
    {
        return new SharpPath(Path.GetFullPath(_path));
    }
    
    /// <summary>
    /// Resolve the path (make absolute and resolve symlinks)
    /// </summary>
    public SharpPath Resolve()
    {
        try
        {
            return new SharpPath(Path.GetFullPath(_path));
        }
        catch
        {
            return Absolute();
        }
    }
    
    /// <summary>
    /// Return a new path with the name changed
    /// </summary>
    public SharpPath WithName(string name)
    {
        var currentName = Name;
        if (string.IsNullOrEmpty(currentName))
            throw new InvalidOperationException("Path has no name to replace");
            
        var dir = Path.GetDirectoryName(_path);
        return new SharpPath(dir != null ? Path.Combine(dir, name) : name);
    }
    
    /// <summary>
    /// Return a new path with the stem changed
    /// </summary>
    public SharpPath WithStem(string stem)
    {
        return WithName(stem + Suffix);
    }
    
    /// <summary>
    /// Return a new path with the suffix changed
    /// </summary>
    public SharpPath WithSuffix(string suffix)
    {
        return WithName(Stem + suffix);
    }
    
    #endregion
    
    #region File System Queries
    
    /// <summary>
    /// Return True if the path exists
    /// </summary>
    public bool Exists => File.Exists(_path) || Directory.Exists(_path);
    
    /// <summary>
    /// Return True if the path points to a regular file
    /// </summary>
    public bool IsFile => File.Exists(_path);
    
    /// <summary>
    /// Return True if the path points to a directory
    /// </summary>
    public bool IsDirectory => Directory.Exists(_path);
    
    /// <summary>
    /// Return True if the path points to a symbolic link
    /// </summary>
    public bool IsSymlink
    {
        get
        {
            try
            {
                if (IsFile)
                {
                    var fileInfo = new FileInfo(_path);
                    return fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
                }
                if (IsDirectory)
                {
                    var dirInfo = new DirectoryInfo(_path);
                    return dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Get file information
    /// </summary>
    public FileSystemInfo? Stat()
    {
        try
        {
            if (IsFile)
                return new FileInfo(_path);
            if (IsDirectory)
                return new DirectoryInfo(_path);
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
    
    #region File Operations
    
    /// <summary>
    /// Open the file pointed to by the path
    /// </summary>
    public FileStream Open(FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read)
    {
        return new FileStream(_path, mode, access, share);
    }
    
    /// <summary>
    /// Read the entire file as text
    /// </summary>
    public string ReadText(Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return File.ReadAllText(_path, encoding);
    }
    
    /// <summary>
    /// Read the entire file as bytes
    /// </summary>
    public byte[] ReadBytes()
    {
        return File.ReadAllBytes(_path);
    }
    
    /// <summary>
    /// Write text to the file
    /// </summary>
    public void WriteText(string contents, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        File.WriteAllText(_path, contents, encoding);
    }
    
    /// <summary>
    /// Write bytes to the file
    /// </summary>
    public void WriteBytes(byte[] bytes)
    {
        File.WriteAllBytes(_path, bytes);
    }
    
    /// <summary>
    /// Create an empty file or update the modification time
    /// </summary>
    public void Touch()
    {
        if (Exists)
        {
            File.SetLastWriteTime(_path, DateTime.Now);
        }
        else
        {
            // Create parent directories if they don't exist
            var parent = Parent;
            if (!parent.Exists)
                parent.MakeDirectory(createParents: true);
                
            File.Create(_path).Dispose();
        }
    }
    
    #endregion
    
    #region Directory Operations
    
    /// <summary>
    /// Iterate over directory contents
    /// </summary>
    public IEnumerable<SharpPath> IterateDirectory()
    {
        if (!IsDirectory)
            throw new InvalidOperationException("Path is not a directory");
            
        foreach (var entry in Directory.EnumerateFileSystemEntries(_path))
        {
            yield return new SharpPath(entry);
        }
    }
    
    /// <summary>
    /// Glob pattern matching
    /// </summary>
    public IEnumerable<SharpPath> Glob(string pattern)
    {
        if (!IsDirectory)
            throw new InvalidOperationException("Path is not a directory");
            
        var searchPattern = pattern.Replace('/', Path.DirectorySeparatorChar);
        
        // Handle recursive patterns
        if (pattern.Contains("**"))
        {
            return Directory.EnumerateFiles(_path, "*", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateDirectories(_path, "*", SearchOption.AllDirectories))
                .Where(p => MatchesGlobPattern(p, pattern))
                .Select(p => new SharpPath(p));
        }
        else
        {
            return Directory.EnumerateFiles(_path, searchPattern, SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateDirectories(_path, searchPattern, SearchOption.TopDirectoryOnly))
                .Select(p => new SharpPath(p));
        }
    }
    
    /// <summary>
    /// Recursive glob pattern matching
    /// </summary>
    public IEnumerable<SharpPath> RecursiveGlob(string pattern)
    {
        return Glob("**/" + pattern);
    }
    
    /// <summary>
    /// Create directory
    /// </summary>
    public void MakeDirectory(bool createParents = false, bool existOk = false)
    {
        try
        {
            if (createParents)
            {
                Directory.CreateDirectory(_path);
            }
            else
            {
                Directory.CreateDirectory(_path);
            }
        }
        catch (IOException) when (existOk && IsDirectory)
        {
            // Directory already exists and existOk is true
        }
    }
    
    /// <summary>
    /// Remove directory (must be empty)
    /// </summary>
    public void RemoveDirectory()
    {
        Directory.Delete(_path, false);
    }
    
    #endregion
    
    #region File System Operations
    
    /// <summary>
    /// Rename or move this file/directory
    /// </summary>
    public SharpPath Rename(string newPath)
    {
        var destination = new SharpPath(newPath);
        
        if (IsFile)
        {
            File.Move(_path, destination._path);
        }
        else if (IsDirectory)
        {
            Directory.Move(_path, destination._path);
        }
        else
        {
            throw new FileNotFoundException($"Path not found: {_path}");
        }
        
        return destination;
    }
    
    /// <summary>
    /// Replace this file/directory (overwrite if exists)
    /// </summary>
    public SharpPath Replace(string newPath)
    {
        var destination = new SharpPath(newPath);
        
        if (IsFile)
        {
            File.Move(_path, destination._path, overwrite: true);
        }
        else if (IsDirectory)
        {
            if (destination.Exists)
                Directory.Delete(destination._path, true);
            Directory.Move(_path, destination._path);
        }
        else
        {
            throw new FileNotFoundException($"Path not found: {_path}");
        }
        
        return destination;
    }
    
    /// <summary>
    /// Remove this file or symbolic link
    /// </summary>
    public void Unlink(bool missingOk = false)
    {
        try
        {
            if (IsFile || IsSymlink)
            {
                File.Delete(_path);
            }
            else if (!missingOk)
            {
                throw new FileNotFoundException($"File not found: {_path}");
            }
        }
        catch (FileNotFoundException) when (missingOk)
        {
            // Ignore if missingOk is true
        }
    }
    
    /// <summary>
    /// Create a symbolic link pointing to target
    /// </summary>
    public void SymlinkTo(string target)
    {
        if (OperatingSystem.IsWindows())
        {
            // Determine if target is a directory
            var targetPath = new SharpPath(target);
            if (targetPath.IsDirectory)
            {
                Directory.CreateSymbolicLink(_path, target);
            }
            else
            {
                File.CreateSymbolicLink(_path, target);
            }
        }
        else
        {
            File.CreateSymbolicLink(_path, target);
        }
    }
    
    #endregion
    
    #region Static Methods
    
    /// <summary>
    /// Return the current working directory
    /// </summary>
    public static SharpPath CurrentDirectory => new SharpPath(Directory.GetCurrentDirectory());
    
    /// <summary>
    /// Return the user's home directory
    /// </summary>
    public static SharpPath Home => new SharpPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    
    #endregion
    
    #region Helper Methods
    
    private bool MatchesGlobPattern(string path, string pattern)
    {
        // Simple glob pattern matching implementation
        // This is a basic implementation and could be enhanced
        pattern = pattern.Replace("**", "*");
        var fileName = Path.GetFileName(path);
        var patternName = Path.GetFileName(pattern);
        return MatchesWildcard(fileName, patternName);
    }
    
    private bool MatchesWildcard(string text, string pattern)
    {
        // Simple wildcard matching for * and ?
        int textIndex = 0;
        int patternIndex = 0;
        int textLength = text.Length;
        int patternLength = pattern.Length;
        
        while (textIndex < textLength && patternIndex < patternLength)
        {
            if (pattern[patternIndex] == '*')
            {
                patternIndex++;
                if (patternIndex == patternLength)
                    return true;
                    
                while (textIndex < textLength)
                {
                    if (MatchesWildcard(text.Substring(textIndex), pattern.Substring(patternIndex)))
                        return true;
                    textIndex++;
                }
                return false;
            }
            else if (pattern[patternIndex] == '?' || pattern[patternIndex] == text[textIndex])
            {
                textIndex++;
                patternIndex++;
            }
            else
            {
                return false;
            }
        }
        
        // Handle remaining asterisks in pattern
        while (patternIndex < patternLength && pattern[patternIndex] == '*')
            patternIndex++;
            
        return textIndex == textLength && patternIndex == patternLength;
    }
    
    #endregion
    
    #region Helper Methods
    
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return ".";
        
        // Handle relative paths
        if (path == "." || path == "..") return path;
        
        // Normalize separators and remove redundant separators
        path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        
        return path;
    }
    
    #endregion
    
    #region Object Overrides
    
    /// <summary>
    /// Returns the string representation of the path
    /// </summary>
    public override string ToString() => _path;
    
    /// <summary>
    /// Determines whether the specified object is equal to the current path
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is SharpPath other && Equals(other);
    }
    
    /// <summary>
    /// Determines whether the specified path is equal to the current path
    /// </summary>
    public bool Equals(SharpPath? other)
    {
        if (other is null) return false;
        
        // Use case-insensitive comparison on Windows
        var comparison = OperatingSystem.IsWindows() 
            ? StringComparison.OrdinalIgnoreCase 
            : StringComparison.Ordinal;
            
        return string.Equals(_path, other._path, comparison);
    }
    
    /// <summary>
    /// Returns the hash code for the current path
    /// </summary>
    public override int GetHashCode()
    {
        var comparison = OperatingSystem.IsWindows() 
            ? StringComparison.OrdinalIgnoreCase 
            : StringComparison.Ordinal;
            
        return _path.GetHashCode(comparison);
    }
    
    /// <summary>
    /// Compares the current path with another path
    /// </summary>
    public int CompareTo(SharpPath? other)
    {
        if (other is null) return 1;
        
        var comparison = OperatingSystem.IsWindows() 
            ? StringComparison.OrdinalIgnoreCase 
            : StringComparison.Ordinal;
            
        return string.Compare(_path, other._path, comparison);
    }
    
    /// <summary>
    /// Determines whether two paths are equal
    /// </summary>
    public static bool operator ==(SharpPath? left, SharpPath? right)
    {
        return EqualityComparer<SharpPath>.Default.Equals(left, right);
    }
    
    /// <summary>
    /// Determines whether two paths are not equal
    /// </summary>
    public static bool operator !=(SharpPath? left, SharpPath? right)
    {
        return !(left == right);
    }
    
    #endregion
}
