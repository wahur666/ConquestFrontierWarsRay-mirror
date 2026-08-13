using DACOM;

namespace ConquestFrontierWarsRay.Runtime.Physics;

public sealed class PhysicsDesc : AggDesc {
	public PhysicsDesc(string interfaceName = PhysicsIdentifiers.InterfaceName, string? description = null)
		: base(interfaceName, description) {
	}

	public string SolverImplementation { get; init; } = PhysicsIdentifiers.TrapezoidalImplementation;
}

public sealed class OdeSolverDesc : DacomDesc {
	public OdeSolverDesc(string? implementationName = null, string interfaceName = PhysicsIdentifiers.OdeInterfaceName)
		: base(interfaceName) {
		ImplementationName = implementationName;
	}

	public string? ImplementationName { get; init; }
}
