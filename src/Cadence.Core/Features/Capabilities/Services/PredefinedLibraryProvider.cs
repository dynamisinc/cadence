using System.Text.Json;
using Cadence.Core.Features.Capabilities.Models.DTOs;

namespace Cadence.Core.Features.Capabilities.Services;

/// <summary>
/// Provides access to predefined capability libraries loaded from embedded JSON resource.
/// </summary>
public class PredefinedLibraryProvider : IPredefinedLibraryProvider
{
    private readonly List<PredefinedLibrary> _libraries;

    public PredefinedLibraryProvider()
    {
        _libraries = LoadLibrariesFromJson();
    }

    /// <inheritdoc />
    public IEnumerable<PredefinedLibraryInfo> GetAvailableLibraries()
    {
        return _libraries.Select(lib => new PredefinedLibraryInfo(
            lib.Id,
            lib.Name,
            lib.Description,
            lib.Capabilities.Count
        ));
    }

    /// <inheritdoc />
    public PredefinedLibrary? GetLibrary(string libraryId)
    {
        return _libraries.FirstOrDefault(lib =>
            string.Equals(lib.Id, libraryId, StringComparison.OrdinalIgnoreCase));
    }

    private static List<PredefinedLibrary> LoadLibrariesFromJson()
    {
        var assembly = typeof(PredefinedLibraryProvider).Assembly;
        var resourceName = "Cadence.Core.Features.Capabilities.Data.PredefinedCapabilityLibraries.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Could not find embedded resource: {resourceName}. " +
                "Ensure the JSON file is marked as an Embedded Resource in the .csproj file.");
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var document = JsonDocument.Parse(json);
        var librariesArray = document.RootElement.GetProperty("libraries");

        var libraries = new List<PredefinedLibrary>();

        foreach (var libElement in librariesArray.EnumerateArray())
        {
            var id = libElement.GetProperty("id").GetString()!;
            var name = libElement.GetProperty("name").GetString()!;
            var description = libElement.GetProperty("description").GetString()!;

            var capabilities = new List<PredefinedCapability>();
            var capsArray = libElement.GetProperty("capabilities");

            foreach (var capElement in capsArray.EnumerateArray())
            {
                var capName = capElement.GetProperty("name").GetString()!;
                var capDesc = capElement.TryGetProperty("description", out var descProp)
                    ? descProp.GetString()
                    : null;
                var capCategory = capElement.TryGetProperty("category", out var catProp)
                    ? catProp.GetString()
                    : null;

                capabilities.Add(new PredefinedCapability(capName, capDesc, capCategory));
            }

            libraries.Add(new PredefinedLibrary(id, name, description, capabilities));
        }

        return libraries;
    }
}
