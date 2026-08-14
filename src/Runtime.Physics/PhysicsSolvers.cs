namespace ConquestFrontierWarsRay.Runtime.Physics;

public static class PhysicsSolvers {
	public static void Solve(PhysicsSolverKind kind, IOrdinaryDifferentialEquation solvable, float timeStep) {
		ArgumentNullException.ThrowIfNull(solvable);
		switch (kind) {
			case PhysicsSolverKind.Euler:
				SolveEuler(solvable, timeStep);
				break;
			case PhysicsSolverKind.Rk4:
				SolveRk4(solvable, timeStep);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown solver kind.");
		}
	}

	private static void SolveEuler(IOrdinaryDifferentialEquation solvable, float timeStep) {
		var time = solvable.GetTime();
		var length = solvable.GetStateLength();
		if (length > 0) {
			var state = new float[length];
			var derivative = new float[length];

			solvable.GetState(state.AsSpan(), time);
			solvable.GetDerivative(derivative.AsSpan(), state.AsSpan(), time);

			for (var index = 0; index < length; index++) {
				state[index] += derivative[index] * timeStep;
			}

			solvable.SetState(state.AsSpan());
		}

		solvable.SetTime(time + timeStep);
	}

	private static void SolveRk4(IOrdinaryDifferentialEquation solvable, float timeStep) {
		var time = solvable.GetTime();
		var length = solvable.GetStateLength();
		if (length > 0) {
			var state = new float[length];
			var k1 = new float[length];
			var k2 = new float[length];
			var k3 = new float[length];
			var k4 = new float[length];
			var work = new float[length];
			var halfTimeStep = timeStep / 2f;

			solvable.GetState(state.AsSpan(), time);
			solvable.GetDerivative(k1.AsSpan(), state.AsSpan(), time);
			ScaleInPlace(k1.AsSpan(), timeStep);

			for (var index = 0; index < length; index++) {
				work[index] = state[index] + (0.5f * k1[index]);
			}

			solvable.GetDerivative(k2.AsSpan(), work.AsSpan(), time + halfTimeStep);
			ScaleInPlace(k2.AsSpan(), timeStep);

			for (var index = 0; index < length; index++) {
				work[index] = state[index] + (0.5f * k2[index]);
			}

			solvable.GetDerivative(k3.AsSpan(), work.AsSpan(), time + halfTimeStep);
			ScaleInPlace(k3.AsSpan(), timeStep);

			for (var index = 0; index < length; index++) {
				work[index] = state[index] + k3[index];
			}

			solvable.GetDerivative(k4.AsSpan(), work.AsSpan(), time + timeStep);
			ScaleInPlace(k4.AsSpan(), timeStep);

			const float oneSixth = 1f / 6f;
			for (var index = 0; index < length; index++) {
				work[index] = state[index] + (oneSixth * (k1[index] + (2f * k2[index]) + (2f * k3[index]) + k4[index]));
			}

			solvable.SetState(work.AsSpan());
		}

		solvable.SetTime(time + timeStep);
	}

	private static void ScaleInPlace(Span<float> values, float scale) {
		for (var index = 0; index < values.Length; index++) {
			values[index] *= scale;
		}
	}
}
