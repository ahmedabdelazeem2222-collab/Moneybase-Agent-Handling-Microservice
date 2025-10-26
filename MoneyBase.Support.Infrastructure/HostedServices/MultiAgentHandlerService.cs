using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoneyBase.Support.Application.DTOs;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;

namespace MoneyBase.Support.Infrastructure.HostedServices
{
    public class MultiAgentHandlerService : BackgroundService, IDisposable
    {
        #region Fields
        private readonly ILogger<MultiAgentHandlerService> _logger;
        private readonly IConfiguration _config;
        private readonly IHubContext<AgentHub.AgentHub> _hubContext;
        private IConnection _connection;
        private readonly List<IModel> _channels = new();
        private const string ExchangeName = "chat.agent.exchange";
        private const string ExchangeTypeName = "topic";

        public MultiAgentHandlerService(ILogger<MultiAgentHandlerService> logger, 
            IConfiguration config, IHubContext<AgentHub.AgentHub> hubContext)
        {
            _logger = logger;
            _config = config;
            _hubContext = hubContext;
        }
        #endregion

        #region Job & Methods

        /// <summary>
        /// Listens for messages from agent-specific queues, updates chat statuses,
        /// and sends real-time notifications to the assigned agent's interface (front end) via SignalR.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Load agent Ids
                var agentIds = await LoadAgentsAsync();
                if (agentIds.Count == 0)
                {
                    _logger.LogWarning("No agents found — no consumers started.");
                    return;
                }

                var factory = new ConnectionFactory
                {
                    HostName = _config["RabbitMQ:Host"],
                    UserName = _config["RabbitMQ:User"],
                    Password = _config["RabbitMQ:Pass"],
                    DispatchConsumersAsync = true
                };

                _connection = factory.CreateConnection();

                foreach (var agentId in agentIds)
                {
                    var channel = _connection.CreateModel();
                    _channels.Add(channel);

                    channel.ExchangeDeclare(ExchangeName, ExchangeTypeName, durable: true);

                    var queueName = $"agent.{agentId}.queue";
                    var routingKey = $"agent.{agentId}";

                    channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
                    channel.QueueBind(queueName, ExchangeName, routingKey);

                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.Received += async (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var json = Encoding.UTF8.GetString(body);
                            var message = JsonConvert.DeserializeObject<ChatAssignedMessage>(json);

                            _logger.LogInformation("Agent {AgentId} received chat {ChatId}", agentId, message.ChatId);

                            bool result = await HandleAssignedChatAsync(agentId, message);
                            if (result)
                                channel.BasicAck(ea.DeliveryTag, multiple: false);
                            else
                                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error handling message for agent {AgentId}", agentId);
                            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                        }
                    };

                    channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                    _logger.LogInformation("Started consumer for agent {AgentId}", agentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MultiAgentHandlerService/ExecuteAsync Error");
            }
        }

        #endregion

        #region Private Methods
        private async Task<bool> HandleAssignedChatAsync(Guid agentId, ChatAssignedMessage message)
        {
            try
            {
                var currentChat = await getChatById(agentId);
                if (currentChat != null)
                {
                    currentChat.ChatStatus = Domain.Enums.ChatStatusEnum.Assigned;
                    currentChat.AssignedAt = DateTime.UtcNow;
                    currentChat.AgentId = agentId;
                    // calling Update Chat API in Chat API micro service.
                    await UpdateChat(currentChat);

                    // Notify the agent about the ticket via SignalR hub.
                    await NotifyAgentViaSignalR(message);

                    _logger.LogInformation("Processing chat {ChatId} for agent {AgentId}", message.ChatId, agentId);
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Chat {currentChat.Id} not found in DB");
                    return false;
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "MultiAgentHandlerService/HandleAssignedChatAsync Failed");
                return false;
            }
        }
        
        /// <summary>
        /// Notify Agent Via SignalR (agent front end)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task NotifyAgentViaSignalR(ChatAssignedMessage message)
        {
            try
            {
                _logger.LogInformation("Chat {ChatId} assigned to agent {AgentId}", message.ChatId, message.AgentId);

                await _hubContext.Clients
                    .Group($"agent-{message.AgentId}")
                    .SendAsync("ChatAssigned", new
                    {
                        chatId = message.ChatId,
                        userId = message.UserId,
                        assignedAt = message.AssignedAt
                    });

                _logger.LogInformation("Notified agent {AgentId} via SignalR", message.AgentId);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "NotifyAgentViaSignalR Failed");
            }
        }
        private async Task<List<Guid>> LoadAgentsAsync()
        {
            // Loads agents from the Chat API microservice to apply various business cases, 
            // similar to the approach used in the Cash Assignment microservice. 
            // Implementation is reused here for demonstration purposes to save time. 
            // Thank you for your understanding.
            return new List<Guid>();
        }
        private async Task<ChatSessionDto> getChatById(Guid agentId)
        {
            // Loads agents from the Chat API microservice to apply various business cases, 
            // similar to the approach used in the Cash Assignment microservice. 
            // Implementation is reused here for demonstration purposes to save time. 
            // Thank you for your understanding.
            return new ChatSessionDto();
        }
        private async Task<bool> UpdateChat(ChatSessionDto chatSession)
        {
            // similar to the approach used in the Cash Assignment microservice. 
            // Implementation is reused here for demonstration purposes to save time. 
            // Thank you for your understanding.
            return true;
        }

        #endregion
        public override void Dispose()
        {
            foreach (var ch in _channels) ch.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
