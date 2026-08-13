using System.Numerics;
using Raylib_cs;

namespace RaySharp.Mesh;

internal sealed class MeshTextureLighting {
	private const string VertexShader = """
            #version 330

            in vec3 vertexPosition;
            in vec2 vertexTexCoord;
            in vec3 vertexNormal;
            in vec4 vertexColor;

            uniform mat4 mvp;
            uniform mat4 matModel;

            out vec2 fragTexCoord;
            out vec3 fragNormal;
            out vec4 fragColor;

            void main()
            {
                fragTexCoord = vertexTexCoord;
                fragNormal = normalize(mat3(transpose(inverse(matModel))) * vertexNormal);
                fragColor = vertexColor;
                gl_Position = mvp * vec4(vertexPosition, 1.0);
            }
            """;

	private const string FragmentShader = """
            #version 330

            in vec2 fragTexCoord;
            in vec3 fragNormal;
            in vec4 fragColor;

            uniform sampler2D texture0;
            uniform vec3 lightDirection;
            uniform vec3 lightColor;
            uniform vec3 ambientColor;

            out vec4 finalColor;

            void main()
            {
                vec4 texel = texture(texture0, fragTexCoord);
                vec3 normal = normalize(fragNormal);
                float diffuse = max(dot(normal, normalize(-lightDirection)), 0.0);
                float wrap = diffuse * 0.82 + 0.18;
                vec3 lit = texel.rgb * fragColor.rgb * (ambientColor + lightColor * wrap);
                finalColor = vec4(clamp(lit, 0.0, 1.0), texel.a * fragColor.a);
            }
            """;

	private readonly Shader shader;

	private MeshTextureLighting(Shader shader) {
		this.shader = shader;
		Vector3 lightDirection = Vector3.Normalize(new Vector3(-0.45f, -1.0f, -0.62f));
		Vector3 lightColor = new(1.05f, 1.0f, 0.92f);
		Vector3 ambientColor = new(0.32f, 0.38f, 0.48f);

		Raylib.SetShaderValue(shader, Raylib.GetShaderLocation(shader, "lightDirection"), lightDirection, ShaderUniformDataType.Vec3);
		Raylib.SetShaderValue(shader, Raylib.GetShaderLocation(shader, "lightColor"), lightColor, ShaderUniformDataType.Vec3);
		Raylib.SetShaderValue(shader, Raylib.GetShaderLocation(shader, "ambientColor"), ambientColor, ShaderUniformDataType.Vec3);
	}

	public static MeshTextureLighting Load() {
		return new MeshTextureLighting(Raylib.LoadShaderFromMemory(VertexShader, FragmentShader));
	}

	public void Begin() {
		Raylib.BeginShaderMode(shader);
	}

	public void End() {
		Raylib.EndShaderMode();
	}

	public void Unload() {
		Raylib.UnloadShader(shader);
	}
}
