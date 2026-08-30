using System.Collections.Generic;
using System.Linq;

namespace RaySharp.Mesh;

internal sealed class MeshLoadDiagnostics {
	public int DecodedTextureCount { get; set; }
	public int DiffuseMaterialMapCount { get; set; }
	public int TexturedTriangleCount { get; set; }
	public int UntexturedTriangleCount { get; set; }
	public List<string> DecodedTextureNames { get; } = [];
	public List<string> DiffuseTextureNames { get; } = [];
	public List<string> MissingTextureNames { get; } = [];

	public string Summary(int uploadedTextureCount) {
		string decoded = DecodedTextureNames.Count == 0 ? "none" : string.Join(", ", DecodedTextureNames.Take(4));
		string maps = DiffuseTextureNames.Count == 0 ? "none" : string.Join(", ", DiffuseTextureNames.Distinct().Take(4));
		string missing = MissingTextureNames.Count == 0 ? "none" : string.Join(", ", MissingTextureNames.Distinct().Take(4));
		return $"tex decoded/uploaded {DecodedTextureCount}/{uploadedTextureCount}; material maps {DiffuseMaterialMapCount} [{maps}]; tris textured/untextured {TexturedTriangleCount}/{UntexturedTriangleCount}; decoded [{decoded}]; missing [{missing}]";
	}
}
