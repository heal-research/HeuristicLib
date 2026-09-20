using System.Runtime.CompilerServices;

// The name OperatorCompatibilityTests gives its in-memory compilations. Those compile against this assembly as
// metadata, so the fixture types they instantiate must be visible to them; the fixtures are internal to keep the
// runner from reflecting over open generic types it cannot load.
[assembly: InternalsVisibleTo("TestCompilation")]
