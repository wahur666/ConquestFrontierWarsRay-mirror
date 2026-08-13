using System.Numerics;
using RaySharp.Mesh;

namespace RaySharp.MeshViewer;

internal sealed class MeshViewerAnimationController {
	private const float AnimatedTranslationScale = 0.01f;
	private readonly Dictionary<string, Matrix4x4> partTransforms;
	private readonly Dictionary<string, Matrix4x4> bindTransforms;

	public MeshViewerAnimationController(IReadOnlyList<MeshAnimationClip> clips, Dictionary<string, Matrix4x4> partTransforms) {
		Clips = clips;
		this.partTransforms = partTransforms;
		bindTransforms = partTransforms.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
		ActiveClipIndex = SelectDefaultClip(clips);
		Apply();
	}

	public IReadOnlyList<MeshAnimationClip> Clips { get; }
	public int ActiveClipIndex { get; private set; }
	public MeshAnimationClip? ActiveClip => Clips.Count == 0 ? null : Clips[ActiveClipIndex];
	public int ClipCount => Clips.Count;
	public float Time { get; private set; }
	public int Direction { get; private set; } = 1;
	public bool Playing { get; private set; }
	public bool Loop { get; set; } = true;
	public bool PingPong { get; set; }

	public void Play() {
		Playing = true;
	}

	public void Stop() {
		Playing = false;
	}

	public void Restart() {
		Time = 0.0f;
		Direction = 1;
		Playing = true;
		Apply();
	}

	public void Reset() {
		Time = 0.0f;
		Direction = 1;
		Playing = false;
		ResetBindTransforms();
		Apply();
	}

	public void SelectClip(int index) {
		if (Clips.Count == 0) {
			return;
		}

		int next = Math.Clamp(index, 0, Clips.Count - 1);
		if (next == ActiveClipIndex) {
			return;
		}

		ActiveClipIndex = next;
		Time = 0.0f;
		Direction = 1;
		ResetBindTransforms();
		Apply();
	}

	public void Update(float deltaTime) {
		MeshAnimationClip? clip = ActiveClip;
		if (!Playing || clip is null) {
			return;
		}

		Time += deltaTime * Direction;
		ConstrainTime(clip.Duration);
		Apply();
	}

	private void ConstrainTime(float duration) {
		if (duration <= 0.0f) {
			Time = 0.0f;
			Playing = false;
			return;
		}

		if (PingPong) {
			while (Time > duration || Time < 0.0f) {
				if (Time > duration) {
					Time = duration - (Time - duration);
					Direction = -1;
				} else {
					Time = -Time;
					Direction = 1;
					if (!Loop) {
						Time = 0.0f;
						Playing = false;
						break;
					}
				}
			}

			return;
		}

		if (Loop) {
			Time %= duration;
			if (Time < 0.0f) {
				Time += duration;
			}
		} else if (Time >= duration) {
			Time = duration;
			Playing = false;
		}
	}

	private void Apply() {
		MeshAnimationClip? clip = ActiveClip;
		if (clip is null) {
			return;
		}

		foreach (MeshAnimationTrack track in clip.Tracks) {
			if (!partTransforms.ContainsKey(track.TargetName)) {
				continue;
			}

			if (track.TargetType == MeshAnimationTargetType.Object && track.Channel.Kind == MeshAnimationChannelKind.FullTransform) {
				MeshTransformKeyframe sample = SampleTransform(track.Channel.TransformFrames, Time);
				ApplyObjectFullTransform(track.TargetName, sample);
			} else if (track.Channel.Kind == MeshAnimationChannelKind.FullTransform && track.Joint is MeshCompoundJoint fullJoint) {
				MeshTransformKeyframe sample = SampleTransform(track.Channel.TransformFrames, Time);
				ApplyFullJointTransform(track.TargetName, fullJoint, sample);
			} else if (track.Channel.Kind == MeshAnimationChannelKind.Float && track.Joint is MeshCompoundJoint floatJoint) {
				MeshFloatKeyframe sample = SampleFloat(track.Channel.FloatFrames, Time, floatJoint);
				ApplyFloatJointTransform(track.TargetName, floatJoint, sample.Value);
			} else if (track.Channel.Kind == MeshAnimationChannelKind.Quaternion && track.Joint is MeshCompoundJoint quatJoint) {
				MeshQuaternionKeyframe sample = SampleQuaternion(track.Channel.QuaternionFrames, Time);
				ApplyQuaternionJointTransform(track.TargetName, quatJoint, sample.Rotation);
			} else if (track.Channel.Kind == MeshAnimationChannelKind.Vector && track.Joint is MeshCompoundJoint vectorJoint) {
				MeshVectorKeyframe sample = SampleVector(track.Channel.VectorFrames, Time);
				ApplyVectorJointTransform(track.TargetName, vectorJoint, sample.Position);
			}
		}
	}

