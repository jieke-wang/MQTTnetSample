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
using MQTTnet.Extensions.Rpc;
using MQTTnet.Extensions.Rpc.Options;
using MQTTnet.Extensions.Rpc.Options.TopicGeneration;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace RpcClientSample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMqttClientOptions _options;
        private readonly IMqttFactory _mqttFactory;
        private IMqttClient _mqttClient;
        private readonly MqttUserProperty _mqttUserProperty;
        //private IMqttRpcClient _mqttRpcClient;
        private readonly IMqttRpcClientOptions _mqttRpcClientOptions;

        //const string requestTopic = "originator/destination/{0}/info";
        //const string responseTopic = "destination/originator/{0}/info";
        const string username = "jieke";
        const string password = "wang";
        //const string method = "demo_method";
        const string method = "method_name";
        const string appName = "rpc";

        public Worker(ILogger<Worker> logger)
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

            _mqttUserProperty = new MqttUserProperty(username, password);

            _mqttRpcClientOptions = new MqttRpcClientOptionsBuilder()
                .WithTopicGenerationStrategy(new MqttRpcClientTopicGenerationStrategy())
                .Build();
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
                _logger.LogInformation($"\n\n连接状态: {arg.AuthenticateResult.ResultCode}\n\n");
            });

            await _mqttClient.ConnectAsync(_options, cancellationToken);

            //_mqttRpcClient = new MqttRpcClient(_mqttClient, _mqttRpcClientOptions);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    #region
                    //string request = $"{Environment.ProcessId} {Guid.NewGuid()} {DateTime.Now}";
                    //Console.WriteLine($"\n\n发送请求: {request}\n\n");
                    //byte[] response = await _mqttRpcClient.ExecuteAsync(method, Encoding.UTF8.GetBytes(""), MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce, stoppingToken);

                    //byte[] response = await _mqttRpcClient.ExecuteAsync(TimeSpan.FromHours(1), method, Encoding.UTF8.GetBytes(request), MqttQualityOfServiceLevel.ExactlyOnce);
                    //byte[] response = await _mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), method, Encoding.UTF8.GetBytes(request), MqttQualityOfServiceLevel.ExactlyOnce);
                    //byte[] response = await _mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(100), method, request, MqttQualityOfServiceLevel.ExactlyOnce);
                    //Console.WriteLine($"\n\n收到响应: {Encoding.UTF8.GetString(response)}\n\n"); 
                    #endregion

                    using (IMqttRpcClient rpcClient = new MqttRpcClient(_mqttClient, _mqttRpcClientOptions))
                    {
                        string request = $"{Environment.ProcessId} {Guid.NewGuid()} {DateTime.Now}";
                        Console.WriteLine($"\n\n发送请求: {request}\n\n");
                        byte[] response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(100), method, request, MqttQualityOfServiceLevel.ExactlyOnce);
                        Console.WriteLine($"\n\n收到响应: {Encoding.UTF8.GetString(response)}\n\n");
                    }

                    //await Task.Delay(5000, stoppingToken);
                    //await Task.Delay(999999, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            //_mqttRpcClient?.Dispose();
            await _mqttClient?.DisconnectAsync(cancellationToken);
            _mqttClient?.Dispose();

            await base.StopAsync(cancellationToken);
        }

        public class MqttRpcClientTopicGenerationStrategy : IMqttRpcClientTopicGenerationStrategy
        {
            public MqttRpcTopicPair CreateRpcTopics(TopicGenerationContext context)
            {
                Console.WriteLine($"Method: {context.MethodName}");
                //return new MqttRpcTopicPair
                //{
                //    RequestTopic = string.Format(requestTopic, context.MethodName),
                //    ResponseTopic = string.Format(responseTopic, context.MethodName)
                //};
                return new MqttRpcTopicPair
                {
                    RequestTopic = $"MQTTnet.RPC/{appName}/{context.MethodName}",
                    ResponseTopic = $"MQTTnet.RPC/{appName}/{context.MethodName}/response"
                };
            }
        }
    }
}
