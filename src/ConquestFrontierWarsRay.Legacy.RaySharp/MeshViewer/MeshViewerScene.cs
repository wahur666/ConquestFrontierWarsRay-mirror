using System.Globalization;
using System.Numerics;
using RaySharp.Mesh;
using RaySharp.Particle;
using Raylib_cs;

namespace RaySharp.MeshViewer;

internal sealed class MeshViewerScene : IScene {
	private const int GlOne = 1;
	private const int GlSrcAlpha = 0x0302;
	private const int GlFuncAdd = 0x8006;
	private const float SampleTreeWidth = 340.0f;
	private const float TransformPanelWidth = 360.0f;
	private const float HeaderHeight = 56.0f;
	private const float SampleTreeFontSize = 15.0f;
	private const float SampleTreeRowHeight = SampleTreeFontSize + 11.0f;
	private const float MainTransformMoveSpeed = 180.0f;
	private const float MainTransformRotateSpeed = 1.4f;
	private const float MainTransformScaleSpeed = 1.0f;
	private const float HardpointMarkerRadiusFactor = 0.012f;
	private const float HardpointMarkerMinRadius = 0.04f;
	private const float HardpointMarkerMaxRadius = 0.35f;
	private const float HardpointLabelFontSize = 13.0f;
	private const float OrientationGizmoSize = 96.0f;
	private const float OrientationGizmoMargin = 18.0f;
	private const float OrientationGizmoAxisLength = 31.0f;

	private readonly SceneContext context;
	private readonly MeshOrbitCamera camera = new();
	private readonly MeshSampleCatalog sampleCatalog = new();
	private readonly MeshTextureLighting textureLighting = MeshTextureLighting.Load();
	private readonly List<MeshViewerModel> sceneModels = [];
	private string status = "Indexing mesh samples...";
	private float sampleTreeScrollOffset;
	private MeshUvMode uvMode = MeshUvMode.Raw;
	private bool showWireframe = true;
	private bool showHardpoints = true;
	private bool useDecodedTextureFlags = true;
	private Vector3 mainTransformOrigin = Vector3.Zero;
	private Vector3 mainTransformRotationDegrees = Vector3.Zero;
	private Vector3 mainTransformScale = Vector3.One;
	private Vector3 particleOriginOffset = Vector3.Zero;
	private int activeTextBoxId;
	private int activeSliderId;
	private int nextTextBoxId;
	private int nextSliderId;
	private string activeTextBoxValue = string.Empty;
	private int lastDrawnParticles;
	private int lastSkippedParticles;
	private readonly Dictionary<string, Vector3> partRotationDegrees = new(StringComparer.OrdinalIgnoreCase);

	public MeshViewerScene(SceneContext context) {
		this.context = context;
		LoadSamples();
	}

	public SceneRequest? Update(float deltaTime) {
		AppWindow.HandleShortcuts(context.WindowState, context.Ui);
		AppWindow.UpdateMouseConfinement(context.WindowState);
		Rectangle viewport = Viewport;
		camera.Update(viewport);
		Camera3D activeCamera = camera.ToCamera();

		HandleSampleShortcuts(deltaTime);
		UpdateModels(deltaTime);

		Raylib.BeginDrawing();
		Raylib.ClearBackground(AppTheme.Background);

		Raylib.BeginScissorMode((int)viewport.X, (int)viewport.Y, (int)viewport.Width, (int)viewport.Height);
		Raylib.BeginMode3D(activeCamera);
		DrawCenteredGrid();
		DrawActiveMesh();
		if (showHardpoints) {
			DrawActiveHardpointMarkers();
		}
		DrawActiveParticles(activeCamera);
		Raylib.EndMode3D();
		if (showHardpoints) {
			DrawActiveHardpointLabels(activeCamera, viewport);
		}
		Raylib.EndScissorMode();
		DrawOrientationGizmo(activeCamera, viewport);

		DrawOverlay();
		Raylib.EndDrawing();

		return null;
	}

	public void Dispose() {
		while (RemoveModelFromScene() is MeshViewerModel model) {
			model.Dispose();
		}

		textureLighting.Unload();
	}

	private void AddModelToScene(MeshViewerModel model) {
		sceneModels.Add(model);
	}

	private void UpdateModels(float deltaTime) {
		foreach (MeshViewerModel model in sceneModels) {
			model.Update(deltaTime);
		}
	}

	private MeshViewerModel? RemoveModelFromScene() {
		if (sceneModels.Count == 0) {
			return null;
		}

		int index = sceneModels.Count - 1;
		MeshViewerModel model = sceneModels[index];
		sceneModels.RemoveAt(index);
		return model;
	}

	private MeshViewerModel? RemoveAddedModelFromScene() {
		return sceneModels.Count > 1 ? RemoveModelFromScene() : null;
	}

	private void LoadSamples() {
		string? samplesDirectory = MeshSampleResolver.ResolveSamplesDirectory();
		if (samplesDirectory is null) {
			status = "Could not find assets/xml_dump.";
			context.Ui.Status = status;
			return;
		}

		string? defaultSample = MeshSampleResolver.Resolve("asteroid3.3db.xml");
		if (!sampleCatalog.LoadFolder(samplesDirectory, defaultSample)) {
			status = $"No .3db.xml, .cmp.xml, or .shield.xml files found in {samplesDirectory}.";
			context.Ui.Status = status;
			return;
		}

		LoadCurrentSample(frameCamera: true);
	}

	private void LoadCurrentSample(bool frameCamera) {
		string? path = sampleCatalog.CurrentPath;
		if (path is null) {
			return;
		}

		try {
			ResetMainTransform();
			MeshViewerModel model = MeshViewerModel.Load(path, uvMode, BuildMainTransform());
			model.SetParticleOriginOffset(particleOriginOffset);
			while (RemoveModelFromScene() is MeshViewerModel previousModel) {
				previousModel.Dispose();
			}

			AddModelToScene(model);
			status = $"Loaded {model.Geometry.Name} ({uvMode} UV)";
			context.Ui.Status = status;
			Console.WriteLine($"[MeshViewer] {model.Geometry.Name}: {model.Geometry.Diagnostics.Summary(model.UploadedTextureCount)}");
			if (frameCamera) {
				camera.Frame(CombinedSceneBounds);
			}
			EnsureCurrentSampleVisible();
		} catch (Exception ex) {
			status = ex.Message;
			context.Ui.Status = status;
		}
	}

	private void ResetMainTransform() {
		mainTransformOrigin = Vector3.Zero;
		mainTransformRotationDegrees = Vector3.Zero;
		mainTransformScale = Vector3.One;
		activeTextBoxId = 0;
		activeTextBoxValue = string.Empty;
		partRotationDegrees.Clear();
	}

	private void LoadPreviousSample() {
		if (sampleCatalog.MovePrevious() is null) {
			return;
		}

		LoadCurrentSample(frameCamera: true);
	}

