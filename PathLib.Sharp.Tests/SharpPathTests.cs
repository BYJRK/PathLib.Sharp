namespace PathLib.Sharp.Tests;

public class SharpPathTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDirectory;
    private readonly List<string> _filesToCleanup = new();
    private readonly List<string> _directoriesToCleanup = new();

    public SharpPathTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"PathLibSharp_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        _directoriesToCleanup.Add(_tempDirectory);
    }

    public void Dispose()
    {
        // Cleanup files
        foreach (var file in _filesToCleanup.Where(File.Exists))
        {
            try
            {
                File.Delete(file);
            }
            catch { }
        }

        // Cleanup directories
        foreach (
            var dir in _directoriesToCleanup
                .Where(Directory.Exists)
                .OrderByDescending(d => d.Length)
        )
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch { }
        }
    }

    private string GetTempPath(string relativePath = "")
    {
        var fullPath = string.IsNullOrEmpty(relativePath)
            ? _tempDirectory
            : Path.Combine(_tempDirectory, relativePath);

        if (Path.GetExtension(fullPath) != "")
            _filesToCleanup.Add(fullPath);
        else if (!_directoriesToCleanup.Contains(fullPath))
            _directoriesToCleanup.Add(fullPath);

        return fullPath;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_EmptyPath_ReturnsCurrentDirectory()
    {
        var path = new SharpPath();
        Assert.Equal(".", path.ToString());
    }

    [Fact]
    public void Constructor_NullPath_ReturnsCurrentDirectory()
    {
        var path = new SharpPath((string[])null!);
        Assert.Equal(".", path.ToString());
    }

    [Fact]
    public void Constructor_MultipleSegments_CombinesCorrectly()
    {
        var path = new SharpPath("folder1", "folder2", "file.txt");
        var expected = Path.Combine("folder1", "folder2", "file.txt");
        Assert.Equal(expected, path.ToString());
    }

    [Fact]
    public void Constructor_FromAnotherSharpPath_CopiesCorrectly()
    {
        var original = new SharpPath("test", "path");
        var copy = new SharpPath(original);
        Assert.Equal(original.ToString(), copy.ToString());
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void Parts_SimplePath_ReturnsCorrectParts()
    {
        var path = new SharpPath("folder", "subfolder", "file.txt");
        var parts = path.Parts;

        Assert.Contains("folder", parts);
        Assert.Contains("subfolder", parts);
        Assert.Contains("file.txt", parts);
    }

    [Fact]
    public void Parts_CurrentDirectory_ReturnsDot()
    {
        var path = new SharpPath(".");
        var parts = path.Parts;

        Assert.Single(parts);
        Assert.Equal(".", parts[0]);
    }

    [Fact]
    public void Name_FileWithExtension_ReturnsCorrectName()
    {
        var path = new SharpPath("folder", "file.txt");
        Assert.Equal("file.txt", path.Name);
    }

    [Fact]
    public void Name_DirectoryPath_ReturnsDirectoryName()
    {
        var path = new SharpPath("folder", "subfolder");
        Assert.Equal("subfolder", path.Name);
    }

    [Fact]
    public void Stem_FileWithExtension_ReturnsNameWithoutExtension()
    {
        var path = new SharpPath("document.pdf");
        Assert.Equal("document", path.Stem);
    }

    [Fact]
    public void Stem_FileWithMultipleExtensions_ReturnsCorrectStem()
    {
        var path = new SharpPath("archive.tar.gz");
        Assert.Equal("archive.tar", path.Stem);
    }

    [Fact]
    public void Suffix_FileWithExtension_ReturnsExtension()
    {
        var path = new SharpPath("document.pdf");
        Assert.Equal(".pdf", path.Suffix);
    }

    [Fact]
    public void Suffixes_FileWithMultipleExtensions_ReturnsAllSuffixes()
    {
        var path = new SharpPath("archive.tar.gz");
        var suffixes = path.Suffixes;

        Assert.Contains(".tar", suffixes);
        Assert.Contains(".gz", suffixes);
    }

    [Fact]
    public void Parent_FilePath_ReturnsParentDirectory()
    {
        var path = new SharpPath("folder", "file.txt");
        var parent = path.Parent;

        Assert.Equal("folder", parent.ToString());
    }

    [Fact]
    public void Parents_NestedPath_ReturnsAllAncestors()
    {
        var path = new SharpPath("a", "b", "c", "file.txt");
        var parents = path.Parents.ToList();

        Assert.Contains(parents, p => p.ToString().EndsWith("c"));
        Assert.Contains(parents, p => p.ToString().EndsWith(Path.Combine("b", "c")));
    }

    [Theory]
    [InlineData("C:\\folder\\file.txt", "C:")]
    [InlineData("/home/user/file.txt", "")]
    public void Drive_DifferentPaths_ReturnsCorrectDrive(string pathStr, string expectedDrive)
    {
        if (OperatingSystem.IsWindows() || !pathStr.StartsWith("C:"))
        {
            var path = new SharpPath(pathStr);
            Assert.Equal(expectedDrive, path.Drive);
        }
    }

    #endregion

    #region Operators Tests

    [Fact]
    public void DivisionOperator_WithString_CombinesCorrectly()
    {
        var basePath = new SharpPath("folder");
        var combined = basePath / "file.txt";

        var expected = Path.Combine("folder", "file.txt");
        Assert.Equal(expected, combined.ToString());
    }

    [Fact]
    public void DivisionOperator_WithSharpPath_CombinesCorrectly()
    {
        var basePath = new SharpPath("folder");
        var subPath = new SharpPath("subfolder", "file.txt");
        var combined = basePath / subPath;

        var expected = Path.Combine("folder", Path.Combine("subfolder", "file.txt"));
        Assert.Equal(expected, combined.ToString());
    }

    [Fact]
    public void ImplicitConversion_FromString_WorksCorrectly()
    {
        SharpPath path = "test/path";
        var expected = Path.Combine("test", "path");
        Assert.Equal(expected, path.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_WorksCorrectly()
    {
        var path = new SharpPath("test", "path");
        string pathStr = path;
        Assert.Equal(path.ToString(), pathStr);
    }

    #endregion

    #region Path Operations Tests

    [Fact]
    public void JoinPath_MultipleSegments_CombinesCorrectly()
    {
        var path = new SharpPath("base");
        var result = path.JoinPath("folder1", "folder2", "file.txt");

        var expected = Path.Combine("base", "folder1", "folder2", "file.txt");
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void IsAbsolute_AbsolutePath_ReturnsTrue()
    {
        var absolutePath = OperatingSystem.IsWindows()
            ? new SharpPath("C:", "folder", "file.txt")
            : new SharpPath("/", "home", "user", "file.txt");

        Assert.True(absolutePath.IsAbsolute);
    }

    [Fact]
    public void IsAbsolute_RelativePath_ReturnsFalse()
    {
        var relativePath = new SharpPath("folder", "file.txt");
        Assert.False(relativePath.IsAbsolute);
    }

    [Fact]
    public void WithName_ValidName_ReturnsPathWithNewName()
    {
        var path = new SharpPath("folder", "oldfile.txt");
        var newPath = path.WithName("newfile.txt");

        Assert.Equal("newfile.txt", newPath.Name);
        Assert.Equal(path.Parent.ToString(), newPath.Parent.ToString());
    }

    [Fact]
    public void WithStem_ValidStem_ReturnsPathWithNewStem()
    {
        var path = new SharpPath("document.pdf");
        var newPath = path.WithStem("report");

        Assert.Equal("report.pdf", newPath.Name);
    }

    [Fact]
    public void WithSuffix_ValidSuffix_ReturnsPathWithNewSuffix()
    {
        var path = new SharpPath("document.pdf");
        var newPath = path.WithSuffix(".txt");

        Assert.Equal("document.txt", newPath.Name);
    }

    #endregion

    #region File System Queries Tests

    [Fact]
    public void Exists_ExistingFile_ReturnsTrue()
    {
        var testFile = GetTempPath("testfile.txt");
        File.WriteAllText(testFile, "test content");

        var path = new SharpPath(testFile);
        Assert.True(path.Exists);
    }

    [Fact]
    public void Exists_NonExistingFile_ReturnsFalse()
    {
        var path = new SharpPath(GetTempPath("nonexistent.txt"));
        Assert.False(path.Exists);
    }

    [Fact]
    public void IsFile_ExistingFile_ReturnsTrue()
    {
        var testFile = GetTempPath("testfile.txt");
        File.WriteAllText(testFile, "test content");

        var path = new SharpPath(testFile);
        Assert.True(path.IsFile);
        Assert.False(path.IsDirectory);
    }

    [Fact]
    public void IsDirectory_ExistingDirectory_ReturnsTrue()
    {
        var testDir = GetTempPath("testdir");
        Directory.CreateDirectory(testDir);

        var path = new SharpPath(testDir);
        Assert.True(path.IsDirectory);
        Assert.False(path.IsFile);
    }

    [Fact]
    public void Stat_ExistingFile_ReturnsFileInfo()
    {
        var testFile = GetTempPath("testfile.txt");
        File.WriteAllText(testFile, "test content");

        var path = new SharpPath(testFile);
        var stat = path.Stat();

        Assert.NotNull(stat);
        Assert.IsType<FileInfo>(stat);
    }

    #endregion

    #region File Operations Tests

    [Fact]
    public void ReadText_WriteText_WorksCorrectly()
    {
        var testFile = GetTempPath("textfile.txt");
        var path = new SharpPath(testFile);
        var content = "Hello, PathLib.Sharp!";

        path.WriteText(content);
        var readContent = path.ReadText();

        Assert.Equal(content, readContent);
    }

    [Fact]
    public void ReadBytes_WriteBytes_WorksCorrectly()
    {
        var testFile = GetTempPath("binaryfile.bin");
        var path = new SharpPath(testFile);
        var data = Encoding.UTF8.GetBytes("Binary data test");

        path.WriteBytes(data);
        var readData = path.ReadBytes();

        Assert.Equal(data, readData);
    }

    [Fact]
    public void Touch_NonExistingFile_CreatesFile()
    {
        var testFile = GetTempPath("touchfile.txt");
        var path = new SharpPath(testFile);

        Assert.False(path.Exists);
        path.Touch();
        Assert.True(path.Exists);
        Assert.True(path.IsFile);
    }

    [Fact]
    public void Touch_ExistingFile_UpdatesModificationTime()
    {
        var testFile = GetTempPath("existing.txt");
        File.WriteAllText(testFile, "test");
        var path = new SharpPath(testFile);

        var originalTime = File.GetLastWriteTime(testFile);
        System.Threading.Thread.Sleep(1100); // Ensure time difference

        path.Touch();
        var newTime = File.GetLastWriteTime(testFile);

        Assert.True(newTime > originalTime);
    }

    [Fact]
    public void Open_ValidFile_ReturnsFileStream()
    {
        var testFile = GetTempPath("streamfile.txt");
        File.WriteAllText(testFile, "test content");
        var path = new SharpPath(testFile);

        using var stream = path.Open();
        Assert.NotNull(stream);
        Assert.True(stream.CanRead);
    }

    #endregion

    #region Directory Operations Tests

    [Fact]
    public void MakeDirectory_NewDirectory_CreatesDirectory()
    {
        var testDir = GetTempPath("newdirectory");
        var path = new SharpPath(testDir);

        Assert.False(path.Exists);
        path.MakeDirectory();
        Assert.True(path.Exists);
        Assert.True(path.IsDirectory);
    }

    [Fact]
    public void MakeDirectory_WithParents_CreatesNestedDirectories()
    {
        var testDir = GetTempPath(Path.Combine("level1", "level2", "level3"));
        var path = new SharpPath(testDir);

        path.MakeDirectory(createParents: true);
        Assert.True(path.Exists);
        Assert.True(path.IsDirectory);
    }

    [Fact]
    public void IterateDirectory_WithFiles_ReturnsAllEntries()
    {
        var testDir = GetTempPath("iterdir");
        Directory.CreateDirectory(testDir);

        // Create test files and subdirectory
        File.WriteAllText(Path.Combine(testDir, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(testDir, "file2.txt"), "content2");
        Directory.CreateDirectory(Path.Combine(testDir, "subdir"));

        var path = new SharpPath(testDir);
        var entries = path.IterateDirectory().ToList();

        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, e => e.Name == "file1.txt");
        Assert.Contains(entries, e => e.Name == "file2.txt");
        Assert.Contains(entries, e => e.Name == "subdir");
    }

    [Fact]
    public void Glob_SimplePattern_ReturnsMatchingFiles()
    {
        var testDir = GetTempPath("globdir");
        Directory.CreateDirectory(testDir);

        // Create test files
        File.WriteAllText(Path.Combine(testDir, "test1.txt"), "content");
        File.WriteAllText(Path.Combine(testDir, "test2.txt"), "content");
        File.WriteAllText(Path.Combine(testDir, "other.log"), "content");

        var path = new SharpPath(testDir);
        var matches = path.Glob("*.txt").ToList();

        Assert.Equal(2, matches.Count);
        Assert.All(matches, m => Assert.EndsWith(".txt", m.Name));
    }

    [Fact]
    public void RemoveDirectory_EmptyDirectory_RemovesSuccessfully()
    {
        var testDir = GetTempPath("removedir");
        Directory.CreateDirectory(testDir);
        var path = new SharpPath(testDir);

        Assert.True(path.Exists);
        path.RemoveDirectory();
        Assert.False(path.Exists);
    }

    #endregion

    #region File System Operations Tests

    [Fact]
    public void Rename_ExistingFile_RenamesSuccessfully()
    {
        var sourceFile = GetTempPath("source.txt");
        var targetFile = GetTempPath("target.txt");

        File.WriteAllText(sourceFile, "test content");
        var sourcePath = new SharpPath(sourceFile);

        var result = sourcePath.Rename(targetFile);

        Assert.False(File.Exists(sourceFile));
        Assert.True(File.Exists(targetFile));
        Assert.Equal(targetFile, result.ToString());
    }

    [Fact]
    public void Replace_ExistingFile_ReplacesSuccessfully()
    {
        var sourceFile = GetTempPath("source.txt");
        var targetFile = GetTempPath("target.txt");

        File.WriteAllText(sourceFile, "new content");
        File.WriteAllText(targetFile, "old content");

        var sourcePath = new SharpPath(sourceFile);
        var result = sourcePath.Replace(targetFile);

        Assert.False(File.Exists(sourceFile));
        Assert.True(File.Exists(targetFile));
        Assert.Equal("new content", File.ReadAllText(targetFile));
    }

    [Fact]
    public void Unlink_ExistingFile_RemovesFile()
    {
        var testFile = GetTempPath("unlinkfile.txt");
        File.WriteAllText(testFile, "content");
        var path = new SharpPath(testFile);

        Assert.True(path.Exists);
        path.Unlink();
        Assert.False(path.Exists);
    }

    [Fact]
    public void Unlink_NonExistingFileWithMissingOk_DoesNotThrow()
    {
        var path = new SharpPath(GetTempPath("nonexistent.txt"));

        // Should not throw
        path.Unlink(missingOk: true);
    }

    #endregion

    #region Static Methods Tests

    [Fact]
    public void CurrentDirectory_ReturnsValidPath()
    {
        var currentDir = SharpPath.CurrentDirectory;

        Assert.NotNull(currentDir);
        Assert.True(currentDir.Exists);
        Assert.True(currentDir.IsDirectory);
        Assert.True(currentDir.IsAbsolute);
    }

    [Fact]
    public void Home_ReturnsValidPath()
    {
        var home = SharpPath.Home;

        Assert.NotNull(home);
        Assert.True(home.IsAbsolute);
        // Note: Home directory might not always exist in all test environments
    }

    #endregion

    #region Equality and Comparison Tests

    [Fact]
    public void Equals_SamePaths_ReturnsTrue()
    {
        var path1 = new SharpPath("folder", "file.txt");
        var path2 = new SharpPath("folder", "file.txt");

        Assert.True(path1.Equals(path2));
        Assert.True(path1 == path2);
        Assert.False(path1 != path2);
    }

    [Fact]
    public void Equals_DifferentPaths_ReturnsFalse()
    {
        var path1 = new SharpPath("folder1", "file.txt");
        var path2 = new SharpPath("folder2", "file.txt");

        Assert.False(path1.Equals(path2));
        Assert.False(path1 == path2);
        Assert.True(path1 != path2);
    }

    [Fact]
    public void GetHashCode_SamePaths_ReturnsSameHash()
    {
        var path1 = new SharpPath("folder", "file.txt");
        var path2 = new SharpPath("folder", "file.txt");

        Assert.Equal(path1.GetHashCode(), path2.GetHashCode());
    }

    [Fact]
    public void CompareTo_DifferentPaths_ReturnsCorrectComparison()
    {
        var path1 = new SharpPath("a.txt");
        var path2 = new SharpPath("b.txt");

        Assert.True(path1.CompareTo(path2) < 0);
        Assert.True(path2.CompareTo(path1) > 0);
        Assert.Equal(0, path1.CompareTo(path1));
    }

    #endregion

    #region Helper Methods Tests

    [Theory]
    [InlineData("*.txt", "file.txt", true)]
    [InlineData("*.txt", "file.log", false)]
    [InlineData("test?.txt", "test1.txt", true)]
    [InlineData("test?.txt", "test12.txt", false)]
    [InlineData("*", "anything", true)]
    public void WildcardMatching_VariousPatterns_MatchesCorrectly(
        string pattern,
        string text,
        bool expected
    )
    {
        // This tests the internal wildcard matching via the Glob method
        var testDir = GetTempPath("wildcardtest");
        Directory.CreateDirectory(testDir);

        var testFile = Path.Combine(testDir, text);
        File.WriteAllText(testFile, "content");

        var path = new SharpPath(testDir);
        var matches = path.Glob(pattern).ToList();

        if (expected)
            Assert.Single(matches);
        else
            Assert.Empty(matches);
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public void WithName_EmptyName_ThrowsException()
    {
        // For paths without a name component (like "." or root paths)
        var path = new SharpPath(OperatingSystem.IsWindows() ? "C:\\" : "/");

        Assert.Throws<InvalidOperationException>(() => path.WithName("newname"));
    }

    [Fact]
    public void IterateDirectory_NonDirectory_ThrowsException()
    {
        var testFile = GetTempPath("notadir.txt");
        File.WriteAllText(testFile, "content");
        var path = new SharpPath(testFile);

        Assert.Throws<InvalidOperationException>(() => path.IterateDirectory().ToList());
    }

    [Fact]
    public void Glob_NonDirectory_ThrowsException()
    {
        var testFile = GetTempPath("notadir.txt");
        File.WriteAllText(testFile, "content");
        var path = new SharpPath(testFile);

        Assert.Throws<InvalidOperationException>(() => path.Glob("*").ToList());
    }

    [Fact]
    public void ReadText_NonExistentFile_ThrowsException()
    {
        var path = new SharpPath(GetTempPath("nonexistent.txt"));

        Assert.Throws<FileNotFoundException>(() => path.ReadText());
    }

    [Fact]
    public void Unlink_NonExistingFile_ThrowsException()
    {
        var path = new SharpPath(GetTempPath("nonexistent.txt"));

        Assert.Throws<FileNotFoundException>(() => path.Unlink(missingOk: false));
    }

    #endregion
}
