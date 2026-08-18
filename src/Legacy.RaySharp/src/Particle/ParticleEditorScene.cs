using Raylib_cs;
using System.Numerics;
using DrawingColor = System.Drawing.Color;
using Forms = System.Windows.Forms;

namespace RaySharp.Particle;

internal sealed class ParticleEditorScene : IScene {
	private const float MainTransformMoveSpeed = 180.0f;
	private const float MainTransformRotateSpeed = 1.4f;
	private const float MainTransformScaleSpeed = 1.0f;

	private readonly SceneContext context;
	private readonly ParticleEditorUi editorUi;
	private readonly ParticleRenderer renderer = new();
	private readonly ParticleOrbitCamera orbitCamera = new();
	private readonly ParticleEditorLog log = new();
	private readonly ParticleSampleCatalog sampleCatalog = new();
	private ParticleParameters? parameters;
	private IReadOnlyDictionary<string, UtfTextureImage> textures = new Dictionary<string, UtfTextureImage>();
	private ParticlePreviewSystem? system;
	private readonly List<ParticlePreviewSystem> deployedSystems = [];
	private ParticleTextureResource? textureResource;
	private string displayName = string.Empty;
	private string status = "Loading sample particle system...";
	private float diagnosticTimer;
	private Vector3 mainTransformOrigin = Vector3.Zero;
	private Vector3 mainTransformRotationDegrees = Vector3.Zero;
	private Vector3 mainTransformScale = Vector3.One;

	public ParticleEditorScene(SceneContext context) {
		this.context = context;
		editorUi = new ParticleEditorUi(context.UiFont);
		log.Info("Particle editor scene created.");
		LoadSample();
	}

