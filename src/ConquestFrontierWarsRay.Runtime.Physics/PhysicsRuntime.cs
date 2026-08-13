using DACOM;

namespace ConquestFrontierWarsRay.Runtime.Physics;

internal sealed class PhysicsFactory : IDacomFactory {
	public string InterfaceName => PhysicsIdentifiers.InterfaceName;

	public object CreateInstance(DacomDesc descriptor, IDacomRegistry registry) {
		ArgumentNullException.ThrowIfNull(registry);

		return descriptor switch {
			PhysicsDesc typedDescriptor => new PhysicsService(typedDescriptor, registry),
			AggDesc aggregateDescriptor => new PhysicsService(new PhysicsDesc(aggregateDescriptor.InterfaceName, aggregateDescriptor.Description), registry),
			_ => throw new InvalidOperationException("Unsupported physics descriptor.")
		};
	}
}

internal sealed class OdeSolverFactory : IDacomFactory {
	public string InterfaceName => PhysicsIdentifiers.OdeInterfaceName;

	public object CreateInstance(DacomDesc descriptor, IDacomRegistry registry) {
		_ = registry;

		var implementationName = descriptor switch {
			OdeSolverDesc typedDescriptor => typedDescriptor.ImplementationName,
			_ => null
		};

		return implementationName switch {
			null or "" => new TrapezoidalSolver(),
			PhysicsIdentifiers.EulerImplementation => new EulerSolver(),
			PhysicsIdentifiers.Rk4Implementation => new Rk4Solver(),
			PhysicsIdentifiers.TrapezoidalImplementation => new TrapezoidalSolver(),
			_ => throw new InvalidOperationException($"Unsupported ODE solver implementation '{implementationName}'.")
		};
	}
}

public static class PhysicsRuntime {
	public static void Register(IDacomRegistry registry) {
		ArgumentNullException.ThrowIfNull(registry);

		registry.RegisterComponent(new PhysicsFactory(), DacomPriority.Normal);
		registry.RegisterComponent(new OdeSolverFactory(), DacomPriority.Normal);
	}
}