	private void ApplyObjectFullTransform(string targetName, MeshTransformKeyframe sample) {
		Matrix4x4 bind = bindTransforms.GetValueOrDefault(targetName, Matrix4x4.Identity);
		Matrix4x4.Decompose(bind, out Vector3 scale, out Quaternion rotation, out Vector3 translation);
		Vector3 animatedPosition = translation + Vector3.Transform(sample.Position, rotation);
		Quaternion animatedRotation = Quaternion.Normalize(rotation * sample.Rotation);
		partTransforms[targetName] = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(animatedRotation) * Matrix4x4.CreateTranslation(animatedPosition);
	}

	private void ApplyFullJointTransform(string targetName, MeshCompoundJoint joint, MeshTransformKeyframe sample) {
		Matrix4x4 relOrientation = MatrixFromRotation(joint.RelativeOrientation);
		Matrix4x4 rotation = Matrix4x4.CreateFromQuaternion(sample.Rotation) * relOrientation;
		if (joint.Type is MeshCompoundJointType.Revolute or MeshCompoundJointType.Prismatic or MeshCompoundJointType.Spherical) {
			Vector3 childPoint = Vector3.Transform(joint.ChildPoint, rotation);
			partTransforms[targetName] = MatrixWithTranslation(rotation, joint.ParentPoint + sample.Position - childPoint);
		} else {
			partTransforms[targetName] = MatrixWithTranslation(rotation, joint.ParentPoint + sample.Position);
		}
	}

	private void ApplyQuaternionJointTransform(string targetName, MeshCompoundJoint joint, Quaternion rotationValue) {
		if (joint.Type != MeshCompoundJointType.Spherical) {
			return;
		}

		Matrix4x4 rotation = Matrix4x4.CreateFromQuaternion(rotationValue) * MatrixFromRotation(joint.RelativeOrientation);
		Vector3 childPoint = Vector3.Transform(joint.ChildPoint, rotation);
		partTransforms[targetName] = MatrixWithTranslation(rotation, joint.ParentPoint - childPoint);
	}

	private void ApplyVectorJointTransform(string targetName, MeshCompoundJoint joint, Vector3 position) {
		if (joint.Type != MeshCompoundJointType.Translational) {
			return;
		}

		partTransforms[targetName] = MatrixFromRotationTranslation(joint.RelativeOrientation, joint.ParentPoint + position);
	}

	private void ApplyFloatJointTransform(string targetName, MeshCompoundJoint joint, float value) {
		if (joint.Type == MeshCompoundJointType.Revolute) {
			Vector3 axis = NormalizeOrDefault(joint.Axis, Vector3.UnitY);
			Matrix4x4 rotation = Matrix4x4.CreateFromAxisAngle(axis, value) * MatrixFromRotation(joint.RelativeOrientation);
			Vector3 childPoint = Vector3.Transform(joint.ChildPoint, rotation);
			partTransforms[targetName] = MatrixWithTranslation(rotation, joint.ParentPoint - childPoint);
		} else if (joint.Type == MeshCompoundJointType.Prismatic) {
			Vector3 axis = joint.Axis.LengthSquared() > 0.000001f ? Vector3.Normalize(joint.Axis) * value * AnimatedTranslationScale : Vector3.Zero;
			Matrix4x4 relOrientation = MatrixFromRotation(joint.RelativeOrientation);
			Vector3 childPoint = Vector3.Transform(joint.ChildPoint, relOrientation);
			partTransforms[targetName] = MatrixWithTranslation(relOrientation, joint.ParentPoint + axis - childPoint);
		}
	}

	private void ResetBindTransforms() {
		foreach ((string name, Matrix4x4 transform) in bindTransforms) {
			partTransforms[name] = transform;
		}
	}

	private static int SelectDefaultClip(IReadOnlyList<MeshAnimationClip> clips) {
		if (clips.Count == 0) {
			return 0;
		}

		int best = 0;
		for (int index = 1; index < clips.Count; index++) {
			if (clips[index].Duration > clips[best].Duration) {
				best = index;
			}
		}

		return best;
	}

