using Cadence.Core.Features.Email.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Email;

/// <summary>
/// Tests for PlaceholderEmailTemplateRenderer.
/// </summary>
public class PlaceholderEmailTemplateRendererTests
{
    private readonly Mock<ILogger<PlaceholderEmailTemplateRenderer>> _logger;
    private readonly InMemoryEmailTemplateStore _templateStore;
    private readonly PlaceholderEmailTemplateRenderer _renderer;

    public PlaceholderEmailTemplateRendererTests()
    {
        _logger = new Mock<ILogger<PlaceholderEmailTemplateRenderer>>();
        _templateStore = new InMemoryEmailTemplateStore();
        _renderer = new PlaceholderEmailTemplateRenderer(_logger.Object, _templateStore);
    }

    // =========================================================================
    // RenderAsync - Basic Tests
    // =========================================================================

    [Fact]
    public async Task RenderAsync_ValidTemplate_ProducesBothHtmlAndPlainText()
    {
        _templateStore.AddTemplate(new EmailTemplate(
            TemplateId: "Test",
            SubjectTemplate: "Hello {{Name}}",
            HtmlContent: "<p>Hi {{Name}}, welcome to {{OrgName}}</p>",
            PlainTextContent: "Hi {{Name}}, welcome to {{OrgName}}"
        ));

        var model = new TestModel { Name = "Alice", OrgName = "FEMA" };
        var result = await _renderer.RenderAsync("Test", model);

        Assert.Equal("Hello Alice", result.Subject);
        Assert.Equal("<p>Hi Alice, welcome to FEMA</p>", result.HtmlBody);
        Assert.Equal("Hi Alice, welcome to FEMA", result.PlainTextBody);
    }

    [Fact]
    public async Task RenderAsync_ReplacesAllPlaceholders()
    {
        _templateStore.AddTemplate(new EmailTemplate(
            TemplateId: "Multi",
            SubjectTemplate: "{{Greeting}} {{Name}}",
            HtmlContent: "<h1>{{Greeting}}</h1><p>{{Name}} from {{City}}</p>",
            PlainTextContent: "{{Greeting}} {{Name}} from {{City}}"
        ));

        var model = new MultiModel { Greeting = "Welcome", Name = "Bob", City = "Denver" };
        var result = await _renderer.RenderAsync("Multi", model);

        Assert.Equal("Welcome Bob", result.Subject);
        Assert.Contains("Welcome", result.HtmlBody);
        Assert.Contains("Bob", result.HtmlBody);
        Assert.Contains("Denver", result.HtmlBody);
    }

    [Fact]
    public async Task RenderAsync_NullPropertyValue_ReplacesWithEmptyString()
    {
        _templateStore.AddTemplate(new EmailTemplate(
            TemplateId: "Nullable",
            SubjectTemplate: "Hello {{Name}}",
            HtmlContent: "<p>{{Name}} - {{NullableField}}</p>",
            PlainTextContent: "{{Name}} - {{NullableField}}"
        ));

        var model = new NullableModel { Name = "Alice", NullableField = null };
        var result = await _renderer.RenderAsync("Nullable", model);

        Assert.Equal("<p>Alice - </p>", result.HtmlBody);
    }

    [Fact]
    public async Task RenderAsync_UnmatchedPlaceholders_RemovedFromOutput()
    {
        _templateStore.AddTemplate(new EmailTemplate(
            TemplateId: "Unmatched",
            SubjectTemplate: "Test",
            HtmlContent: "<p>{{Name}} and {{Unknown}}</p>",
            PlainTextContent: "{{Name}} and {{Unknown}}"
        ));

        var model = new TestModel { Name = "Alice", OrgName = "FEMA" };
        var result = await _renderer.RenderAsync("Unmatched", model);

        Assert.Equal("<p>Alice and </p>", result.HtmlBody);
        Assert.DoesNotContain("{{Unknown}}", result.HtmlBody);
    }

    // =========================================================================
    // RenderAsync - Date Formatting
    // =========================================================================

    [Fact]
    public async Task RenderAsync_DateTimeProperty_FormatsAsHumanReadable()
    {
        _templateStore.AddTemplate(new EmailTemplate(
            TemplateId: "Dated",
            SubjectTemplate: "Invitation",
            HtmlContent: "<p>Expires: {{ExpiresAt}}</p>",
            PlainTextContent: "Expires: {{ExpiresAt}}"
        ));

        var model = new DatedModel { ExpiresAt = new DateTime(2026, 3, 15) };
        var result = await _renderer.RenderAsync("Dated", model);

        Assert.Contains("March 15, 2026", result.HtmlBody);
    }

    // =========================================================================
    // RenderAsync - Layout Wrapping
    // =========================================================================

    [Fact]
    public async Task RenderAsync_WithLayout_WrapsContentInLayout()
    {
        _templateStore.AddTemplate(new EmailTemplate(
            TemplateId: "_Layout",
            SubjectTemplate: "",
            HtmlContent: "<html><body>{{Content}}</body></html>",
            PlainTextContent: ""
        ));

        _templateStore.AddTemplate(new EmailTemplate(
            TemplateId: "Inner",
            SubjectTemplate: "Test Subject",
            HtmlContent: "<p>Hello {{Name}}</p>",
            PlainTextContent: "Hello {{Name}}"
        ));

        var model = new TestModel { Name = "Alice", OrgName = "FEMA" };
        var result = await _renderer.RenderAsync("Inner", model);

        Assert.Equal("<html><body><p>Hello Alice</p></body></html>", result.HtmlBody);
    }

    // =========================================================================
    // RenderAsync - Error Handling
    // =========================================================================

    [Fact]
    public async Task RenderAsync_EmptyTemplateId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _renderer.RenderAsync("", new TestModel()));
    }

    [Fact]
    public async Task RenderAsync_NonexistentTemplate_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _renderer.RenderAsync("DoesNotExist", new TestModel()));
    }

    // =========================================================================
    // InMemoryEmailTemplateStore Tests
    // =========================================================================

    [Fact]
    public async Task TemplateStore_GetTemplate_ReturnsRegisteredTemplate()
    {
        var template = new EmailTemplate("Test", "Subject", "HTML", "Text");
        _templateStore.AddTemplate(template);

        var result = await _templateStore.GetTemplateAsync("Test");

        Assert.NotNull(result);
        Assert.Equal("Test", result.TemplateId);
    }

    [Fact]
    public async Task TemplateStore_GetTemplate_IsCaseInsensitive()
    {
        _templateStore.AddTemplate(new EmailTemplate("MyTemplate", "Subject", "HTML", "Text"));

        var result = await _templateStore.GetTemplateAsync("mytemplate");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task TemplateStore_GetTemplate_ReturnsNullForUnknown()
    {
        var result = await _templateStore.GetTemplateAsync("Unknown");

        Assert.Null(result);
    }

    // =========================================================================
    // Test Models
    // =========================================================================

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string OrgName { get; set; } = string.Empty;
    }

    private class MultiModel
    {
        public string Greeting { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    private class NullableModel
    {
        public string Name { get; set; } = string.Empty;
        public string? NullableField { get; set; }
    }

    private class DatedModel
    {
        public DateTime ExpiresAt { get; set; }
    }
}