	private void LoadNextSample() {
		if (sampleCatalog.MoveNext() is null) {
			return;
		}

		LoadCurrentSample(frameCamera: true);
	}

	private void LoadSampleAtIndex(int index) {
		if (!sampleCatalog.SetCurrentIndex(index)) {
			return;
		}

		LoadCurrentSample(frameCamera: true);
	}

	private void HandleSampleShortcuts(float deltaTime) {
		bool textEditing = IsTextEditing;
		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.F) && PrimaryModel is not null) {
			camera.Frame(CombinedSceneBounds);
		}

		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.U)) {
			uvMode = uvMode == MeshUvMode.FlipV ? MeshUvMode.Raw : MeshUvMode.FlipV;
			LoadCurrentSample(frameCamera: false);
		}

		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.W)) {
			showWireframe = !showWireframe;
		}

		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.H)) {
			showHardpoints = !showHardpoints;
		}

		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.T)) {
			useDecodedTextureFlags = !useDecodedTextureFlags;
		}

		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.F9)) {
			CaptureDiagnostics("manual F9");
		}

		if (!textEditing) {
			HandleMainModelTransformControls(deltaTime);
		}

		if (!textEditing && IsAddModelPressed()) {
			AddTranslatedModel();
		} else if (!textEditing && IsRemoveModelPressed()) {
			RemoveAddedModel();
		}

		if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.Left)) {
			LoadPreviousSample();
		} else if (!textEditing && Raylib.IsKeyPressed(KeyboardKey.Right)) {
			LoadNextSample();
		}
	}

	private void AddTranslatedModel() {
		string? path = sampleCatalog.CurrentPath;
		if (path is null) {
			return;
		}

		try {
			MeshViewerModel model = MeshViewerModel.Load(path, uvMode, origin: new Vector3(0.0f, 5.0f, 0.0f) * sceneModels.Count, rotation: Quaternion.Identity, scale: Vector3.One);
			model.SetParticleOriginOffset(particleOriginOffset);
			AddModelToScene(model);
			status = $"Added {model.Geometry.Name} at (0, 5, 0). Models: {sceneModels.Count}";
			context.Ui.Status = status;
		} catch (Exception ex) {
			status = ex.Message;
			context.Ui.Status = status;
		}
	}

	private void RemoveAddedModel() {
		MeshViewerModel? model = RemoveAddedModelFromScene();
		if (model is null) {
			status = "Base model cannot be removed.";
			context.Ui.Status = status;
			return;
		}

		string name = model.Geometry.Name;
		model.Dispose();
		status = $"Removed {name}. Models: {sceneModels.Count}";
		context.Ui.Status = status;
	}

	private static bool IsAddModelPressed() {
		return Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd);
	}

	private static bool IsRemoveModelPressed() {
		return Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract);
	}

	private void HandleMainModelTransformControls(float deltaTime) {
		if (PrimaryModel is null || deltaTime <= 0.0f) {
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
			ApplyMainModelTransform();
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
			ApplyMainModelTransform();
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
			ApplyMainModelTransform();
		}
	}

	private void ApplyMainModelTransform() {
		MeshViewerModel? model = PrimaryModel;
		if (model is null) {
			return;
		}

		mainTransformScale = Vector3.Max(new Vector3(0.01f), mainTransformScale);
		model.SetTransform(BuildMainTransform());
	}

	private void ApplyParticleOriginOffset() {
		foreach (MeshViewerModel model in sceneModels) {
			model.SetParticleOriginOffset(particleOriginOffset);
		}
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

	private void DrawActiveMesh() {
		if (sceneModels.Count == 0) {
			return;
		}

		foreach (MeshViewerModel model in sceneModels) {
			DrawModel(model);
		}
	}

	private void DrawActiveParticles(Camera3D activeCamera) {
		lastDrawnParticles = 0;
		lastSkippedParticles = 0;
		if (sceneModels.Count == 0) {
			return;
		}

		foreach (MeshViewerModel model in sceneModels) {
			foreach (MeshViewerParticleSystem particleSystem in model.ParticleSystems) {
				DrawParticleSystem(activeCamera, particleSystem);
			}
		}
	}

	private void DrawActiveHardpointMarkers() {
		foreach (MeshViewerModel model in sceneModels) {
			float radius = HardpointMarkerRadius(model);
			foreach (MeshHardpoint hardpoint in model.Geometry.Hardpoints) {
				Vector3 position = HardpointWorldPosition(model, hardpoint);
				Raylib.DrawSphere(position, radius, new Color(255, 196, 74, 235));
				Raylib.DrawSphereWires(position, radius, 12, 12, new Color(72, 28, 6, 210));
			}
		}
	}

	private void DrawActiveHardpointLabels(Camera3D activeCamera, Rectangle viewport) {
		Vector3 cameraForward = Vector3.Normalize(activeCamera.Target - activeCamera.Position);
		foreach (MeshViewerModel model in sceneModels) {
			float radius = HardpointMarkerRadius(model);
			foreach (MeshHardpoint hardpoint in model.Geometry.Hardpoints
				         .OrderByDescending(hardpoint => Vector3.DistanceSquared(HardpointWorldPosition(model, hardpoint), activeCamera.Position))) {
				Vector3 position = HardpointWorldPosition(model, hardpoint);
				if (Vector3.Dot(position - activeCamera.Position, cameraForward) <= 0.0f) {
					continue;
				}

				Vector2 screen = Raylib.GetWorldToScreen(position, activeCamera);
				screen.Y -= MathF.Max(18.0f, radius * 18.0f);
				if (!Raylib.CheckCollisionPointRec(screen, viewport)) {
					continue;
				}

				DrawHardpointLabel(hardpoint.Name, screen);
			}
		}
	}

	private void DrawHardpointLabel(string name, Vector2 anchor) {
		Vector2 textSize = Raylib.MeasureTextEx(context.UiFont.Font, name, HardpointLabelFontSize, UiFont.Spacing);
		Rectangle background = new(
			anchor.X - textSize.X * 0.5f - 7.0f,
			anchor.Y - textSize.Y - 8.0f,
			textSize.X + 14.0f,
			textSize.Y + 8.0f);
		Raylib.DrawRectangleRec(background, new Color(12, 15, 18, 214));
		Raylib.DrawRectangleLinesEx(background, 1.0f, new Color(255, 196, 74, 210));
		Raylib.DrawTextEx(
			context.UiFont.Font,
			name,
			new Vector2(background.X + 7.0f, background.Y + 4.0f),
			HardpointLabelFontSize,
			UiFont.Spacing,
			new Color(255, 238, 196, 255));
	}

	private void DrawOrientationGizmo(Camera3D activeCamera, Rectangle viewport) {
		float size = Math.Min(OrientationGizmoSize, Math.Max(54.0f, Math.Min(viewport.Width, viewport.Height) - OrientationGizmoMargin * 2.0f));
		if (size <= 0.0f) {
			return;
		}

		Vector2 center = new(
			viewport.X + viewport.Width - OrientationGizmoMargin - size * 0.5f,
			viewport.Y + OrientationGizmoMargin + size * 0.5f);
		Rectangle background = new(center.X - size * 0.5f, center.Y - size * 0.5f, size, size);
		Raylib.DrawRectangleRec(background, new Color(12, 15, 18, 164));
		Raylib.DrawRectangleLinesEx(background, 1.0f, new Color(93, 109, 132, 155));
		Raylib.DrawCircleV(center, 3.0f, new Color(226, 232, 240, 230));

		Vector3 forward = NormalizeOrDefault(activeCamera.Target - activeCamera.Position, Vector3.UnitZ);
		Vector3 right = NormalizeOrDefault(Vector3.Cross(forward, activeCamera.Up), Vector3.UnitX);
		Vector3 up = NormalizeOrDefault(Vector3.Cross(right, forward), Vector3.UnitY);
		DrawOrientationGizmoAxis(center, forward, right, up, Vector3.UnitX, "X", new Color(235, 89, 91, 255));
		DrawOrientationGizmoAxis(center, forward, right, up, Vector3.UnitY, "Y", new Color(104, 214, 128, 255));
		DrawOrientationGizmoAxis(center, forward, right, up, Vector3.UnitZ, "Z", new Color(92, 157, 255, 255));
	}

	private void DrawOrientationGizmoAxis(
		Vector2 center,
		Vector3 cameraForward,
		Vector3 cameraRight,
		Vector3 cameraUp,
		Vector3 axis,
		string label,
		Color color) {
		float depth = Vector3.Dot(axis, cameraForward);
		float alpha = depth < 0.0f ? 0.58f : 1.0f;
		Vector2 direction = new(Vector3.Dot(axis, cameraRight), -Vector3.Dot(axis, cameraUp));
		if (direction.LengthSquared() <= 0.000001f) {
			direction = new Vector2(0.0f, -1.0f);
		} else {
			direction = Vector2.Normalize(direction);
		}

		Vector2 end = center + direction * OrientationGizmoAxisLength;
		Color lineColor = FadeColor(color, alpha);
		Raylib.DrawLineEx(center, end, depth < 0.0f ? 2.0f : 3.0f, lineColor);
		Raylib.DrawCircleV(end, depth < 0.0f ? 4.5f : 5.5f, lineColor);
		Vector2 labelSize = Raylib.MeasureTextEx(context.UiFont.Font, label, 13.0f, UiFont.Spacing);
		Raylib.DrawTextEx(
			context.UiFont.Font,
			label,
			new Vector2(end.X - labelSize.X * 0.5f, end.Y - labelSize.Y * 0.5f),
			13.0f,
			UiFont.Spacing,
			Color.White);
	}

	private void DrawParticleSystem(Camera3D camera, MeshViewerParticleSystem particleSystem) {
		if (!particleSystem.TextureResource.IsValid || particleSystem.System.Particles.Count == 0) {
			return;
		}

		Vector3 cameraForward = Vector3.Normalize(camera.Target - camera.Position);
		Rlgl.DisableBackfaceCulling();
		Rlgl.DisableDepthMask();
		Raylib.BeginBlendMode(SelectParticleBlendMode(particleSystem.System.Parameters));
		foreach (ParticleInstance particle in particleSystem.System.Particles
			         .OrderByDescending(particle => Vector3.Dot(particleSystem.System.TransformPosition(particle.Position) - camera.Position, cameraForward))) {
			ParticleColorFrame frame = particleSystem.System.ParticleColor(particle);
			float size = particleSystem.System.TransformSize(particle.Size);
			if (size <= 0.01f) {
				lastSkippedParticles++;
				continue;
			}

			Color tint = new(
				ToByte(frame.R),
				ToByte(frame.G),
				ToByte(frame.B),
				255);
			Raylib.DrawBillboard(
				camera,
				particleSystem.TextureResource.Texture,
				particleSystem.System.TransformPosition(particle.Position),
				Math.Max(0.01f, size),
				tint);
			lastDrawnParticles++;
		}

		Raylib.EndBlendMode();
		Rlgl.EnableDepthMask();
		Rlgl.EnableBackfaceCulling();
	}

	private void DrawModel(MeshViewerModel model) {
		MeshGeometry activeMesh = model.Geometry;
		Color fill = new(146, 155, 166, 255);
		Color wire = new(35, 42, 52, 145);
		foreach ((IReadOnlyList<MeshTriangle> triangles, Matrix4x4 world, Matrix4x4 normal) in RenderBatches(model)) {
			foreach (MeshTriangle triangle in triangles) {
				MeshTextureResource? texture = MeshTextureLoader.Find(model.Textures, triangle.TextureName);
				if (texture is not null && texture.IsValid) {
					DrawTexturedTriangle(world, normal, triangle, texture.Texture, textureLighting, useDecodedTextureFlags);
				} else {
					if (triangle.UsesAlphaBlend) {
						Raylib.BeginBlendMode(BlendMode.Alpha);
					}

					if (!triangle.WritesDepth) {
						Rlgl.DisableDepthMask();
					}

					LitRenderer.DrawTriangle(
						TransformPosition(world, triangle.A),
						TransformPosition(world, triangle.B),
						TransformPosition(world, triangle.C),
						triangle.TextureName is null ? triangle.Tint : fill);

					if (!triangle.WritesDepth) {
						Rlgl.EnableDepthMask();
					}

					if (triangle.UsesAlphaBlend) {
						Raylib.EndBlendMode();
					}
				}
			}

			foreach (MeshTriangle triangle in triangles) {
				MeshTextureResource? secondDiffuseTexture = MeshTextureLoader.Find(model.Textures, triangle.SecondDiffuseTextureName);
				if (secondDiffuseTexture is not null && secondDiffuseTexture.IsValid) {
					DrawSecondDiffuseTriangle(world, normal, triangle, secondDiffuseTexture.Texture, useDecodedTextureFlags);
				}
			}

			foreach (MeshTriangle triangle in triangles) {
				MeshTextureResource? emissiveTexture = MeshTextureLoader.Find(model.Textures, triangle.EmissiveTextureName);
				if (emissiveTexture is not null && emissiveTexture.IsValid) {
					DrawEmissiveTriangle(world, normal, triangle, emissiveTexture.Texture, useDecodedTextureFlags);
				}
			}

			if (showWireframe) {
				foreach (MeshTriangle triangle in triangles) {
					Vector3 a = TransformPosition(world, triangle.A);
					Vector3 b = TransformPosition(world, triangle.B);
					Vector3 c = TransformPosition(world, triangle.C);
					Raylib.DrawLine3D(a, b, wire);
					Raylib.DrawLine3D(b, c, wire);
					Raylib.DrawLine3D(c, a, wire);
				}
			}
		}
	}

	private static void DrawTexturedTriangle(
		Matrix4x4 world,
		Matrix4x4 normalMatrix,
		MeshTriangle triangle,
		Texture2D texture,
		MeshTextureLighting lighting,
		bool useDecodedTextureFlags) {
		if (triangle.UsesAlphaBlend) {
			Raylib.BeginBlendMode(BlendMode.Alpha);
		}

		if (!triangle.WritesDepth) {
			Rlgl.DisableDepthMask();
		}

		lighting.Begin();
		MeshTextureAddress textureAddress = ResolveTextureAddress(triangle.TextureAddress, useDecodedTextureFlags);
		ApplyTextureAddress(texture, textureAddress);
		Rlgl.SetTexture(texture.Id);
		Rlgl.Begin((int)DrawMode.Triangles);
		Rlgl.Color4ub(triangle.Tint.R, triangle.Tint.G, triangle.Tint.B, triangle.Tint.A);
		EmitTexturedVertex(world, normalMatrix, triangle.A, SelectUv(triangle.UvA, triangle.Uv2A, textureAddress), triangle.NormalA);
		EmitTexturedVertex(world, normalMatrix, triangle.B, SelectUv(triangle.UvB, triangle.Uv2B, textureAddress), triangle.NormalB);
		EmitTexturedVertex(world, normalMatrix, triangle.C, SelectUv(triangle.UvC, triangle.Uv2C, textureAddress), triangle.NormalC);
		Rlgl.End();
		Rlgl.SetTexture(0);
		lighting.End();

		if (!triangle.WritesDepth) {
			Rlgl.EnableDepthMask();
		}

		if (triangle.UsesAlphaBlend) {
			Raylib.EndBlendMode();
		}
	}

	private static void DrawSecondDiffuseTriangle(Matrix4x4 world, Matrix4x4 normalMatrix, MeshTriangle triangle, Texture2D texture, bool useDecodedTextureFlags) {
		Raylib.BeginBlendMode(BlendMode.Multiplied);
		Rlgl.DisableDepthMask();
		MeshTextureAddress textureAddress = ResolveTextureAddress(triangle.SecondDiffuseTextureAddress, useDecodedTextureFlags);
		ApplyTextureAddress(texture, textureAddress);
		Rlgl.SetTexture(texture.Id);
		Rlgl.Begin((int)DrawMode.Triangles);
		Rlgl.Color4ub(255, 255, 255, 255);
		EmitTexturedVertex(world, normalMatrix, triangle.A, SelectUv(triangle.UvA, triangle.Uv2A, textureAddress), triangle.NormalA);
		EmitTexturedVertex(world, normalMatrix, triangle.B, SelectUv(triangle.UvB, triangle.Uv2B, textureAddress), triangle.NormalB);
		EmitTexturedVertex(world, normalMatrix, triangle.C, SelectUv(triangle.UvC, triangle.Uv2C, textureAddress), triangle.NormalC);
		Rlgl.End();
		Rlgl.SetTexture(0);
		Rlgl.EnableDepthMask();
		Raylib.EndBlendMode();
	}

	private static void DrawEmissiveTriangle(Matrix4x4 world, Matrix4x4 normalMatrix, MeshTriangle triangle, Texture2D texture, bool useDecodedTextureFlags) {
		Rlgl.SetBlendFactors(GlSrcAlpha, GlOne, GlFuncAdd);
		Raylib.BeginBlendMode(BlendMode.Custom);
		Rlgl.DisableDepthTest();
		Rlgl.DisableDepthMask();
		MeshTextureAddress textureAddress = ResolveTextureAddress(triangle.EmissiveTextureAddress, useDecodedTextureFlags);
		ApplyTextureAddress(texture, textureAddress);
		Rlgl.SetTexture(texture.Id);
		Rlgl.Begin((int)DrawMode.Triangles);
		Rlgl.Color4ub(255, 255, 255, triangle.EmissiveBlend);
		EmitTexturedVertex(world, normalMatrix, triangle.A, SelectUv(triangle.UvA, triangle.Uv2A, textureAddress), triangle.NormalA);
		EmitTexturedVertex(world, normalMatrix, triangle.B, SelectUv(triangle.UvB, triangle.Uv2B, textureAddress), triangle.NormalB);
		EmitTexturedVertex(world, normalMatrix, triangle.C, SelectUv(triangle.UvC, triangle.Uv2C, textureAddress), triangle.NormalC);
		Rlgl.End();
		Rlgl.SetTexture(0);
		Rlgl.EnableDepthMask();
		Rlgl.EnableDepthTest();
		Raylib.EndBlendMode();
	}

	private static void ApplyTextureAddress(Texture2D texture, MeshTextureAddress address) {
		Raylib.SetTextureWrap(texture, ToRaylibTextureWrap(address));
	}

	private static MeshTextureAddress ResolveTextureAddress(MeshTextureAddress address, bool useDecodedTextureFlags) {
		return useDecodedTextureFlags ? address : MeshTextureAddress.Repeat;
	}

	private static Vector2 SelectUv(Vector2 uv0, Vector2 uv1, MeshTextureAddress address) {
		return address.CoordinateSet == 1 ? uv1 : uv0;
	}

	private static TextureWrap ToRaylibTextureWrap(MeshTextureAddress address) {
		if (address.U == 1 || address.V == 1) {
			return TextureWrap.MirrorRepeat;
		}

		if (address.U == 2 || address.V == 2 || address.U == 3 || address.V == 3) {
			return TextureWrap.Clamp;
		}

		return TextureWrap.Repeat;
	}

	private static void EmitTexturedVertex(Matrix4x4 world, Matrix4x4 normalMatrix, Vector3 position, Vector2 uv, Vector3 normal) {
		Vector3 transformedPosition = TransformPosition(world, position);
		Vector3 transformedNormal = TransformNormal(normalMatrix, normal);
		Rlgl.TexCoord2f(uv.X, uv.Y);
		Rlgl.Normal3f(transformedNormal.X, transformedNormal.Y, transformedNormal.Z);
		Rlgl.Vertex3f(transformedPosition.X, transformedPosition.Y, transformedPosition.Z);
	}

	private static IEnumerable<(IReadOnlyList<MeshTriangle> Triangles, Matrix4x4 World, Matrix4x4 Normal)> RenderBatches(MeshViewerModel model) {
		if (model.Geometry.Parts.Count == 0) {
			yield return (model.Geometry.Triangles, model.WorldMatrix, model.NormalMatrix);
			yield break;
		}

		foreach (MeshPartGeometry part in model.Geometry.Parts) {
			Matrix4x4 compoundWorld = model.PartWorldTransforms.GetValueOrDefault(part.Name, part.LocalTransform);
			Matrix4x4 world = compoundWorld * model.WorldMatrix;
			yield return (part.Triangles, world, NormalMatrix(world));
		}
	}

	private static Matrix4x4 NormalMatrix(Matrix4x4 world) {
		return Matrix4x4.Invert(world, out Matrix4x4 inverseWorld)
			? Matrix4x4.Transpose(inverseWorld)
			: Matrix4x4.Identity;
	}

	private static Vector3 TransformPosition(Matrix4x4 world, Vector3 position) {
		return Vector3.Transform(position, world);
	}

	private static Vector3 TransformNormal(Matrix4x4 normalMatrix, Vector3 normal) {
		Vector3 transformed = Vector3.TransformNormal(normal, normalMatrix);
		return transformed == Vector3.Zero ? normal : Vector3.Normalize(transformed);
	}

	private static Vector3 HardpointWorldPosition(MeshViewerModel model, MeshHardpoint hardpoint) {
		Matrix4x4 localToModel = hardpoint.ParentName is not null
			? model.PartWorldTransforms.GetValueOrDefault(hardpoint.ParentName, Matrix4x4.Identity)
			: Matrix4x4.Identity;
		return Vector3.Transform(hardpoint.Position, localToModel * model.WorldMatrix);
	}

	private static float HardpointMarkerRadius(MeshViewerModel model) {
		Vector3 size = model.Bounds.Max - model.Bounds.Min;
		float diagonal = size.Length();
		return Math.Clamp(diagonal * HardpointMarkerRadiusFactor, HardpointMarkerMinRadius, HardpointMarkerMaxRadius);
	}

	private void DrawOverlay() {
		Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), AppTheme.HeaderHeight, AppTheme.HeaderBackground);
		Raylib.DrawLine(0, AppTheme.HeaderHeight, Raylib.GetScreenWidth(), AppTheme.HeaderHeight, AppTheme.HeaderBorder);
		Raylib.DrawTextEx(context.UiFont.Font, "RaySharp Mesh Viewer", new Vector2(14.0f, 15.0f), 18.0f, UiFont.Spacing, AppTheme.TextPrimary);
		Raylib.DrawTextEx(
			context.UiFont.Font,
			"Drag: orbit | Shift/middle drag: pan | Wheel: zoom | F: frame | U: UV mode | W: wire | H: hardpoints | T: tex flags | F9: snapshot | +/-: add/remove model | Left/Right: sample | F8: confine mouse | F11: fullscreen",
			new Vector2(14.0f, 34.0f),
			14.0f,
			UiFont.Spacing,
			AppTheme.TextMuted);
		Raylib.DrawTextEx(context.UiFont.Font, $"{Raylib.GetFPS()} FPS", new Vector2(Raylib.GetScreenWidth() - 82.0f, 20.0f), 13.0f, UiFont.Spacing, AppTheme.TextMuted);
		DrawSampleTree();
		DrawTransformPanel();
		DrawHud();
	}

	private void CaptureDiagnostics(string reason, bool updateStatus = true) {
		string logPath = MeshViewerDiagnostics.Snapshot(
			reason,
			status,
			uvMode,
			useDecodedTextureFlags,
			showWireframe,
			particleOriginOffset,
			camera,
			camera.ToCamera(),
			sceneModels,
			lastDrawnParticles,
			lastSkippedParticles);
		if (!updateStatus) {
			return;
		}

		status = $"Mesh diagnostics captured: {logPath}";
		context.Ui.Status = status;
	}

	private void DrawTransformPanel() {
		Rectangle panel = TransformPanel;
		nextTextBoxId = 1;
		nextSliderId = 1;
		Raylib.DrawRectangleRec(panel, new Color(21, 25, 32, 245));
		Raylib.DrawLine((int)panel.X, (int)panel.Y, (int)panel.X, (int)(panel.Y + panel.Height), AppTheme.HeaderBorder);
		DrawText("Main Model Transform", panel.X + 16.0f, panel.Y + 14.0f, 18.0f, AppTheme.TextPrimary);
		DrawText("Only model 1 is affected", panel.X + 16.0f, panel.Y + 42.0f, 13.0f, AppTheme.TextMuted);
		DrawText("IJKL/UO move | CV yaw | ZX scale", panel.X + 16.0f, panel.Y + 64.0f, 13.0f, AppTheme.TextMuted);

		float y = panel.Y + 104.0f;
		bool changed = false;
		changed |= VectorInputs("Origin", panel.X + 16.0f, ref y, ref mainTransformOrigin);
		changed |= VectorInputs("Rotation", panel.X + 16.0f, ref y, ref mainTransformRotationDegrees);
		changed |= VectorInputs("Scale", panel.X + 16.0f, ref y, ref mainTransformScale);
		if (changed) {
			ApplyMainModelTransform();
		}

		y += 14.0f;
		DrawAnimationControls(panel.X + 16.0f, ref y);

		y += 14.0f;
		DrawText("Particle Preview", panel.X + 16.0f, y, 15.0f, AppTheme.TextPrimary);
		y += 26.0f;

		if (VectorInputs("P Origin", panel.X + 16.0f, ref y, ref particleOriginOffset)) {
			ApplyParticleOriginOffset();
		}

		y += 14.0f;
		DrawPartRotationControls(panel.X + 16.0f, ref y);
	}

	private void DrawAnimationControls(float x, ref float y) {
		MeshViewerAnimationController? animation = PrimaryModel?.Animation;
		DrawText("Animation", x, y, 15.0f, AppTheme.TextPrimary);
		y += 24.0f;
		if (animation is null) {
			DrawText("No clips", x, y, 13.0f, AppTheme.TextMuted);
			y += 24.0f;
			return;
		}

		MeshAnimationClip? clip = animation.ActiveClip;
		string clipText = clip is null
			? "No active clip"
			: $"{animation.ActiveClipIndex + 1}/{animation.ClipCount} {clip.Name}";
		if (Button(new Rectangle(x, y - 3.0f, 32.0f, 24.0f), "<", animation.ClipCount > 1)) {
			animation.SelectClip(animation.ActiveClipIndex - 1);
		}

		Raylib.DrawRectangleRec(new Rectangle(x + 38.0f, y - 3.0f, 234.0f, 24.0f), new Color(25, 30, 38, 255));
		Raylib.DrawRectangleLinesEx(new Rectangle(x + 38.0f, y - 3.0f, 234.0f, 24.0f), 1.0f, new Color(58, 67, 81, 255));
		DrawText(clipText, x + 44.0f, y + 3.0f, 13.0f, AppTheme.TextPrimary);
		if (Button(new Rectangle(x + 278.0f, y - 3.0f, 32.0f, 24.0f), ">", animation.ClipCount > 1)) {
			animation.SelectClip(animation.ActiveClipIndex + 1);
		}

		y += 31.0f;
		if (Button(new Rectangle(x, y - 3.0f, 58.0f, 24.0f), "Start", !animation.Playing)) {
			animation.Play();
		}

		if (Button(new Rectangle(x + 64.0f, y - 3.0f, 54.0f, 24.0f), "Stop", animation.Playing)) {
			animation.Stop();
		}

		if (Button(new Rectangle(x + 124.0f, y - 3.0f, 70.0f, 24.0f), "Restart", true)) {
			animation.Restart();
		}

		if (Button(new Rectangle(x + 200.0f, y - 3.0f, 58.0f, 24.0f), "Reset", true)) {
			animation.Reset();
		}

		y += 31.0f;
		bool loop = animation.Loop;
		if (Toggle(new Rectangle(x, y - 3.0f, 84.0f, 24.0f), "Loop", ref loop)) {
			animation.Loop = loop;
		}

		bool pingPong = animation.PingPong;
		if (Toggle(new Rectangle(x + 92.0f, y - 3.0f, 118.0f, 24.0f), "Ping-pong", ref pingPong)) {
			animation.PingPong = pingPong;
		}

		y += 31.0f;
		string statusText = clip is null
			? "Stopped"
			: $"{(animation.Playing ? "Playing" : "Stopped")} {Format(animation.Time)} / {Format(clip.Duration)}s, {clip.Tracks.Count} tracks";
		DrawText(statusText, x, y, 13.0f, AppTheme.TextMuted);
		y += 24.0f;
	}

	private void DrawPartRotationControls(float x, ref float y) {
		MeshViewerModel? model = PrimaryModel;
		DrawText("Part Rotations", x, y, 15.0f, AppTheme.TextPrimary);
		y += 24.0f;
		if (model is null || model.Geometry.Parts.Count == 0) {
			DrawText("No parts", x, y, 13.0f, AppTheme.TextMuted);
			y += 24.0f;
			return;
		}

		foreach (MeshPartGeometry part in model.Geometry.Parts) {
			bool hasOverride = partRotationDegrees.ContainsKey(part.Name);
			DrawText(part.Name, x, y, 13.0f, hasOverride ? AppTheme.TextPrimary : AppTheme.TextMuted);
			if (Button(new Rectangle(x + 290.0f, y - 2.0f, 30.0f, 20.0f), "R", hasOverride)) {
				partRotationDegrees.Remove(part.Name);
				model.ClearPartRotation(part.Name);
			}

			y += 20.0f;
			if (!partRotationDegrees.TryGetValue(part.Name, out Vector3 deg)) {
				deg = Vector3.Zero;
			}

			bool changed = VectorInputs("Rot", x, ref y, ref deg);
			if (changed) {
				partRotationDegrees[part.Name] = deg;
				Quaternion rotation = Quaternion.CreateFromYawPitchRoll(
					DegreesToRadians(deg.Y),
					DegreesToRadians(deg.X),
					DegreesToRadians(deg.Z));
				model.SetPartRotation(part.Name, rotation);
			}
		}
	}

	private void DrawSampleTree() {
		Rectangle panel = SampleTreePanel;
		Raylib.DrawRectangleRec(panel, new Color(18, 22, 28, 245));
		Raylib.DrawLine((int)(panel.X + panel.Width), (int)panel.Y, (int)(panel.X + panel.Width), (int)(panel.Y + panel.Height), AppTheme.HeaderBorder);
		DrawText("Mesh Samples", panel.X + 16.0f, panel.Y + 14.0f, 18.0f, AppTheme.TextPrimary);
		DrawText($"v {Path.GetFileName(sampleCatalog.Folder ?? "No folder loaded")}", panel.X + 16.0f, panel.Y + 44.0f, 13.0f, AppTheme.TextMuted);

		Rectangle clip = SampleTreeClip;
		float contentHeight = SampleTreeContentHeight;
		UpdateSampleTreeScroll(clip, contentHeight);

		Vector2 mouse = Raylib.GetMousePosition();
		float y = clip.Y - sampleTreeScrollOffset;
		Raylib.BeginScissorMode((int)clip.X, (int)clip.Y, (int)clip.Width, (int)clip.Height);
		for (int rowIndex = 0; rowIndex < sampleCatalog.TreeRows.Count; rowIndex++) {
			MeshSampleTreeRow treeRow = sampleCatalog.TreeRows[rowIndex];
			Rectangle row = new(clip.X + 4.0f, y, clip.Width - 12.0f, SampleTreeRowHeight - 2.0f);
			bool current = !treeRow.IsFolder && treeRow.FileIndex == sampleCatalog.CurrentIndex;
			bool clickable = !treeRow.IsFolder;
			bool hot = clickable && Raylib.CheckCollisionPointRec(mouse, row);
			if (current) {
				Raylib.DrawRectangleRec(row, new Color(51, 86, 111, 255));
			} else if (hot) {
				Raylib.DrawRectangleRec(row, new Color(38, 47, 58, 255));
			}

			float indent = treeRow.Indent * 18.0f;
			string prefix = treeRow.IsFolder ? "v " : current ? "> " : "  ";
			DrawText(
				$"{prefix}{treeRow.Name}",
				row.X + 6.0f + indent,
				row.Y + 4.0f,
				SampleTreeFontSize,
				current ? new Color(241, 244, 248, 255) : treeRow.IsFolder ? AppTheme.TextPrimary : AppTheme.TextMuted);
			if (hot && Raylib.IsMouseButtonPressed(MouseButton.Left)) {
				LoadSampleAtIndex(treeRow.FileIndex);
			}

			y += SampleTreeRowHeight;
		}
		Raylib.EndScissorMode();

		DrawScrollbar(clip, contentHeight);
	}

	private void EnsureCurrentSampleVisible() {
		int rowIndex = sampleCatalog.CurrentRowIndex;
		if (rowIndex < 0) {
			return;
		}

		EnsureSampleTreeSelectionVisible(SampleTreeClip, SampleTreeContentHeight, rowIndex);
	}

	private void UpdateSampleTreeScroll(Rectangle clip, float contentHeight) {
		float maxScroll = Math.Max(0.0f, contentHeight - clip.Height);
		Vector2 mouse = Raylib.GetMousePosition();
		if (Raylib.CheckCollisionPointRec(mouse, clip)) {
			float wheel = Raylib.GetMouseWheelMove();
			if (wheel != 0.0f) {
				sampleTreeScrollOffset -= wheel * 34.0f;
			}
		}

		sampleTreeScrollOffset = Math.Clamp(sampleTreeScrollOffset, 0.0f, maxScroll);
	}

	private void EnsureSampleTreeSelectionVisible(Rectangle clip, float contentHeight, int selectedIndex) {
		float maxScroll = Math.Max(0.0f, contentHeight - clip.Height);
		float rowTop = Math.Max(0, selectedIndex) * SampleTreeRowHeight;
		float rowBottom = rowTop + SampleTreeRowHeight;
		if (rowTop < sampleTreeScrollOffset) {
			sampleTreeScrollOffset = rowTop;
		} else if (rowBottom > sampleTreeScrollOffset + clip.Height) {
			sampleTreeScrollOffset = rowBottom - clip.Height;
		}

		sampleTreeScrollOffset = Math.Clamp(sampleTreeScrollOffset, 0.0f, maxScroll);
	}

	private void DrawScrollbar(Rectangle clip, float contentHeight) {
		if (contentHeight <= clip.Height) {
			return;
		}

		float trackX = clip.X + clip.Width - 5.0f;
		Rectangle track = new(trackX, clip.Y + 2.0f, 4.0f, clip.Height - 4.0f);
		float thumbHeight = Math.Max(24.0f, track.Height * clip.Height / contentHeight);
		float maxThumbTravel = track.Height - thumbHeight;
		float maxScroll = contentHeight - clip.Height;
		float thumbY = track.Y + maxThumbTravel * (sampleTreeScrollOffset / maxScroll);
		Raylib.DrawRectangleRec(track, new Color(45, 52, 63, 255));
		Raylib.DrawRectangleRec(new Rectangle(track.X, thumbY, track.Width, thumbHeight), new Color(93, 183, 255, 220));
	}

	private void DrawHud() {
		int screenHeight = Raylib.GetScreenHeight();
		DrawPanelText(status, 18.0f, screenHeight - 72.0f, 14.0f);
		MeshGeometry? activeMesh = PrimaryModel?.Geometry;
		string animationStats = PrimaryModel?.Animation is MeshViewerAnimationController animation
			? $" | anim {animation.ActiveClipIndex + 1}/{animation.ClipCount} {(animation.Playing ? "play" : "stop")}"
			: string.Empty;
		string stats = activeMesh is null
			? sampleCatalog.Summary
			: $"{sceneModels.Count} models | {activeMesh.VertexCount} vertices | {activeMesh.FaceCount} faces | {activeMesh.MaterialCount} materials | {activeMesh.TextureCount} textures | {activeMesh.Triangles.Count * sceneModels.Count} drawn triangles | hardpoints {TotalHardpointCount} {(showHardpoints ? "on" : "off")}{animationStats} | particles {TotalParticleSystemCount}/{TotalActiveParticleCount} drawn {lastDrawnParticles} origin {FormatVector(particleOriginOffset)} | UV {uvMode} | TexFlags {(useDecodedTextureFlags ? "on" : "legacy")} | Wire {(showWireframe ? "on" : "off")}";
		DrawPanelText(stats, 18.0f, screenHeight - 38.0f, 14.0f);
		if (activeMesh is not null && PrimaryModel is not null) {
			DrawPanelText(activeMesh.Diagnostics.Summary(TotalUploadedTextureCount), 18.0f, screenHeight - 106.0f, 13.0f);
		}
	}

	private void DrawPanelText(string text, float x, float y, float fontSize) {
		fontSize = Math.Max(13.0f, fontSize);
		Vector2 size = Raylib.MeasureTextEx(context.UiFont.Font, text, fontSize, UiFont.Spacing);
		float maxWidth = Math.Max(120.0f, Raylib.GetScreenWidth() - x - 10.0f);
		Raylib.DrawRectangleRec(new Rectangle(x - 8.0f, y - 6.0f, Math.Min(size.X + 16.0f, maxWidth), 26.0f), new Color(16, 18, 22, 214));
		DrawText(text, x, y, fontSize, new Color(219, 226, 236, 255));
	}

	private bool VectorInputs(string label, float x, ref float y, ref Vector3 vector) {
		bool changed = false;
		DrawText(label, x, y, 13.0f, AppTheme.TextPrimary);
		changed |= NumericInput($"{label} X", x + 78.0f, y - 4.0f, ref vector.X);
		changed |= NumericInput($"{label} Y", x + 182.0f, y - 4.0f, ref vector.Y);
		changed |= NumericInput($"{label} Z", x + 286.0f, y - 4.0f, ref vector.Z);
		y += 30.0f;
		return changed;
	}

	private bool NumericInput(string label, float x, float y, ref float value) {
		int inputId = nextTextBoxId++;
		Rectangle rect = new(x, y, 94.0f, 24.0f);
		Vector2 mouse = Raylib.GetMousePosition();
		bool hot = Raylib.CheckCollisionPointRec(mouse, rect);
		bool active = activeTextBoxId == inputId;
		bool changed = false;

		if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
			if (hot) {
				activeTextBoxId = inputId;
				activeTextBoxValue = Format(value);
				active = true;
			} else if (active) {
				changed |= CommitActiveTextBox(ref value);
				activeTextBoxId = 0;
				active = false;
			}
		}

		if (active) {
			ReadTextBoxInput();
			if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && activeTextBoxValue.Length > 0) {
				activeTextBoxValue = activeTextBoxValue[..^1];
			}

			if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter)) {
				changed |= CommitActiveTextBox(ref value);
				activeTextBoxId = 0;
				active = false;
			} else if (TryParseFloat(activeTextBoxValue, out float parsed) && Math.Abs(parsed - value) > 0.0001f) {
				value = parsed;
				changed = true;
			}
		}

		string text = active ? activeTextBoxValue : Format(value);
		Raylib.DrawRectangleRec(rect, active ? new Color(29, 39, 49, 255) : hot ? new Color(39, 48, 59, 255) : new Color(25, 30, 38, 255));
		Raylib.DrawRectangleLinesEx(rect, 1.0f, active ? new Color(143, 215, 255, 255) : new Color(58, 67, 81, 255));
		DrawText(text, rect.X + 6.0f, rect.Y + 6.0f, 13.0f, new Color(241, 244, 248, 255));
		if (active) {
			Vector2 textSize = Raylib.MeasureTextEx(context.UiFont.Font, text, 13.0f, UiFont.Spacing);
			float caretX = Math.Min(rect.X + rect.Width - 6.0f, rect.X + 6.0f + textSize.X + 1.0f);
			Raylib.DrawLine((int)caretX, (int)(rect.Y + 5.0f), (int)caretX, (int)(rect.Y + rect.Height - 5.0f), new Color(143, 215, 255, 255));
		}

		return changed;
	}

	private bool Slider(string label, float x, ref float y, ref float value, float min, float max) {
		int sliderId = nextSliderId++;
		Rectangle bar = new(x + 112.0f, y + 8.0f, 160.0f, 7.0f);
		Rectangle hitBox = new(bar.X, bar.Y - 8.0f, bar.Width, 23.0f);
		float oldValue = value;
		DrawText(label, x, y, 13.0f, AppTheme.TextPrimary);
		DrawText(Format(value), x + 278.0f, y, 13.0f, AppTheme.TextMuted);
		Raylib.DrawRectangleRec(bar, new Color(48, 57, 70, 255));
		float t = max > min ? (value - min) / (max - min) : 0.0f;
		t = Math.Clamp(t, 0.0f, 1.0f);
		Rectangle fill = bar;
		fill.Width *= t;
		Raylib.DrawRectangleRec(fill, new Color(93, 183, 255, 255));

		Vector2 mouse = Raylib.GetMousePosition();
		bool hot = Raylib.CheckCollisionPointRec(mouse, hitBox);
		if (Raylib.IsMouseButtonPressed(MouseButton.Left) && hot) {
			activeSliderId = sliderId;
		}

		if (Raylib.IsMouseButtonReleased(MouseButton.Left) && activeSliderId == sliderId) {
			activeSliderId = 0;
		}

		if (Raylib.IsMouseButtonDown(MouseButton.Left) && activeSliderId == sliderId) {
			value = min + Math.Clamp((mouse.X - bar.X) / bar.Width, 0.0f, 1.0f) * (max - min);
		}

		y += 25.0f;
		return Math.Abs(value - oldValue) > 0.0001f;
	}

	private bool Button(Rectangle rect, string text, bool enabled) {
		Vector2 mouse = Raylib.GetMousePosition();
		bool hot = enabled && Raylib.CheckCollisionPointRec(mouse, rect);
		Color fill = !enabled
			? new Color(31, 36, 45, 180)
			: hot
				? new Color(48, 61, 75, 255)
				: new Color(34, 41, 52, 255);
		Raylib.DrawRectangleRec(rect, fill);
		Raylib.DrawRectangleLinesEx(rect, 1.0f, new Color(58, 67, 81, enabled ? 255 : 160));
		Vector2 textSize = Raylib.MeasureTextEx(context.UiFont.Font, text, 13.0f, UiFont.Spacing);
		DrawText(text, rect.X + (rect.Width - textSize.X) * 0.5f, rect.Y + 6.0f, 13.0f, enabled ? AppTheme.TextPrimary : AppTheme.TextMuted);
		return hot && Raylib.IsMouseButtonPressed(MouseButton.Left);
	}

	private bool Toggle(Rectangle rect, string text, ref bool value) {
		bool oldValue = value;
		if (Button(rect, $"{(value ? "[x]" : "[ ]")} {text}", true)) {
			value = !value;
		}

		return value != oldValue;
	}

	private void ReadTextBoxInput() {
		int codepoint = Raylib.GetCharPressed();
		while (codepoint > 0) {
			char character = (char)codepoint;
			if (IsNumericInputCharacter(character) && activeTextBoxValue.Length < 16) {
				activeTextBoxValue += character == ',' ? '.' : character;
			}

			codepoint = Raylib.GetCharPressed();
		}
	}

	private bool CommitActiveTextBox(ref float value) {
		if (!TryParseFloat(activeTextBoxValue, out float parsed) || Math.Abs(parsed - value) <= 0.0001f) {
			return false;
		}

		value = parsed;
		return true;
	}

	private void DrawText(string text, float x, float y, float fontSize, Color color) {
		fontSize = Math.Max(13.0f, fontSize);
		Raylib.DrawTextEx(context.UiFont.Font, text, new Vector2(x, y), fontSize, UiFont.Spacing, color);
	}

	private static Rectangle Viewport => new(
		SampleTreeWidth,
		HeaderHeight,
		Math.Max(1.0f, Raylib.GetScreenWidth() - SampleTreeWidth - TransformPanelWidth),
		Math.Max(1.0f, Raylib.GetScreenHeight() - HeaderHeight));

	private static Rectangle TransformPanel => new(
		Math.Max(SampleTreeWidth, Raylib.GetScreenWidth() - TransformPanelWidth),
		HeaderHeight,
		TransformPanelWidth,
		Math.Max(1.0f, Raylib.GetScreenHeight() - HeaderHeight));

	private static Rectangle SampleTreePanel => new(
		0.0f,
		HeaderHeight,
		SampleTreeWidth,
		Math.Max(1.0f, Raylib.GetScreenHeight() - HeaderHeight - TreeViewPadding));

	private static float TreeViewPadding => 110.0f;

	private static Rectangle SampleTreeClip {
		get {
			Rectangle panel = SampleTreePanel;
			return new Rectangle(
				panel.X + 8.0f,
				panel.Y + 70.0f,
				panel.Width - 16.0f,
				Math.Max(1.0f, panel.Height - 78.0f));
		}
	}

	private float SampleTreeContentHeight => sampleCatalog.TreeRows.Count * SampleTreeRowHeight + 8.0f;

	private MeshViewerModel? PrimaryModel => sceneModels.Count == 0 ? null : sceneModels[0];

	private bool IsTextEditing => activeTextBoxId != 0;

	private int TotalUploadedTextureCount => sceneModels.Sum(model => model.UploadedTextureCount);

	private int TotalParticleSystemCount => sceneModels.Sum(model => model.ParticleSystemCount);

	private int TotalActiveParticleCount => sceneModels.Sum(model => model.ActiveParticleCount);

	private int TotalHardpointCount => sceneModels.Sum(model => model.Geometry.Hardpoints.Count);

	private BoundingBox CombinedSceneBounds {
		get {
			if (sceneModels.Count == 0) {
				return new BoundingBox(Vector3.Zero, Vector3.One);
			}

			Vector3 min = sceneModels[0].Bounds.Min;
			Vector3 max = sceneModels[0].Bounds.Max;
			for (int index = 1; index < sceneModels.Count; index++) {
				min = Vector3.Min(min, sceneModels[index].Bounds.Min);
				max = Vector3.Max(max, sceneModels[index].Bounds.Max);
			}

			return new BoundingBox(min, max);
		}
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

	private static bool IsNumericInputCharacter(char character) {
		return char.IsDigit(character)
			|| character is '-' or '+' or '.' or ',' or 'e' or 'E';
	}

	private static bool TryParseFloat(string text, out float value) {
		return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}

	private static BlendMode SelectParticleBlendMode(ParticleParameters parameters) {
		return parameters.SrcBlend == ParticleConstants.BlendSrcAlpha
		       && parameters.DstBlend == ParticleConstants.BlendInvSrcAlpha
			? BlendMode.Alpha
			: BlendMode.Additive;
	}

	private static int ToByte(float value) {
		return (int)(Math.Clamp(value, 0.0f, 1.0f) * 255.0f);
	}

	private static Color FadeColor(Color color, float alpha) {
		return new Color(color.R, color.G, color.B, ToByte(alpha));
	}

	private static Vector3 NormalizeOrDefault(Vector3 value, Vector3 fallback) {
		return value.LengthSquared() > 0.000001f ? Vector3.Normalize(value) : fallback;
	}

	private static string Format(float value) {
		return value.ToString(MathF.Round(value) == value ? "0" : "0.###", CultureInfo.InvariantCulture);
	}

	private static string FormatVector(Vector3 value) {
		return $"({Format(value.X)},{Format(value.Y)},{Format(value.Z)})";
	}

	private static void DrawCenteredGrid() {
		const int slices = 20;
		const float spacing = 2.0f;
		const float y = 0.0f;
		float extent = slices * spacing * 0.5f;

		Color minor = new(48, 57, 70, 210);
		Color major = new(93, 109, 132, 235);

		for (int index = 0; index <= slices; index++) {
			float coordinate = -extent + index * spacing;
			bool centerLine = Math.Abs(coordinate) < 0.001f;
			Color color = centerLine ? major : minor;
			Raylib.DrawLine3D(new Vector3(-extent, y, coordinate), new Vector3(extent, y, coordinate), color);
			Raylib.DrawLine3D(new Vector3(coordinate, y, -extent), new Vector3(coordinate, y, extent), color);
		}
	}
}
