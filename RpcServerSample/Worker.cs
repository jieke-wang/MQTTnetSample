using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace RpcServerSample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMqttServerOptions _mqttServerOptions;
        private readonly IMqttFactory _mqttFactory;
        private IMqttServer _mqttServer;

        const string requestTopic = "originator/destination/method/info";
        const string responseTopic = "destination/originator/method/info";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            _mqttServerOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpointPort(6666)
                .WithConnectionValidator(context =>
                {
                    if (context.ClientId.Length < 10)
                    {
                        context.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
                        return;
                    }

                    if (context.Username != "jieke" || context.Password != "wang")
                    {
                        context.ReasonCode = MqttConnectReasonCode.NotAuthorized;
                        return;
                    }

                    context.ReasonCode = MqttConnectReasonCode.Success;
                })
                .Build();
            _mqttFactory = new MqttFactory();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _mqttServer = _mqttFactory.CreateMqttServer();

            // 客户端连接
            _mqttServer.UseClientConnectedHandler(arg =>
            {
                _logger.LogInformation($"\n\n{arg.ClientId} 已连接\n\n");
            });
            // 客户端断开
            _mqttServer.UseClientDisconnectedHandler(arg =>
            {
                _logger.LogInformation($"\n\n{arg.ClientId} 已断开, 断开类型: {arg.DisconnectType}\n\n");
            });
            // 接收客户端发来的消息
            _mqttServer.UseApplicationMessageReceivedHandler(async arg =>
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine($"\n\n===Server===");
                message.AppendLine($"接收来自 [{arg.ClientId}] 消息");
                message.AppendLine($"主题 [{arg.ApplicationMessage.Topic}]");
                message.AppendLine($"消息: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}");
                message.Append($"\n\n");
                _logger.LogInformation(message.ToString());

                if (string.IsNullOrWhiteSpace(arg.ApplicationMessage.ResponseTopic) == false)
                {
                    string payLoad = $"回复: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}";
                    await _mqttServer.PublishAsync(new MqttApplicationMessage
                    {
                        Topic = arg.ApplicationMessage.ResponseTopic,
                        Payload = Encoding.UTF8.GetBytes(payLoad),
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                        ResponseTopic = arg.ApplicationMessage.Topic,
                        UserProperties = arg.ApplicationMessage.UserProperties,
                        SubscriptionIdentifiers = arg.ApplicationMessage.SubscriptionIdentifiers,
                        CorrelationData = arg.ApplicationMessage.CorrelationData
                    }, cancellationToken);
                }

                //arg.ProcessingFailed = false;
                //arg.ReasonCode = MqttApplicationMessageReceivedReasonCode.Success;
                //arg.IsHandled = true;
            });

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _mqttServer.StartAsync(_mqttServerOptions);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _mqttServer?.ClearRetainedApplicationMessagesAsync();
            await _mqttServer?.StopAsync();
            _mqttServer?.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }
}
