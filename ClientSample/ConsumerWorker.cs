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
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace ClientSample
{
    public class ConsumerWorker : BackgroundService
    {
        private readonly ILogger<ConsumerWorker> _logger;
        private readonly MqttTopicFilter _mqttTopicFilter;
        private readonly IMqttClientOptions _options;
        private readonly IMqttFactory _mqttFactory;
        private IMqttClient _mqttClient;
        private readonly MqttUserProperty _mqttUserProperty;

        const string producerTopic = "jieke/wang/producer";
        const string consumerTopic = "jieke/wang/consumer";
        const string username = "jieke";
        const string password = "wang";

        public ConsumerWorker(ILogger<ConsumerWorker> logger)
        {
            _logger = logger;
            _options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithCredentials(username, password)
                .WithCleanSession()
                .WithTcpServer(options =>
                {
                    options.Server = "127.0.0.1";
                    options.Port = 6666;
                })
                .Build();
            _mqttFactory = new MqttFactory();

            _mqttTopicFilter = new MqttTopicFilter
            {
                Topic = consumerTopic,
                QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce
            };

            _mqttUserProperty = new MqttUserProperty(username, password);
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
                //Console.WriteLine("\n\n");
                //_logger.LogInformation("===Consumer===");
                //_logger.LogInformation($"接收来自 [{arg.ClientId}] 消息");
                //_logger.LogInformation($"主题 [{arg.ApplicationMessage.Topic}]");
                //_logger.LogInformation($"回复主题 [{arg.ApplicationMessage.ResponseTopic}]");
                //_logger.LogInformation($"消息: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}");
                //Console.WriteLine("\n\n");

                StringBuilder message = new StringBuilder();
                message.AppendLine($"\n\n===Consumer===");
                message.AppendLine($"接收来自 [{arg.ClientId}] 消息");
                message.AppendLine($"主题 [{arg.ApplicationMessage.Topic}]");
                message.AppendLine($"消息: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}");
                message.Append($"\n\n");
                _logger.LogInformation(message.ToString());

                //if (string.IsNullOrWhiteSpace(arg.ApplicationMessage.ResponseTopic) == false)
                //{
                //    string payLoad = $"回复: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}";
                //    _mqttClient.PublishAsync(new MqttApplicationMessage
                //    {
                //        Topic = producerTopic,
                //        Payload = Encoding.UTF8.GetBytes(payLoad),
                //        QualityOfServiceLevel = arg.ApplicationMessage.QualityOfServiceLevel,
                //        ResponseTopic = consumerTopic,
                //        UserProperties = arg.ApplicationMessage.UserProperties
                //    }, cancellationToken);
                //}

                string payLoad = $"回复: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}";
                _mqttClient.PublishAsync(new MqttApplicationMessage
                {
                    Topic = producerTopic,
                    Payload = Encoding.UTF8.GetBytes(payLoad),
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    //ResponseTopic = consumerTopic,
                    UserProperties = arg.ApplicationMessage.UserProperties
                }, cancellationToken);

                //arg.ProcessingFailed = false;
                //arg.ReasonCode = MqttApplicationMessageReceivedReasonCode.Success;
                //arg.IsHandled = true;
            });

            await _mqttClient.ConnectAsync(_options, cancellationToken);
            await _mqttClient.SubscribeAsync(new MqttClientSubscribeOptions
            {
                TopicFilters = new List<MqttTopicFilter> { _mqttTopicFilter },
                UserProperties = new List<MqttUserProperty> { _mqttUserProperty }
            }, cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _mqttClient.PingAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                stoppingToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(60));
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
