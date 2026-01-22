// AI: purpose=xUnit collection definition for tests that share global RNG state; ensures sequential execution.
// AI: invariants=All tests in this collection run sequentially, not in parallel with each other.
// AI: deps=xUnit test framework.
// AI: change=Created to fix flaky tests caused by parallel RNG.Initialize() calls.

using Xunit;

namespace Music.Tests
{
    /// <summary>
    /// Collection definition for tests that depend on global RNG state.
    /// Tests in this collection run sequentially to avoid race conditions
    /// when multiple tests call Rng.Initialize() with different seeds.
    /// </summary>
    [CollectionDefinition("RngDependentTests", DisableParallelization = true)]
    public class RngDependentTestsCollection : ICollectionFixture<RngTestFixture>
    {
    }

    /// <summary>
    /// Shared fixture for RNG-dependent tests. Currently empty but can be
    /// extended to provide shared setup/teardown for RNG state.
    /// </summary>
    public class RngTestFixture
    {
        // No shared state needed currently - the collection just ensures sequential execution
    }
}