	private static MeshFloatKeyframe SampleFloat(IReadOnlyList<MeshFloatKeyframe> frames, float time, MeshCompoundJoint joint) {
		(MeshFloatKeyframe a, MeshFloatKeyframe b, float t) = Bracket(frames, time);
		float value = joint.Type == MeshCompoundJointType.Revolute
			? InterpolateArc(a.Value, b.Value, t)
			: Lerp(a.Value, b.Value, t);
		return new MeshFloatKeyframe(time, value);
	}

	private static MeshVectorKeyframe SampleVector(IReadOnlyList<MeshVectorKeyframe> frames, float time) {
		(MeshVectorKeyframe a, MeshVectorKeyframe b, float t) = Bracket(frames, time);
		return new MeshVectorKeyframe(time, Vector3.Lerp(a.Position, b.Position, t));
	}

	private static MeshQuaternionKeyframe SampleQuaternion(IReadOnlyList<MeshQuaternionKeyframe> frames, float time) {
		(MeshQuaternionKeyframe a, MeshQuaternionKeyframe b, float t) = Bracket(frames, time);
		return new MeshQuaternionKeyframe(time, Quaternion.Slerp(a.Rotation, b.Rotation, t));
	}

	private static MeshTransformKeyframe SampleTransform(IReadOnlyList<MeshTransformKeyframe> frames, float time) {
		(MeshTransformKeyframe a, MeshTransformKeyframe b, float t) = Bracket(frames, time);
		return new MeshTransformKeyframe(time, Vector3.Lerp(a.Position, b.Position, t), Quaternion.Slerp(a.Rotation, b.Rotation, t));
	}

	private static (T A, T B, float T) Bracket<T>(IReadOnlyList<T> frames, float time) where T : notnull {
		if (frames.Count == 1 || time <= KeyTime(frames[0])) {
			return (frames[0], frames[0], 0.0f);
		}

		T last = frames[^1];
		if (time >= KeyTime(last)) {
			return (last, last, 0.0f);
		}

		int nextIndex = 1;
		while (nextIndex < frames.Count && KeyTime(frames[nextIndex]) < time) {
			nextIndex++;
		}

		T previous = frames[nextIndex - 1];
		T next = frames[nextIndex];
		float span = Math.Max(KeyTime(next) - KeyTime(previous), 0.000001f);
		return (previous, next, (time - KeyTime(previous)) / span);
	}

	private static float KeyTime<T>(T frame) {
		return frame switch {
			MeshFloatKeyframe value => value.Time,
			MeshVectorKeyframe value => value.Time,
			MeshQuaternionKeyframe value => value.Time,
			MeshTransformKeyframe value => value.Time,
			_ => 0.0f
		};
	}

	private static float InterpolateArc(float a, float b, float t) {
		float adjusted = b;
		if (adjusted - a < -MathF.PI) {
			adjusted += MathF.PI * 2.0f;
		} else if (adjusted - a > MathF.PI) {
			adjusted -= MathF.PI * 2.0f;
		}

		return Lerp(a, adjusted, t);
	}

	private static float Lerp(float a, float b, float t) {
		return a + (b - a) * t;
	}

	private static Vector3 NormalizeOrDefault(Vector3 value, Vector3 fallback) {
		return value.LengthSquared() > 0.000001f ? Vector3.Normalize(value) : fallback;
	}

	private static Matrix4x4 MatrixFromRotation(Matrix3x3 rotation) {
		return new Matrix4x4(
			rotation.E00, rotation.E10, rotation.E20, 0.0f,
			rotation.E01, rotation.E11, rotation.E21, 0.0f,
			rotation.E02, rotation.E12, rotation.E22, 0.0f,
			0.0f, 0.0f, 0.0f, 1.0f);
	}

	private static Matrix4x4 MatrixFromRotationTranslation(Matrix3x3 rotation, Vector3 translation) {
		return MatrixWithTranslation(MatrixFromRotation(rotation), translation);
	}

	private static Matrix4x4 MatrixWithTranslation(Matrix4x4 matrix, Vector3 translation) {
		matrix.M41 = translation.X;
		matrix.M42 = translation.Y;
		matrix.M43 = translation.Z;
		matrix.M44 = 1.0f;
		return matrix;
	}
}
