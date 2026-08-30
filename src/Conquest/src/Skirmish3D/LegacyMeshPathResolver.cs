using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConquestFrontierWarsRay.Data;

namespace ConquestFrontierWarsRay.Frontend;

internal static class LegacyMeshPathResolver {
	private static readonly string[] DefaultMeshCandidates = [
		// "destable.cmp.xml",
		// "mblast.cmp.xml",
		// "mcarrion.cmp.xml",
		"holeplat.cmp.xml",
		// "asteroid3.3db.xml"
	];

	public static string ResolveDefaultMeshPath() {
		var repoRoot = RepoPaths.LocateRepoRoot();
		var repoParent = Directory.GetParent(repoRoot)?.FullName;
		var searchRoots = new List<string>();
		AddSearchRoots(searchRoots, repoRoot);

		if (!string.IsNullOrWhiteSpace(repoParent)) {
			AddSearchRoots(searchRoots, repoParent);
		}

		foreach (var root in searchRoots.Where(Directory.Exists)) {
			foreach (var candidate in DefaultMeshCandidates) {
				var path = Path.Combine(root, candidate);
				if (File.Exists(path)) {
					return path;
				}
			}
		}

		throw new FileNotFoundException(
			$"Could not locate a legacy mesh sample. Checked: {string.Join(", ", searchRoots)}");
	}

	private static void AddSearchRoots(ICollection<string> searchRoots, string baseDirectory) {
		searchRoots.Add(Path.Combine(baseDirectory, "assets", "models_xml"));
		searchRoots.Add(Path.Combine(baseDirectory, "assets", "xml_dump"));
		searchRoots.Add(Path.Combine(baseDirectory, "models_xml"));
		searchRoots.Add(Path.Combine(baseDirectory, "xml_dump"));
	}
}
