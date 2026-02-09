using Cadence.Core.Data;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.SystemSettings.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Features.SystemSettings.Services;

public class EmailConfigurationProvider : IEmailConfigurationProvider
{
    private const string CacheKey = "EmailConfiguration";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _context;
    private readonly EmailServiceOptions _defaults;
    private readonly IMemoryCache _cache;

    public EmailConfigurationProvider(
        AppDbContext context,
        IOptions<EmailServiceOptions> defaults,
        IMemoryCache cache)
    {
        _context = context;
        _defaults = defaults.Value;
        _cache = cache;
    }

    public async Task<ResolvedEmailConfiguration> GetConfigurationAsync()
    {
        if (_cache.TryGetValue(CacheKey, out ResolvedEmailConfiguration? cached) && cached != null)
            return cached;

        var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();

        var resolved = new ResolvedEmailConfiguration
        {
            SupportAddress = settings?.SupportAddress ?? _defaults.SupportAddress,
            DefaultSenderAddress = settings?.DefaultSenderAddress ?? _defaults.DefaultSenderAddress,
            DefaultSenderName = settings?.DefaultSenderName ?? _defaults.DefaultSenderName,
        };

        _cache.Set(CacheKey, resolved, CacheDuration);

        return resolved;
    }
}
