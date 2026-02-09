using Xunit;

namespace Cadence.WebApi.Tests;

/// <summary>
/// Defines a test collection for WebApi integration tests that use CadenceWebApplicationFactory.
/// Tests in this collection run sequentially (not in parallel) to prevent shared state interference
/// from rate limiters and in-memory databases across test classes.
/// </summary>
[CollectionDefinition("WebApi Integration")]
public class IntegrationTestCollection : ICollectionFixture<CadenceWebApplicationFactory>
{
}
