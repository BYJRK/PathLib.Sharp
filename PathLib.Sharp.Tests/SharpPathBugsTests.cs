using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PathLib;
using Xunit;
using Xunit.Abstractions;

namespace PathLib.Sharp.Tests;

public class SharpPathBugsTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDirectory;
    private readonly List<string> _filesToCleanup = new();
    private readonly List<string> _directoriesToCleanup = new();

    public SharpPathBugsTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"PathLibSharp_BugsTests_{Guid.NewGuid():N}"
        );
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

    /// <summary>
    /// Bug Reproduction: MatchesGlobPattern only checks the filename, ignoring directory structure in pattern.
    /// </summary>
    [Fact]
    public void Glob_RecursivePattern_ShouldRespectDirectoryStructure()
    {
        var root = new SharpPath(_tempDirectory);

        // Setup directory structure:
        // root/src/match.cs
        // root/test/match.cs

        var srcDir = root / "src";
        var testDir = root / "test";

        srcDir.MakeDirectory();
        testDir.MakeDirectory();

        (srcDir / "match.cs").Touch();
        (testDir / "match.cs").Touch();

        // We want to match only files in 'src' folder recursively or using wildcards
        // The implementation handles "**" by enumerating everything and then filtering
        // using MatchesGlobPattern.
        // Current implementation: pattern "**/src/*.cs" -> "*/src/*.cs" -> PatternName "*.cs".
        // It matches matches any file ending in .cs regardless of path.

        var matches = root.Glob("**/src/*.cs").ToList();

        matches.Should().NotBeEmpty("because we should find at least one file");

        // Should NOT find test/match.cs
        matches.Should().NotContain(p => p.Parent.Name == "test");

        // Should find src/match.cs
        matches.Should().Contain(p => p.Parent.Name == "src");
        matches.Should().ContainSingle();
    }

    /// <summary>
    /// Bug Reproduction: MakeDirectory(createParents: false) relies on Directory.CreateDirectory
    /// which automatically creates parents. It should fail if parents are missing.
    /// </summary>
    [Fact]
    public void MakeDirectory_NoCreateParents_ShouldThrowIfParentMissing()
    {
        var path = new SharpPath(_tempDirectory) / "missing_parent" / "target_dir";

        // Should throw because missing_parent does not exist and createParents is false
        Action makedirAct = () => path.MakeDirectory(createParents: false);
        makedirAct.Should().Throw<IOException>();
    }

    /// <summary>
    /// Bug Reproduction: MakeDirectory(existOk: false) relies on Directory.CreateDirectory
    /// which does not throw if directory exists. It should throw.
    /// </summary>
    [Fact]
    public void MakeDirectory_NoExistOk_ShouldThrowIfDirectoryExists()
    {
        var path = new SharpPath(_tempDirectory) / "existing_dir";
        path.MakeDirectory(); // Create it first

        // Should throw because directory exists and existOk is false
        Action existAct = () => path.MakeDirectory(existOk: false);
        existAct.Should().ThrowExactly<IOException>();
    }

    /// <summary>
    /// Bug Reproduction: IsSymlink returns false for broken symlinks because it depends on Exists/IsFile/IsDirectory
    /// which return false for broken links.
    /// </summary>
    [Fact]
    public void IsSymlink_BrokenSymlink_ShouldReturnTrue()
    {
        var linkPath = new SharpPath(_tempDirectory) / "broken_link";
        var targetPath = new SharpPath(_tempDirectory) / "non_existent_target";

        try
        {
            File.CreateSymbolicLink(linkPath.ToString(), targetPath.ToString());
        }
        catch (Exception ex)
        {
            // If we can't create symlinks (e.g. permissions), skip this test
            // Skip logic depends on runner, we'll just return here to avoid false negative failure
            // In a real project we'd use Skip.If
            _output.WriteLine($"Skipping symlink test: {ex.Message}");
            return;
        }

        linkPath.IsSymlink.Should().BeTrue("because IsSymlink should be true for broken symbolic link");
        linkPath.Exists.Should().BeFalse("because Exists should be false for broken symbolic link");
    }

    /// <summary>
    /// Bug Reproduction: Touch() automatically creates parent directories.
    /// Standard pathlib behavior is to fail if parents don't exist.
    /// </summary>
    [Fact]
    public void Touch_MissingParent_ShouldThrow()
    {
        var path = new SharpPath(_tempDirectory) / "missing_dir_for_touch" / "file.txt";

        Action touchAct = () => path.Touch();
        touchAct.Should().Throw<IOException>();
    }
}
