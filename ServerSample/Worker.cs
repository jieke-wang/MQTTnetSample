using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace ServerSample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMqttServerOptions _mqttServerOptions;
        private readonly IMqttFactory _mqttFactory;
        private IMqttServer _mqttServer;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            _mqttServerOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpointPort(6666)
                .WithConnectionValidator(context => 
                {
                    if(context.ClientId.Length < 10)
                    {
                        context.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
                        return;
                    }

                    if(context.Username != "jieke" || context.Password != "wang")
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
                _logger.LogInformation($"{arg.ClientId} 已连接");
            });
            // 客户端断开
            _mqttServer.UseClientDisconnectedHandler(arg =>
            {
                _logger.LogInformation($"{arg.ClientId} 已断开, 类型为: {arg.DisconnectType}");
            });
            // 接收客户端发来的消息
            _mqttServer.UseApplicationMessageReceivedHandler(arg => 
            {
                _logger.LogInformation("===Server===");
                _logger.LogInformation($"接收来自 [{arg.ClientId}] 消息");
                _logger.LogInformation($"主题 [{arg.ApplicationMessage.Topic}]");
                _logger.LogInformation($"回复主题 [{arg.ApplicationMessage.ResponseTopic}]");
                _logger.LogInformation($"消息: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)}");
                Console.WriteLine("\n\n");

                if (string.IsNullOrWhiteSpace(arg.ApplicationMessage.ResponseTopic) == false)
                {
                    _mqttServer.PublishAsync(new MqttApplicationMessage 
                    {
                        Topic = arg.ApplicationMessage.ResponseTopic,
                        Payload = arg.ApplicationMessage.Payload,
                        QualityOfServiceLevel = arg.ApplicationMessage.QualityOfServiceLevel,
                        ResponseTopic = arg.ApplicationMessage.Topic,
                        UserProperties = arg.ApplicationMessage.UserProperties
                    }, cancellationToken);
                }

                arg.ProcessingFailed = false;
                arg.ReasonCode = MqttApplicationMessageReceivedReasonCode.Success;
                arg.IsHandled = true;
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
        }
    }
}
