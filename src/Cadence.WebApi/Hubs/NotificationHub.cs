using Microsoft.AspNetCore.SignalR;

namespace Cadence.WebApi.Hubs;

public class NotificationHub : Hub
{
    // This hub is primarily used for server-to-client notifications.
    // Clients connect to receive updates.
}
