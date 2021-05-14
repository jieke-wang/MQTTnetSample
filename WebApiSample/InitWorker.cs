using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using WebApiSample.Libs;

namespace WebApiSample
{
    public class InitWorker : BackgroundService
    {
        private readonly MqttSender _mqttSender;

        public InitWorker(MqttSender mqttSender)
        {
            _mqttSender = mqttSender;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _mqttSender.InitAsync(stoppingToken);
        }
    }
}
