using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConquestFrontierWarsRay.Data;

namespace RaySharp.Particle;

internal static class ParticleSampleResolver {
	public static string? Resolve(string fileName) {
		string[] candidates = BuildDirectoryCandidates()
			.Select(directory => Path.Combine(directory, fileName))
			.ToArray();

		return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
	}

	public static string? ResolveSamplesDirectory() {
		string[] candidates = BuildDirectoryCandidates();

		return candidates.Select(Path.GetFullPath).FirstOrDefault(Directory.Exists);
	}

	private static string[] BuildDirectoryCandidates() {
		HashSet<string> candidates = new(StringComparer.OrdinalIgnoreCase);
		void Add(string path) {
			if (!string.IsNullOrWhiteSpace(path)) {
				candidates.Add(Path.GetFullPath(path));
			}
		}

		try {
			string assetsRoot = RepoPaths.LocateAssetsRoot();
			Add(Path.Combine(assetsRoot, "particles_xml"));
			Add(Path.Combine(assetsRoot, "xml_unified"));
		} catch (DirectoryNotFoundException) {
		}

		foreach (string root in EnumerateSearchRoots()) {
			Add(Path.Combine(root, "assets", "particles_xml"));
			Add(Path.Combine(root, "assets", "xml_unified"));
			Add(Path.Combine(root, "particles_xml"));
			Add(Path.Combine(root, "xml_unified"));
			Add(Path.Combine(root, "RaySharp", "assets", "particles_xml"));
			Add(Path.Combine(root, "RaySharp", "assets", "xml_unified"));
			Add(Path.Combine(root, "ConquestSharp", "3dbModels", "particles_xml"));
			Add(Path.Combine(root, "ConquestSharp", "3dbModels", "xml_unified"));
		}

		return candidates.ToArray();
	}

	private static IEnumerable<string> EnumerateSearchRoots() {
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
		foreach (string start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory }) {
			if (string.IsNullOrWhiteSpace(start)) {
				continue;
			}

			for (DirectoryInfo? current = new DirectoryInfo(start); current is not null; current = current.Parent) {
				if (seen.Add(current.FullName)) {
					yield return current.FullName;
				}
			}
		}
	}
}
