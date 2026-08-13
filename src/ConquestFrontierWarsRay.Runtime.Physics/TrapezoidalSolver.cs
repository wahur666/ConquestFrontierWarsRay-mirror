using DACOM;

namespace ConquestFrontierWarsRay.Runtime.Physics;

internal sealed class TrapezoidalSolver : DacomComponent, IOrdinaryDifferentialEquationSolver {
	private float[] _stateGuess = [];
	private float[] _previousState = [];
	private float[] _forwardDerivative = [];

	public TrapezoidalSolver() {
		RegisterInterface(PhysicsIdentifiers.OdeInterfaceName, this);
	}

	public void Solve(IOrdinaryDifferentialEquation solvable, float timeStep) {
		ArgumentNullException.ThrowIfNull(solvable);

		var time = solvable.GetTime();
		var length = solvable.GetStateLength();
		if (length > 0) {
			EnsureCapacity(length);

			var previousState = _previousState.AsSpan(0, length);
			var forwardDerivative = _forwardDerivative.AsSpan(0, length);
			var stateGuess = _stateGuess.AsSpan(0, length);

			solvable.GetState(previousState, time);
			solvable.GetDerivative(forwardDerivative, previousState, time);
			previousState.CopyTo(stateGuess);

			for (var index = 0; index < length; index++) {
				stateGuess[index] = previousState[index] + (timeStep * forwardDerivative[index]);
			}

			solvable.SetState(stateGuess);
		}

		solvable.SetTime(time + timeStep);
	}

	private void EnsureCapacity(int length) {
		if (_stateGuess.Length >= length) {
			return;
		}

		var capacity = (int)MathF.Floor((length * 1.5f) + 0.5f);
		_stateGuess = new float[capacity];
		_previousState = new float[capacity];
		_forwardDerivative = new float[capacity];
	}
}
