using DACOM;

namespace ConquestFrontierWarsRay.Runtime.Physics;

internal sealed class EulerSolver : DacomComponent, IOrdinaryDifferentialEquationSolver {
	private float[] _state = [];
	private float[] _derivative = [];

	public EulerSolver() {
		RegisterInterface(PhysicsIdentifiers.OdeInterfaceName, this);
	}

	public void Solve(IOrdinaryDifferentialEquation solvable, float timeStep) {
		ArgumentNullException.ThrowIfNull(solvable);

		var time = solvable.GetTime();
		var length = solvable.GetStateLength();
		if (length > 0) {
			EnsureCapacity(length);

			var state = _state.AsSpan(0, length);
			var derivative = _derivative.AsSpan(0, length);

			solvable.GetState(state, time);
			solvable.GetDerivative(derivative, state, time);

			for (var index = 0; index < length; index++) {
				state[index] += derivative[index] * timeStep;
			}

			solvable.SetState(state);
		}

		solvable.SetTime(time + timeStep);
	}

	private void EnsureCapacity(int length) {
		if (_state.Length >= length) {
			return;
		}

		var capacity = (int)MathF.Floor((length * 1.5f) + 0.5f);
		_state = new float[capacity];
		_derivative = new float[capacity];
	}
}