	public SceneRequest? Update(float deltaTime) {
		AppWindow.HandleShortcuts(context.WindowState, context.Ui);
		AppWindow.UpdateMouseConfinement(context.WindowState);

		Rectangle viewport = Viewport;
		orbitCamera.Update(viewport);
		bool textEditing = editorUi.IsTextEditing;
		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.R)) {
			system?.Restart();
			foreach (ParticlePreviewSystem deployedSystem in deployedSystems) {
				deployedSystem.Restart();
			}
		}

		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.F) && parameters is not null) {
			orbitCamera.Frame(parameters);
		}

		if (!textEditing && IsDeployPressed()) {
			DeployParticleSystem();
		}

		if (!textEditing && IsUndeployPressed()) {
			RemoveLastDeployedParticleSystem();
		}

		if (!textEditing) {
			HandleMainSystemTransformControls(deltaTime);
		}

		Camera3D camera = orbitCamera.ToCamera();
		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.F10)) {
			WriteSnapshot(deltaTime, camera, "ManualSnapshot");
		}

		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.E)) {
			WriteSnapshot(deltaTime, camera, "FlaggedBadSample");
		}

		system?.Update(deltaTime);
		foreach (ParticlePreviewSystem deployedSystem in deployedSystems) {
			deployedSystem.Update(deltaTime);
		}
		UpdateDiagnostics(deltaTime);

		Raylib.BeginDrawing();
		Raylib.ClearBackground(new Color(21, 23, 27, 255));

		Raylib.BeginScissorMode((int)viewport.X, (int)viewport.Y, (int)viewport.Width, (int)viewport.Height);
		Raylib.BeginMode3D(camera);
		renderer.DrawWorld(camera, ActiveSystems, textureResource);
		Raylib.EndMode3D();
		Raylib.EndScissorMode();

		ParticleEditorUiResult result = editorUi.Draw(
			parameters,
			system,
			ref mainTransformOrigin,
			ref mainTransformRotationDegrees,
			ref mainTransformScale,
			status,
			BuildStats(),
			sampleCatalog.Summary,
			sampleCatalog.Files,
			sampleCatalog.Folder,
			sampleCatalog.CurrentIndex,
			textureResource?.Name ?? "fallback sprite",
			log.Entries);
		Raylib.EndDrawing();

		if (result.OpenFile) {
			OpenParticleFileDialog();
		} else if (result.OpenFolder) {
			OpenParticleFolderDialog();
		} else if (result.PreviousSample) {
			LoadPreviousSample();
		} else if (result.NextSample) {
			LoadNextSample();
		} else if (result.SelectedSampleIndex is int selectedSampleIndex) {
			LoadSampleAtIndex(selectedSampleIndex);
		} else if (result.ColorPickIndex is int colorPickIndex) {
			OpenColorPicker(colorPickIndex);
		} else if (result.ReloadSample) {
			log.Info("Reload sample requested.");
			LoadSample();
		} else if (result.Restart) {
			system?.Restart();
			foreach (ParticlePreviewSystem deployedSystem in deployedSystems) {
				deployedSystem.Restart();
			}
			log.Info("Particle system restarted from UI.");
		} else if (result.Frame && parameters is not null) {
			orbitCamera.Frame(parameters);
			log.Info("Camera framed from UI.");
		} else if (result.MainTransformChanged) {
			ApplyMainSystemTransform();
		} else if (result.Changed) {
			if (result.ColorChanged && parameters is not null) {
				int updatedFrames = ParticleColorFrameInterpolator.RecalculateInBetweenFrames(parameters);
				log.Info($"Recalculated {updatedFrames} non-keyed color frames.");
			}

			log.Info("Parameters changed; rebuilding system.");
			RebuildSystem();
		}

		return null;
	}

	public void Dispose() {
		textureResource?.Dispose();
		ClearDeployedParticleSystems();
		system?.Dispose();
	}

	private void WriteSnapshot(float deltaTime, Camera3D camera, string reason) {
		log.Snapshot(ParticleSceneSnapshot.Capture(
			deltaTime,
			reason,
			displayName,
			status,
			orbitCamera,
			camera,
			system,
			textureResource));
	}

	private void LoadSample() {
		string? samplesDirectory = ParticleSampleResolver.ResolveSamplesDirectory();
		if (samplesDirectory is not null) {
			sampleCatalog.LoadFolder(samplesDirectory);
			log.Info($"Indexed {sampleCatalog.Count} unified particle files from '{samplesDirectory}'.");
		}

		string? path = ParticleSampleResolver.Resolve("blast.pte.unified.xml");
		if (path is null) {
			status = "Could not find assets/xml_unified/blast.pte.unified.xml.";
			log.Error(status);
			return;
		}

		sampleCatalog.SetCurrent(path);
		LoadParticleFile(path, frameCamera: true);
	}

	private void OpenParticleFileDialog() {
		using Forms.OpenFileDialog dialog = new() {
			Title = "Open unified particle XML",
			Filter = "Unified particle XML (*.pte.unified.xml)|*.pte.unified.xml",
			InitialDirectory = ParticleSampleResolver.ResolveSamplesDirectory() ?? Environment.CurrentDirectory,
			CheckFileExists = true,
			Multiselect = false
		};

		if (dialog.ShowDialog() != Forms.DialogResult.OK) {
			return;
		}

		sampleCatalog.LoadFolder(Path.GetDirectoryName(dialog.FileName)!, dialog.FileName);
		LoadParticleFile(dialog.FileName, frameCamera: true);
	}

	private void OpenParticleFolderDialog() {
		using Forms.FolderBrowserDialog dialog = new() {
			Description = "Open folder containing .pte.unified.xml particle files",
			SelectedPath = sampleCatalog.Folder ?? ParticleSampleResolver.ResolveSamplesDirectory() ?? Environment.CurrentDirectory,
			UseDescriptionForTitle = true
		};

		if (dialog.ShowDialog() != Forms.DialogResult.OK) {
			return;
		}

		if (!sampleCatalog.LoadFolder(dialog.SelectedPath)) {
			status = $"No .pte.unified.xml files found in {dialog.SelectedPath}.";
			log.Warning(status);
			return;
		}

		log.Info($"Indexed {sampleCatalog.Count} unified particle files from '{dialog.SelectedPath}'.");
		if (sampleCatalog.CurrentPath is not null) {
			LoadParticleFile(sampleCatalog.CurrentPath, frameCamera: true);
		}
	}

	private void LoadPreviousSample() {
		string? path = sampleCatalog.MovePrevious();
		if (path is null) {
			log.Warning(sampleCatalog.Count == 0
				? "Previous sample requested, but no sample catalog is loaded."
				: "Previous sample requested at the first catalog entry; selection was clamped.");
			return;
		}

		log.Info($"Loading previous sample: {sampleCatalog.Summary}.");
		LoadParticleFile(path, frameCamera: true);
	}

	private void LoadNextSample() {
		string? path = sampleCatalog.MoveNext();
		if (path is null) {
			log.Warning(sampleCatalog.Count == 0
				? "Next sample requested, but no sample catalog is loaded."
				: "Next sample requested at the last catalog entry; selection was clamped.");
			return;
		}

		log.Info($"Loading next sample: {sampleCatalog.Summary}.");
		LoadParticleFile(path, frameCamera: true);
	}

	private void LoadSampleAtIndex(int index) {
		string? path = sampleCatalog.GetPath(index);
		if (path is null || !sampleCatalog.SetCurrentIndex(index)) {
			log.Warning($"Sample tree selection {index} is outside the current catalog.");
			return;
		}

		log.Info($"Loading selected sample: {sampleCatalog.Summary}.");
		LoadParticleFile(path, frameCamera: true);
	}

	private void OpenColorPicker(int frameIndex) {
		if (parameters is null || frameIndex < 0 || frameIndex >= parameters.ColorFrames.Length) {
			return;
		}

		ParticleColorFrame frame = parameters.ColorFrames[frameIndex];
		using Forms.ColorDialog dialog = new() {
			FullOpen = true,
			Color = DrawingColor.FromArgb(
				ToByte(frame.R),
				ToByte(frame.G),
				ToByte(frame.B))
		};

		if (dialog.ShowDialog() != Forms.DialogResult.OK) {
			return;
		}

		parameters.ColorFrames[frameIndex] = new ParticleColorFrame(
			dialog.Color.R / 255.0f,
			dialog.Color.G / 255.0f,
			dialog.Color.B / 255.0f,
			frame.A);
		parameters.ColorKeyFrameBits |= 1u << frameIndex;
		int updatedFrames = ParticleColorFrameInterpolator.RecalculateInBetweenFrames(parameters);
		log.Info($"Color frame {frameIndex} changed via ColorDialog to rgb=({parameters.ColorFrames[frameIndex].R:0.###},{parameters.ColorFrames[frameIndex].G:0.###},{parameters.ColorFrames[frameIndex].B:0.###}).");
		log.Info($"Recalculated {updatedFrames} non-keyed color frames.");
		RebuildSystem();
	}

	private void LoadParticleFile(string path, bool frameCamera) {
		if (!path.EndsWith(".pte.unified.xml", StringComparison.OrdinalIgnoreCase)) {
			status = "Only .pte.unified.xml particle files are supported.";
			log.Warning($"Rejected non-unified particle file '{path}'.");
			return;
		}

		try {
			log.Info($"Loading unified particle file '{path}'.");
			ParticleLoadResult result = ParticleParameterLoader.Load(path);
			parameters = result.Parameters;
			textures = result.Textures;
			displayName = Path.GetFileName(path);
			log.Info($"Loaded parameters: texture='{parameters.TextureName}', freq={parameters.Frequency:0.###}, initial={parameters.InitialParticleCount:0.###}, max={parameters.MaxParticleCount}, life={parameters.Lifetime:0.###}, particleLife={parameters.ParticleLifetime:0.###}, size={parameters.ParticleSize:0.###}, velocity={parameters.ParticleVelocity:0.###}.");
			log.Info($"Decoded texture count: {textures.Count}.");
			RebuildSystem(frameCamera);
			status = $"Loaded {displayName}";
		} catch (Exception ex) {
			parameters = null;
			system = null;
			ClearDeployedParticleSystems();
			textureResource?.Dispose();
			textureResource = null;
			status = ex.Message;
			log.Error(ex.ToString());
		}
	}

	private void RebuildSystem(bool frameCamera = false) {
		if (parameters is null) {
			return;
		}

		textureResource?.Dispose();
		ClearDeployedParticleSystems();
		textureResource = ParticleTextureLoader.Load(textures, parameters.TextureName, log);
		system?.Dispose();
		system = new ParticlePreviewSystem(parameters, BuildMainTransform());
		log.Info($"System rebuilt: maxParticles={system.MaxParticles}, textureValid={textureResource.IsValid}, fallback={textureResource.IsFallback}.");
		if (frameCamera) {
			orbitCamera.Frame(parameters, clampInitialDistance: true);
			log.Info("Camera framed for loaded particle system.");
		}
	}

	private void UpdateDiagnostics(float deltaTime) {
		diagnosticTimer -= deltaTime;
		if (diagnosticTimer > 0.0f || system is null) {
			return;
		}

		diagnosticTimer = 1.0f;
		log.Info($"Runtime: active={ActiveParticleCount}, deployed={deployedSystems.Count}, created={CreatedParticleCount}, drawn={renderer.LastDrawnParticles}, skipped={renderer.LastSkippedParticles}, textureValid={textureResource?.IsValid ?? false}, lifecycle='{system.LifecycleText()}'.");
		if (system.Particles.Count == 0 && system.CreatedParticles == 0 && parameters is not null) {
			log.Warning($"No particles created. Check frequency={parameters.Frequency:0.###} and initial={parameters.InitialParticleCount:0.###}.");
		}
	}

	private string BuildStats() {
		if (parameters is null || system is null) {
			return "No particle system loaded";
		}

		string textureName = string.IsNullOrWhiteSpace(parameters.TextureName) ? "no texture" : parameters.TextureName;
		Vector3 origin = system.Transform.Origin;
		Vector3 scale = system.Transform.Scale;
		return $"{displayName} | {system.LifecycleText()} | {ActiveParticleCount} particles | deployed {deployedSystems.Count} | main pos ({origin.X:0.#},{origin.Y:0.#},{origin.Z:0.#}) rot ({mainTransformRotationDegrees.X:0.#},{mainTransformRotationDegrees.Y:0.#},{mainTransformRotationDegrees.Z:0.#}) scale ({scale.X:0.##},{scale.Y:0.##},{scale.Z:0.##}) | {textureName} -> {textureResource?.Name ?? "fallback sprite"}";
	}

	private IReadOnlyList<ParticlePreviewSystem> ActiveSystems {
		get {
			if (system is null) {
				return deployedSystems;
			}

			return [system, .. deployedSystems];
		}
	}

	private int ActiveParticleCount => (system?.Particles.Count ?? 0) + deployedSystems.Sum(deployedSystem => deployedSystem.Particles.Count);

	private int CreatedParticleCount => (system?.CreatedParticles ?? 0) + deployedSystems.Sum(deployedSystem => deployedSystem.CreatedParticles);

	private void DeployParticleSystem() {
		if (parameters is null) {
			log.Warning("Deploy requested, but no particle parameters are loaded.");
			return;
		}

		Vector3 origin = new(0.0f, 250.0f * (deployedSystems.Count + 1), 0.0f);
		ParticlePreviewSystem deployedSystem = new(parameters, origin);
		deployedSystems.Add(deployedSystem);
		log.Info($"Deployed particle preview {deployedSystems.Count} at origin=({origin.X:0.###},{origin.Y:0.###},{origin.Z:0.###}).");
	}

	private void RemoveLastDeployedParticleSystem() {
		if (deployedSystems.Count == 0) {
			log.Warning("Remove deployed particle requested, but only the original origin system remains.");
			return;
		}

		int index = deployedSystems.Count - 1;
		deployedSystems[index].Dispose();
		deployedSystems.RemoveAt(index);
		log.Info($"Removed deployed particle preview {index + 1}; original origin system remains.");
	}

	private void ClearDeployedParticleSystems() {
		foreach (ParticlePreviewSystem deployedSystem in deployedSystems) {
			deployedSystem.Dispose();
		}

		deployedSystems.Clear();
	}

	private void HandleMainSystemTransformControls(float deltaTime) {
		if (system is null || deltaTime <= 0.0f) {
			return;
		}

		float speedMultiplier = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift)
			? 3.0f
			: 1.0f;
		float moveStep = MainTransformMoveSpeed * speedMultiplier * deltaTime;
		Vector3 translation = Vector3.Zero;
		if (Raylib.IsKeyDown(KeyboardKey.J)) {
			translation.X -= moveStep;
		}

		if (Raylib.IsKeyDown(KeyboardKey.L)) {
			translation.X += moveStep;
		}

		if (Raylib.IsKeyDown(KeyboardKey.U)) {
			translation.Y += moveStep;
		}

		if (Raylib.IsKeyDown(KeyboardKey.O)) {
			translation.Y -= moveStep;
		}

		if (Raylib.IsKeyDown(KeyboardKey.K)) {
			translation.Z -= moveStep;
		}

		if (Raylib.IsKeyDown(KeyboardKey.I)) {
			translation.Z += moveStep;
		}

		if (translation != Vector3.Zero) {
			mainTransformOrigin += translation;
			ApplyMainSystemTransform();
		}

		float yaw = 0.0f;
		if (Raylib.IsKeyDown(KeyboardKey.C)) {
			yaw -= MainTransformRotateSpeed * speedMultiplier * deltaTime;
		}

		if (Raylib.IsKeyDown(KeyboardKey.V)) {
			yaw += MainTransformRotateSpeed * speedMultiplier * deltaTime;
		}

		if (yaw != 0.0f) {
			mainTransformRotationDegrees.Y = WrapDegrees(mainTransformRotationDegrees.Y + RadiansToDegrees(yaw));
			ApplyMainSystemTransform();
		}

		float scaleFactor = 1.0f;
		if (Raylib.IsKeyDown(KeyboardKey.Z)) {
			scaleFactor *= MathF.Exp(-MainTransformScaleSpeed * speedMultiplier * deltaTime);
		}

		if (Raylib.IsKeyDown(KeyboardKey.X)) {
			scaleFactor *= MathF.Exp(MainTransformScaleSpeed * speedMultiplier * deltaTime);
		}

		if (scaleFactor != 1.0f) {
			mainTransformScale = Vector3.Max(new Vector3(0.01f), mainTransformScale * scaleFactor);
			ApplyMainSystemTransform();
		}
	}

	private void ApplyMainSystemTransform() {
		if (system is null) {
			return;
		}

		mainTransformScale = Vector3.Max(new Vector3(0.01f), mainTransformScale);
		system.SetTransform(BuildMainTransform());
	}

	private Transform BuildMainTransform() {
		return new Transform(
			mainTransformOrigin,
			Quaternion.CreateFromYawPitchRoll(
				DegreesToRadians(mainTransformRotationDegrees.Y),
				DegreesToRadians(mainTransformRotationDegrees.X),
				DegreesToRadians(mainTransformRotationDegrees.Z)),
			mainTransformScale);
	}

	private static float DegreesToRadians(float degrees) {
		return degrees * MathF.PI / 180.0f;
	}

	private static float RadiansToDegrees(float radians) {
		return radians * 180.0f / MathF.PI;
	}

	private static float WrapDegrees(float degrees) {
		while (degrees > 180.0f) {
			degrees -= 360.0f;
		}

		while (degrees < -180.0f) {
			degrees += 360.0f;
		}

		return degrees;
	}

	private static bool IsDeployPressed() {
		return Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd);
	}

	private static bool IsUndeployPressed() {
		return Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract);
	}

	private static Rectangle Viewport => new(
		ParticleEditorUi.SampleTreeWidth,
		56.0f,
		Math.Max(1.0f, Raylib.GetScreenWidth() - ParticleEditorUi.SampleTreeWidth - ParticleEditorUi.ParameterPanelWidth),
		Math.Max(1.0f, Raylib.GetScreenHeight() - 56));

	private static int ToByte(float value) {
		return (int)(Math.Clamp(value, 0.0f, 1.0f) * 255.0f);
	}
}
