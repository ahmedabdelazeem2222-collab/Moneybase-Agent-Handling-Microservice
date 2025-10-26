using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyBase.Support.Infrastructure.AgentHub
{
    public class AgentHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var agentId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(agentId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"agent-{agentId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var agentId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(agentId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"agent-{agentId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
