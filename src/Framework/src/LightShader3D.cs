using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Reusable forward-lighting shader that consumes <see cref="Light3DState"/> snapshots.
/// Typical usage is:
/// 1. construct once for the desired compiled light budget
/// 2. gather per-frame <see cref="Light3D.ToLightState"/> snapshots
/// 3. call <see cref="Begin"/> after the camera 3D pass starts
/// 4. draw lit geometry
/// 5. call <see cref="End"/>
/// 6. dispose the resource with the owning scene or renderer
/// </summary>
public sealed class LightShader3D : Resource {
	private const int OpenGlLightLimit = 8;
	private readonly int _maxLights;
	private Shader _shader;
	private int _viewPosLoc;
	private int _ambientLoc;
	private int[] _lightEnabledLocs = [];
	private int[] _lightTypeLocs = [];
	private int[] _lightPositionLocs = [];
	private int[] _lightDirectionLocs = [];
	private int[] _lightColorLocs = [];
	private int[] _lightRangeLocs = [];
	private int[] _lightAttenuationLocs = [];
	private int[] _lightInnerCosLocs = [];
	private int[] _lightOuterCosLocs = [];

	/// <summary>
	/// Creates a reusable 3D lighting shader.
	/// </summary>
	/// <param name="maxLights">
	/// Maximum dynamic lights compiled into the shader.
	/// This is clamped to the practical OpenGL-era limit of 8 used by the project.
	/// </param>
	public LightShader3D(int maxLights = OpenGlLightLimit) {
		_maxLights = Math.Clamp(maxLights, 1, OpenGlLightLimit);
	}

	/// <summary>
	/// Number of lights compiled into the shader.
	/// </summary>
	public int MaxLights => _maxLights;

	/// <summary>
	/// Ambient baseline color applied before dynamic lights.
	/// </summary>
	public Vector3 AmbientColor { get; set; } = new(0.10f, 0.12f, 0.16f);

	/// <summary>
	/// Begins shader mode and uploads the supplied view and light state.
	/// Lights beyond <see cref="MaxLights"/> are ignored, and unused slots are cleared.
	/// </summary>
	public void Begin(Vector3 viewPosition, IReadOnlyList<Light3DState> lights) {
		EnsureLoaded();

		Raylib.SetShaderValue(_shader, _viewPosLoc, viewPosition, ShaderUniformDataType.Vec3);
		var ambientColor = ResolveAmbientColor(lights);
		Raylib.SetShaderValue(_shader, _ambientLoc, ambientColor, ShaderUniformDataType.Vec3);

		var dynamicLightIndex = 0;
		for (var i = 0; i < _maxLights; i++) {
			var hasLight = TryGetDynamicLight(lights, ref dynamicLightIndex, out var light);
			var enabled = hasLight && light.Enabled ? 1 : 0;
			var type = hasLight ? (int)light.Type : 0;
			var direction = light.Type == Light3DType.Spot ? -light.Direction : light.Direction;

			Raylib.SetShaderValue(_shader, _lightEnabledLocs[i], enabled, ShaderUniformDataType.Int);
			Raylib.SetShaderValue(_shader, _lightTypeLocs[i], type, ShaderUniformDataType.Int);
			Raylib.SetShaderValue(_shader, _lightPositionLocs[i], light.Position, ShaderUniformDataType.Vec3);
			Raylib.SetShaderValue(_shader, _lightDirectionLocs[i], direction, ShaderUniformDataType.Vec3);
			Raylib.SetShaderValue(_shader, _lightColorLocs[i], light.ColorVector, ShaderUniformDataType.Vec3);
			Raylib.SetShaderValue(_shader, _lightRangeLocs[i], light.Range, ShaderUniformDataType.Float);
			Raylib.SetShaderValue(_shader, _lightAttenuationLocs[i], light.Attenuation, ShaderUniformDataType.Float);
			Raylib.SetShaderValue(_shader, _lightInnerCosLocs[i], MathF.Cos(light.InnerSpotAngleDegrees * (MathF.PI / 180f)), ShaderUniformDataType.Float);
			Raylib.SetShaderValue(_shader, _lightOuterCosLocs[i], MathF.Cos(light.OuterSpotAngleDegrees * (MathF.PI / 180f)), ShaderUniformDataType.Float);
		}

		Raylib.BeginShaderMode(_shader);
	}

	internal Vector3 ResolveAmbientColor(IReadOnlyList<Light3DState> lights) {
		var ambient = AmbientColor;
		for (var i = 0; i < lights.Count; i++) {
			var light = lights[i];
			if (!light.Enabled || !light.Ambient) {
				continue;
			}

			ambient += light.ColorVector;
		}

		return ClampColorVector(ambient);
	}

	internal static bool TryGetDynamicLight(IReadOnlyList<Light3DState> lights, ref int scanIndex, out Light3DState light) {
		while (scanIndex < lights.Count) {
			var candidate = lights[scanIndex++];
			if (!candidate.Ambient) {
				light = candidate;
				return true;
			}
		}

		light = default;
		return false;
	}

	/// <summary>
	/// Ends the active shader mode opened by <see cref="Begin"/>.
	/// </summary>
	public void End() {
		if (IsLoaded) {
			Raylib.EndShaderMode();
		}
	}

