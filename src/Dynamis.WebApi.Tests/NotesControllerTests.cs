using System.Net;
using DynamisReferenceApp.Api.Core.Data;
using DynamisReferenceApp.Api.Tools.Notes.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace DynamisReferenceApp.WebApi.Tests;

public class NotesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NotesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });
        });
    }

    [Fact]
    public async Task GetNotes_ReturnsOkAndNotes()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/notes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<IEnumerable<NoteDto>>();
        notes.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNote_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/notes/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateNote_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateNoteRequest("Test Note", "This is a test note.");

        // Act
        var response = await client.PostAsJsonAsync("/api/notes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var note = await response.Content.ReadFromJsonAsync<NoteDto>();
        note.Should().NotBeNull();
        note!.Title.Should().Be(request.Title);
    }

    [Fact]
    public async Task DeleteNote_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create a note first
        var createRequest = new CreateNoteRequest("To Delete", "...");
        var createResponse = await client.PostAsJsonAsync("/api/notes", createRequest);
        var createdNote = await createResponse.Content.ReadFromJsonAsync<NoteDto>();

        // Act
        var response = await client.DeleteAsync($"/api/notes/{createdNote!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResponse = await client.GetAsync($"/api/notes/{createdNote.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
