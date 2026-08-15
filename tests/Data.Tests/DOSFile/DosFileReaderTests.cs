using System.Text;
using ConquestFrontierWarsRay.Data.DosFile;

namespace ConquestFrontierWarsRay.Data.Tests.DOSFile;

public sealed class DosFileReaderTests {
	[Fact]
	public void StringPackDatabase_Exposes_ParseData_And_GametypesHeader() {
		var reader = new DosFileReader(Path.Combine(GetRepoRoot(), "assets", "DB", "StringPack.db"));

		var rootEntries = reader.FindFiles();
		Assert.Contains(rootEntries, entry => entry.IsDirectory && entry.Name.Equals("ParseData", StringComparison.OrdinalIgnoreCase));

		var parseDataEntries = reader.FindFiles("ParseData");
		Assert.Contains(parseDataEntries, entry => !entry.IsDirectory && entry.Name.Equals("gametypes.h", StringComparison.OrdinalIgnoreCase));
	}

	public static IEnumerable<object[]> UtfArchiveCases() {
		var utfArchiveRoot = Path.Combine(GetRepoRoot(), "tests", "Data.Tests", "utfArchives");
		foreach (var treePath in Directory.EnumerateFiles(utfArchiveRoot, "*.tree.txt", SearchOption.TopDirectoryOnly)
			         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)) {
			var archivePath = treePath[..^".tree.txt".Length];
			yield return [archivePath, treePath];
		}
	}

	[Theory]
	[MemberData(nameof(UtfArchiveCases))]
	public void UtfArchive_TreeDump_Matches_Expected_Layout(string archivePath, string treePath) {
		var reader = new DosFileReader(archivePath);
		var expected = File.ReadAllText(treePath).ReplaceLineEndings("\n").TrimEnd('\n');
		var actual = DumpTree(reader, Path.GetFileName(archivePath)).ReplaceLineEndings("\n").TrimEnd('\n');

		Assert.Equal(expected, actual);
	}

	private static string DumpTree(DosFileReader reader, string rootName) {
		var builder = new StringBuilder();
		builder.AppendLine(rootName);
		WriteChildren(reader, builder, string.Empty, string.Empty);
		return builder.ToString();
	}

	private static void WriteChildren(DosFileReader reader, StringBuilder builder, string relativePath, string prefix) {
		var entries = reader.FindFiles(relativePath)
			.OrderByDescending(entry => entry.IsDirectory)
			.ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
			.ToArray();
		for (var index = 0; index < entries.Length; index++) {
			var entry = entries[index];
			var isLast = index == entries.Length - 1;
			var branch = isLast ? "\\-- " : "+-- ";
			var nextPrefix = prefix + (isLast ? "    " : "|   ");

			builder.Append(prefix);
			builder.Append(branch);
			builder.Append(entry.Name);
			if (entry.IsDirectory) {
				builder.Append('/');
				builder.AppendLine();
				WriteChildren(reader, builder, CombineRelativePath(relativePath, entry.Name), nextPrefix);
			} else {
				builder.Append(" (");
				builder.Append(entry.Length);
				builder.Append(" bytes");
				builder.AppendLine(")");
			}
		}
	}

	private static string CombineRelativePath(string left, string right) {
		return string.IsNullOrWhiteSpace(left) ? right : Path.Combine(left, right);
	}

	private static string GetRepoRoot() {
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null) {
			if (File.Exists(Path.Combine(current.FullName, "ConquestFrontierWarsRay.slnx")) &&
			    File.Exists(Path.Combine(current.FullName, "src", "Conquest", "ConquestFrontierWars.csproj")) &&
			    Directory.Exists(Path.Combine(current.FullName, "assets", "DB")) &&
			    Directory.Exists(Path.Combine(current.FullName, "tests", "Data.Tests", "utfArchives"))) {
				return current.FullName;
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException("Could not locate ConquestFrontierWarsRay repo root.");
	}
}