	protected override void LoadCore() {
		_shader = Raylib.LoadShaderFromMemory(VertexShader, BuildFragmentShader(_maxLights));
		_viewPosLoc = Raylib.GetShaderLocation(_shader, "viewPos");
		_ambientLoc = Raylib.GetShaderLocation(_shader, "ambientColor");
		_lightEnabledLocs = GetLightLocations("lightEnabled");
		_lightTypeLocs = GetLightLocations("lightType");
		_lightPositionLocs = GetLightLocations("lightPosition");
		_lightDirectionLocs = GetLightLocations("lightDirection");
		_lightColorLocs = GetLightLocations("lightColor");
		_lightRangeLocs = GetLightLocations("lightRange");
		_lightAttenuationLocs = GetLightLocations("lightAttenuation");
		_lightInnerCosLocs = GetLightLocations("lightInnerCos");
		_lightOuterCosLocs = GetLightLocations("lightOuterCos");
	}

	protected override void UnloadCore() {
		Raylib.UnloadShader(_shader);
	}

	private int[] GetLightLocations(string baseName) {
		var locations = new int[_maxLights];
		for (var i = 0; i < locations.Length; i++) {
			locations[i] = Raylib.GetShaderLocation(_shader, $"{baseName}[{i}]");
		}

		return locations;
	}

	private static Vector3 ClampColorVector(Vector3 value) {
		return new Vector3(
			Math.Clamp(value.X, 0f, 1f),
			Math.Clamp(value.Y, 0f, 1f),
			Math.Clamp(value.Z, 0f, 1f));
	}

	private static string BuildFragmentShader(int maxLights) {
		return $$"""
			#version 330

			#define MAX_LIGHTS {{maxLights}}
			#define LIGHT_DIRECTIONAL 0
			#define LIGHT_POINT 1
			#define LIGHT_SPOT 2

			in vec3 fragPosition;
			in vec3 fragNormal;
			in vec4 fragColor;

			uniform vec3 viewPos;
			uniform vec3 ambientColor;
			uniform int lightEnabled[MAX_LIGHTS];
			uniform int lightType[MAX_LIGHTS];
			uniform vec3 lightPosition[MAX_LIGHTS];
			uniform vec3 lightDirection[MAX_LIGHTS];
			uniform vec3 lightColor[MAX_LIGHTS];
			uniform float lightRange[MAX_LIGHTS];
			uniform float lightAttenuation[MAX_LIGHTS];
			uniform float lightInnerCos[MAX_LIGHTS];
			uniform float lightOuterCos[MAX_LIGHTS];

			out vec4 finalColor;

			void main()
			{
			    vec3 normal = normalize(fragNormal);
			    vec3 baseColor = fragColor.rgb;
			    vec3 lit = baseColor * ambientColor;

			    for (int i = 0; i < MAX_LIGHTS; i++)
			    {
			        if (lightEnabled[i] == 0) continue;

			        vec3 lightVector;
			        float attenuation = 1.0;

			        if (lightType[i] == LIGHT_DIRECTIONAL)
			        {
			            lightVector = normalize(-lightDirection[i]);
			        }
			        else
			        {
			            vec3 toLight = lightPosition[i] - fragPosition;
			            float distanceToLight = length(toLight);
			            if (distanceToLight <= 0.0001) continue;
			            lightVector = toLight / distanceToLight;

			            float rangeFactor = lightRange[i] > 0.0 ? clamp(1.0 - (distanceToLight / lightRange[i]), 0.0, 1.0) : 1.0;
			            attenuation = rangeFactor / (1.0 + lightAttenuation[i] * distanceToLight * distanceToLight);

			            if (lightType[i] == LIGHT_SPOT)
			            {
			                float coneCos = dot(normalize(lightPosition[i] - fragPosition), normalize(lightDirection[i]));
			                float outerCos = lightOuterCos[i];
			                float innerCos = max(lightInnerCos[i], outerCos + 0.0001);
			                float spotFactor = clamp((coneCos - outerCos) / (innerCos - outerCos), 0.0, 1.0);
			                attenuation *= spotFactor;
			            }
			        }

			        float diffuse = max(dot(normal, lightVector), 0.0);
			        float wrap = diffuse * 0.82 + 0.18;
			        lit += baseColor * lightColor[i] * wrap * attenuation;
			    }

			    finalColor = vec4(clamp(lit, 0.0, 1.0), fragColor.a);
			}
			""";
	}

	private const string VertexShader = """
		#version 330

		in vec3 vertexPosition;
		in vec3 vertexNormal;
		in vec4 vertexColor;

		uniform mat4 mvp;
		uniform mat4 matModel;

		out vec3 fragPosition;
		out vec3 fragNormal;
		out vec4 fragColor;

		void main()
		{
		    vec4 worldPosition = matModel * vec4(vertexPosition, 1.0);
		    fragPosition = worldPosition.xyz;
		    fragNormal = normalize(mat3(transpose(inverse(matModel))) * vertexNormal);
		    fragColor = vertexColor;
		    gl_Position = mvp * vec4(vertexPosition, 1.0);
		}
		""";
}
