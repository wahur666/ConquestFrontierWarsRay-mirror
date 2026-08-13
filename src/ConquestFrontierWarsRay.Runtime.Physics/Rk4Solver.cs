using DACOM;

namespace ConquestFrontierWarsRay.Runtime.Physics;

internal sealed class Rk4Solver : DacomComponent, IOrdinaryDifferentialEquationSolver {
	private float[] _state = [];
	private float[] _k1 = [];
	private float[] _k2 = [];
	private float[] _k3 = [];
	private float[] _k4 = [];
	private float[] _work = [];

	public Rk4Solver() {
		RegisterInterface(PhysicsIdentifiers.OdeInterfaceName, this);
	}

	public void Solve(IOrdinaryDifferentialEquation solvable, float timeStep) {
		ArgumentNullException.ThrowIfNull(solvable);

		var time = solvable.GetTime();
		var length = solvable.GetStateLength();
		if (length > 0) {
			EnsureCapacity(length);

			var state = _state.AsSpan(0, length);
			var k1 = _k1.AsSpan(0, length);
			var k2 = _k2.AsSpan(0, length);
			var k3 = _k3.AsSpan(0, length);
			var k4 = _k4.AsSpan(0, length);
			var work = _work.AsSpan(0, length);
			var halfTimeStep = timeStep / 2f;

			solvable.GetState(state, time);
			solvable.GetDerivative(k1, state, time);
			ScaleInPlace(k1, timeStep);

			for (var index = 0; index < length; index++) {
				work[index] = state[index] + (0.5f * k1[index]);
			}

			solvable.GetDerivative(k2, work, time + halfTimeStep);
			ScaleInPlace(k2, timeStep);

			for (var index = 0; index < length; index++) {
				work[index] = state[index] + (0.5f * k2[index]);
			}

			solvable.GetDerivative(k3, work, time + halfTimeStep);
			ScaleInPlace(k3, timeStep);

			for (var index = 0; index < length; index++) {
				work[index] = state[index] + k3[index];
			}

			solvable.GetDerivative(k4, work, time + timeStep);
			ScaleInPlace(k4, timeStep);

			const float oneSixth = 1f / 6f;
			for (var index = 0; index < length; index++) {
				work[index] = state[index] + (oneSixth * (k1[index] + (2f * k2[index]) + (2f * k3[index]) + k4[index]));
			}

			solvable.SetState(work);
		}

		solvable.SetTime(time + timeStep);
	}

	private void EnsureCapacity(int length) {
		if (_state.Length >= length) {
			return;
		}

		var capacity = (int)MathF.Floor((length * 1.5f) + 0.5f);
		_state = new float[capacity];
		_k1 = new float[capacity];
		_k2 = new float[capacity];
		_k3 = new float[capacity];
		_k4 = new float[capacity];
		_work = new float[capacity];
	}

	private static void ScaleInPlace(Span<float> values, float scale) {
		for (var index = 0; index < values.Length; index++) {
			values[index] *= scale;
		}
	}
}
