using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MQTTnet.AspNetCore.Extensions;

namespace WebApiSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(options => 
                    {
                        options.ListenAnyIP(1883, lo => lo.UseMqtt());
                        options.ListenAnyIP(5000);
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}

// https://github.com/chkr1011/MQTTnet/blob/master/Tests/MQTTnet.TestApp.AspNetCore2/Program.cs
// npm install
