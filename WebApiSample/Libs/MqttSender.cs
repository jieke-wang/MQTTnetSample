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
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Protocol;

namespace WebApiSample.Libs
{
    public class MqttSender : IDisposable
    {
        private readonly ILogger<MqttSender> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IMqttClientOptions _options;
        private readonly IMqttFactory _mqttFactory;
        private IMqttClient _mqttClient;

        const string TopicPrefix = "browser/";
        const string username = "jieke";
        const string password = "wang";

        public MqttSender(ILogger<MqttSender> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;

            _options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithCredentials(username, password)
                .WithCleanSession()
                .WithTcpServer(options =>
                {
                    options.Server = "127.0.0.1";
                    options.Port = 1883;
                })
                .Build();
            _mqttFactory = new MqttFactory();
        }

        public async Task InitAsync(CancellationToken stoppingToken)
        {
            _mqttClient = _mqttFactory.CreateMqttClient();
            _mqttClient.UseDisconnectedHandler(async arg =>
            {
                if (stoppingToken.IsCancellationRequested || _mqttClient.IsConnected) return;
                await _mqttClient.ReconnectAsync();
            });
            _mqttClient.UseConnectedHandler(arg =>
            {
                _logger.LogInformation($"连接状态: {arg.AuthenticateResult.ResultCode}");
            });

            await _mqttClient.ConnectAsync(_options, _hostApplicationLifetime.ApplicationStopped);
        }

        public async Task<MqttClientPublishResult> SendAsync(string topic, string payload, CancellationToken cancellationToken = default)
        {
            return await _mqttClient.PublishAsync(new MqttApplicationMessage
            {
                Topic = $"{TopicPrefix}{topic}",
                Payload = Encoding.UTF8.GetBytes(payload),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            }, cancellationToken);
        }

        public void Dispose()
        {
            _mqttClient?.DisconnectAsync(_hostApplicationLifetime.ApplicationStopped).ConfigureAwait(false).GetAwaiter().GetResult();
            _mqttClient?.Dispose();
        }
    }
}
