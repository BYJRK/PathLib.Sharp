using System.Text;
using Xunit.Abstractions;

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
        path.ToString().Should().Be(".");
    }

    [Fact]
    public void Constructor_NullPath_ReturnsCurrentDirectory()
    {
        var path = new SharpPath((string[])null!);
        path.ToString().Should().Be(".");
    }

    [Fact]
    public void Constructor_MultipleSegments_CombinesCorrectly()
    {
        var path = new SharpPath("folder1", "folder2", "file.txt");
        var expected = Path.Combine("folder1", "folder2", "file.txt");
        path.ToString().Should().Be(expected);
    }

    [Fact]
    public void Constructor_FromAnotherSharpPath_CopiesCorrectly()
    {
        var original = new SharpPath("test", "path");
        var copy = new SharpPath(original);
        copy.ToString().Should().Be(original.ToString());
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void Parts_SimplePath_ReturnsCorrectParts()
    {
        var path = new SharpPath("folder", "subfolder", "file.txt");
        var parts = path.Parts;

        parts.Should().Contain("folder");
        parts.Should().Contain("subfolder");
        parts.Should().Contain("file.txt");
    }

    [Fact]
    public void Parts_CurrentDirectory_ReturnsDot()
    {
        var path = new SharpPath(".");
        var parts = path.Parts;

        parts.Should().ContainSingle();
        parts[0].Should().Be(".");
    }

    [Fact]
    public void Name_FileWithExtension_ReturnsCorrectName()
    {
        var path = new SharpPath("folder", "file.txt");
        path.Name.Should().Be("file.txt");
    }

    [Fact]
    public void Name_DirectoryPath_ReturnsDirectoryName()
    {
        var path = new SharpPath("folder", "subfolder");
        path.Name.Should().Be("subfolder");
    }

    [Fact]
    public void Stem_FileWithExtension_ReturnsNameWithoutExtension()
    {
        var path = new SharpPath("document.pdf");
        path.Stem.Should().Be("document");
    }

    [Fact]
    public void Stem_FileWithMultipleExtensions_ReturnsCorrectStem()
    {
        var path = new SharpPath("archive.tar.gz");
        path.Stem.Should().Be("archive.tar");
    }

    [Fact]
    public void Suffix_FileWithExtension_ReturnsExtension()
    {
        var path = new SharpPath("document.pdf");
        path.Suffix.Should().Be(".pdf");
    }

    [Fact]
    public void Suffixes_FileWithMultipleExtensions_ReturnsAllSuffixes()
    {
        var path = new SharpPath("archive.tar.gz");
        var suffixes = path.Suffixes;

        suffixes.Should().Contain(".tar");
        suffixes.Should().Contain(".gz");
    }

    [Fact]
    public void Parent_FilePath_ReturnsParentDirectory()
    {
        var path = new SharpPath("folder", "file.txt");
        var parent = path.Parent;

        parent.ToString().Should().Be("folder");
    }

    [Fact]
    public void Parents_NestedPath_ReturnsAllAncestors()
    {
        var path = new SharpPath("a", "b", "c", "file.txt");
        var parents = path.Parents.ToList();

        parents.Should().Contain(p => p.ToString().EndsWith("c"));
        parents.Should().Contain(p => p.ToString().EndsWith(Path.Combine("b", "c")));
    }

    [Theory]
    [InlineData("C:\\folder\\file.txt", "C:")]
    [InlineData("/home/user/file.txt", "")]
    public void Drive_DifferentPaths_ReturnsCorrectDrive(string pathStr, string expectedDrive)
    {
        if (OperatingSystem.IsWindows() || !pathStr.StartsWith("C:"))
        {
            var path = new SharpPath(pathStr);
            path.Drive.Should().Be(expectedDrive);
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
        combined.ToString().Should().Be(expected);
    }

    [Fact]
    public void DivisionOperator_WithSharpPath_CombinesCorrectly()
    {
        var basePath = new SharpPath("folder");
        var subPath = new SharpPath("subfolder", "file.txt");
        var combined = basePath / subPath;

        var expected = Path.Combine("folder", Path.Combine("subfolder", "file.txt"));
        combined.ToString().Should().Be(expected);
    }

    [Fact]
    public void ImplicitConversion_FromString_WorksCorrectly()
    {
        SharpPath path = "test/path";
        var expected = Path.Combine("test", "path");
        path.ToString().Should().Be(expected);
    }

    [Fact]
    public void ImplicitConversion_ToString_WorksCorrectly()
    {
        var path = new SharpPath("test", "path");
        string pathStr = path;
        pathStr.Should().Be(path.ToString());
    }

    #endregion

    #region Path Operations Tests

    [Fact]
    public void JoinPath_MultipleSegments_CombinesCorrectly()
    {
        var path = new SharpPath("base");
        var result = path.JoinPath("folder1", "folder2", "file.txt");

        var expected = Path.Combine("base", "folder1", "folder2", "file.txt");
        result.ToString().Should().Be(expected);
    }

    [Fact]
    public void IsAbsolute_AbsolutePath_ReturnsTrue()
    {
        var absolutePath = OperatingSystem.IsWindows()
            ? new SharpPath("C:", "folder", "file.txt")
            : new SharpPath("/", "home", "user", "file.txt");

        absolutePath.IsAbsolute.Should().BeTrue();
    }

    [Fact]
    public void IsAbsolute_RelativePath_ReturnsFalse()
    {
        var relativePath = new SharpPath("folder", "file.txt");
        relativePath.IsAbsolute.Should().BeFalse();
    }

    [Fact]
    public void WithName_ValidName_ReturnsPathWithNewName()
    {
        var path = new SharpPath("folder", "oldfile.txt");
        var newPath = path.WithName("newfile.txt");

        newPath.Name.Should().Be("newfile.txt");
        newPath.Parent.ToString().Should().Be(path.Parent.ToString());
    }

    [Fact]
    public void WithStem_ValidStem_ReturnsPathWithNewStem()
    {
        var path = new SharpPath("document.pdf");
        var newPath = path.WithStem("report");

        newPath.Name.Should().Be("report.pdf");
    }

    [Fact]
    public void WithSuffix_ValidSuffix_ReturnsPathWithNewSuffix()
    {
        var path = new SharpPath("document.pdf");
        var newPath = path.WithSuffix(".txt");

        newPath.Name.Should().Be("document.txt");
    }

    #endregion

    #region File System Queries Tests

    [Fact]
    public void Exists_ExistingFile_ReturnsTrue()
    {
        var testFile = GetTempPath("testfile.txt");
        File.WriteAllText(testFile, "test content");

        var path = new SharpPath(testFile);
        path.Exists.Should().BeTrue();
    }

    [Fact]
    public void Exists_NonExistingFile_ReturnsFalse()
    {
        var path = new SharpPath(GetTempPath("nonexistent.txt"));
        path.Exists.Should().BeFalse();
    }

    [Fact]
    public void IsFile_ExistingFile_ReturnsTrue()
    {
        var testFile = GetTempPath("testfile.txt");
        File.WriteAllText(testFile, "test content");

        var path = new SharpPath(testFile);
        path.IsFile.Should().BeTrue();
        path.IsDirectory.Should().BeFalse();
    }

    [Fact]
    public void IsDirectory_ExistingDirectory_ReturnsTrue()
    {
        var testDir = GetTempPath("testdir");
        Directory.CreateDirectory(testDir);

        var path = new SharpPath(testDir);
        path.IsDirectory.Should().BeTrue();
        path.IsFile.Should().BeFalse();
    }

    [Fact]
    public void Stat_ExistingFile_ReturnsFileInfo()
    {
        var testFile = GetTempPath("testfile.txt");
        File.WriteAllText(testFile, "test content");

        var path = new SharpPath(testFile);
        var stat = path.Stat();

        stat.Should().NotBeNull();
        stat.Should().BeOfType<FileInfo>();
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

        readContent.Should().Be(content);
    }

    [Fact]
    public void ReadBytes_WriteBytes_WorksCorrectly()
    {
        var testFile = GetTempPath("binaryfile.bin");
        var path = new SharpPath(testFile);
        var data = Encoding.UTF8.GetBytes("Binary data test");

        path.WriteBytes(data);
        var readData = path.ReadBytes();

        readData.Should().Equal(data);
    }

    [Fact]
    public void Touch_NonExistingFile_CreatesFile()
    {
        var testFile = GetTempPath("touchfile.txt");
        var path = new SharpPath(testFile);

        path.Exists.Should().BeFalse();
        path.Touch();
        path.Exists.Should().BeTrue();
        path.IsFile.Should().BeTrue();
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

        newTime.Should().BeAfter(originalTime);
    }

    [Fact]
    public void Open_ValidFile_ReturnsFileStream()
    {
        var testFile = GetTempPath("streamfile.txt");
        File.WriteAllText(testFile, "test content");
        var path = new SharpPath(testFile);

        using var stream = path.Open();
        stream.Should().NotBeNull();
        stream.CanRead.Should().BeTrue();
    }

    #endregion

    #region Directory Operations Tests

    [Fact]
    public void MakeDirectory_NewDirectory_CreatesDirectory()
    {
        var testDir = GetTempPath("newdirectory");
        var path = new SharpPath(testDir);

        path.Exists.Should().BeFalse();
        path.MakeDirectory();
        path.Exists.Should().BeTrue();
        path.IsDirectory.Should().BeTrue();
    }

    [Fact]
    public void MakeDirectory_WithParents_CreatesNestedDirectories()
    {
        var testDir = GetTempPath(Path.Combine("level1", "level2", "level3"));
        var path = new SharpPath(testDir);

        path.MakeDirectory(createParents: true);
        path.Exists.Should().BeTrue();
        path.IsDirectory.Should().BeTrue();
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

        entries.Should().HaveCount(3);
        entries.Should().Contain(e => e.Name == "file1.txt");
        entries.Should().Contain(e => e.Name == "file2.txt");
        entries.Should().Contain(e => e.Name == "subdir");
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

        matches.Should().HaveCount(2);
        matches.Should().AllSatisfy(m => m.Name.Should().EndWith(".txt"));
    }

    [Fact]
    public void RemoveDirectory_EmptyDirectory_RemovesSuccessfully()
    {
        var testDir = GetTempPath("removedir");
        Directory.CreateDirectory(testDir);
        var path = new SharpPath(testDir);

        path.Exists.Should().BeTrue();
        path.RemoveDirectory();
        path.Exists.Should().BeFalse();
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

        File.Exists(sourceFile).Should().BeFalse();
        File.Exists(targetFile).Should().BeTrue();
        result.ToString().Should().Be(targetFile);
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

        File.Exists(sourceFile).Should().BeFalse();
        File.Exists(targetFile).Should().BeTrue();
        File.ReadAllText(targetFile).Should().Be("new content");
    }

    [Fact]
    public void Unlink_ExistingFile_RemovesFile()
    {
        var testFile = GetTempPath("unlinkfile.txt");
        File.WriteAllText(testFile, "content");
        var path = new SharpPath(testFile);

        path.Exists.Should().BeTrue();
        path.Unlink();
        path.Exists.Should().BeFalse();
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

        currentDir.Should().NotBeNull();
        currentDir.Exists.Should().BeTrue();
        currentDir.IsDirectory.Should().BeTrue();
        currentDir.IsAbsolute.Should().BeTrue();
    }

    [Fact]
    public void Home_ReturnsValidPath()
    {
        var home = SharpPath.Home;

        home.Should().NotBeNull();
        home.IsAbsolute.Should().BeTrue();
        // Note: Home directory might not always exist in all test environments
    }

    #endregion

    #region Equality and Comparison Tests

    [Fact]
    public void Equals_SamePaths_ReturnsTrue()
    {
        var path1 = new SharpPath("folder", "file.txt");
        var path2 = new SharpPath("folder", "file.txt");

        path1.Equals(path2).Should().BeTrue();
        (path1 == path2).Should().BeTrue();
        (path1 != path2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentPaths_ReturnsFalse()
    {
        var path1 = new SharpPath("folder1", "file.txt");
        var path2 = new SharpPath("folder2", "file.txt");

        path1.Equals(path2).Should().BeFalse();
        (path1 == path2).Should().BeFalse();
        (path1 != path2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SamePaths_ReturnsSameHash()
    {
        var path1 = new SharpPath("folder", "file.txt");
        var path2 = new SharpPath("folder", "file.txt");

        path1.GetHashCode().Should().Be(path2.GetHashCode());
    }

    [Fact]
    public void CompareTo_DifferentPaths_ReturnsCorrectComparison()
    {
        var path1 = new SharpPath("a.txt");
        var path2 = new SharpPath("b.txt");

        path1.CompareTo(path2).Should().BeNegative();
        path2.CompareTo(path1).Should().BePositive();
        path1.CompareTo(path1).Should().Be(0);
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
            matches.Should().ContainSingle();
        else
            matches.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public void WithName_EmptyName_ThrowsException()
    {
        // For paths without a name component (like "." or root paths)
        var path = new SharpPath(OperatingSystem.IsWindows() ? "C:\\" : "/");

        Action act = () => path.WithName("newname");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IterateDirectory_NonDirectory_ThrowsException()
    {
        var testFile = GetTempPath("notadir.txt");
        File.WriteAllText(testFile, "content");
        var path = new SharpPath(testFile);

        Action act = () => path.IterateDirectory().ToList();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Glob_NonDirectory_ThrowsException()
    {
        var testFile = GetTempPath("notadir.txt");
        File.WriteAllText(testFile, "content");
        var path = new SharpPath(testFile);

        Action act = () => path.Glob("*").ToList();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReadText_NonExistentFile_ThrowsException()
    {
        var path = new SharpPath(GetTempPath("nonexistent.txt"));

        Action act = () => path.ReadText();
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Unlink_NonExistingFile_ThrowsException()
    {
        var path = new SharpPath(GetTempPath("nonexistent.txt"));

        Action act = () => path.Unlink(missingOk: false);
        act.Should().Throw<FileNotFoundException>();
    }

    #endregion
}
