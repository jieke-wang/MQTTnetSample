using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Protocol;

namespace WebApiClientSample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMqttClientOptions _options;
        private readonly IMqttFactory _mqttFactory;
        private IMqttClient _mqttClient;
        private readonly MqttTopicFilter _mqttTopicFilter;

        const string Topic = "csharp_topic";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                //.WithCredentials(username, password)
                .WithCleanSession()
                .WithTcpServer(options =>
                {
                    options.Server = "127.0.0.1";
                    options.Port = 1883;
                })
                .Build();
            _mqttFactory = new MqttFactory();

            _mqttTopicFilter = new MqttTopicFilter
            {
                Topic = "#",
                QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce
            };
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _mqttClient = _mqttFactory.CreateMqttClient();
            _mqttClient.UseDisconnectedHandler(async arg =>
            {
                if (cancellationToken.IsCancellationRequested || _mqttClient.IsConnected) return;
                await _mqttClient.ReconnectAsync();
            });
            _mqttClient.UseConnectedHandler(arg =>
            {
                _logger.LogInformation($"连接状态: {arg.AuthenticateResult.ResultCode}");
            });

            _mqttClient.UseApplicationMessageReceivedHandler(arg =>
            {
                if (string.Equals(Topic, arg.ApplicationMessage.Topic, StringComparison.OrdinalIgnoreCase)) return;

                StringBuilder message = new StringBuilder();
                message.AppendLine($"\n\n===Consumer===");
                message.AppendLine($"接收来自 [{arg.ClientId}] 消息");
                message.AppendLine($"主题 [{arg.ApplicationMessage.Topic}]");
                message.AppendLine($"消息: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}");
                message.Append($"\n\n");
                _logger.LogInformation(message.ToString());

                string payLoad = $"回复: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}";
                _mqttClient.PublishAsync(new MqttApplicationMessage
                {
                    Topic = Topic,
                    Payload = Encoding.UTF8.GetBytes(payLoad),
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    UserProperties = arg.ApplicationMessage.UserProperties
                }, cancellationToken);
            });

            await _mqttClient.ConnectAsync(_options, cancellationToken);
            await _mqttClient.SubscribeAsync(new MqttClientSubscribeOptions
            {
                TopicFilters = new List<MqttTopicFilter> { _mqttTopicFilter },
            }, cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    string msg = $"{Environment.ProcessId} {Guid.NewGuid()} {DateTime.Now}";

                    Console.WriteLine($"\n\n发送消息: {msg}\n\n");
                    await _mqttClient.PublishAsync(new MqttApplicationMessage
                    {
                        Topic = Topic,
                        Payload = Encoding.UTF8.GetBytes(msg),
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    }, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                await Task.Delay(5000, stoppingToken);
                //await Task.Delay(999999, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _mqttClient?.UnsubscribeAsync(new MqttClientUnsubscribeOptions
            {
                TopicFilters = new List<string> { _mqttTopicFilter.Topic }
            }, cancellationToken);
            await _mqttClient?.DisconnectAsync(cancellationToken);
            _mqttClient?.Dispose();

            await base.StopAsync(cancellationToken);
        }
    }
}
