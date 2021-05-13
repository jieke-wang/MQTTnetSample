using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using MQTTnet;
using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.Extensions;
using MQTTnet.Server;

namespace WebApiSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddHostedMqttServer(options => options.WithoutDefaultEndpoint())
                .AddMqttConnectionHandler()
                .AddConnections();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApiSample", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApiSample v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMqtt("/mqtt");
                endpoints.MapControllers();
            });

            app.UseMqttServer(server =>
            {
                server.StartedHandler = new MqttServerStartedHandlerDelegate(async args =>
                {
                    var frameworkName = GetType().Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?
                        .FrameworkName;

                    var msg = new MqttApplicationMessageBuilder()
                        .WithPayload($"Mqtt hosted on {frameworkName} is awesome")
                        .WithTopic("message");

                    while (true)
                    {
                        try
                        {
                            await server.PublishAsync(msg.Build());
                            msg.WithPayload($"Mqtt hosted on {frameworkName} is still awesome at {DateTime.Now}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        finally
                        {
                            await Task.Delay(TimeSpan.FromSeconds(2));
                        }
                    }
                });

                server.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(arg =>
                {
                    Console.WriteLine($"\n\n{arg.ClientId} 已连接\n\n");
                });

                server.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(arg =>
                {
                    Console.WriteLine($"\n\n{arg.ClientId} 已断开, 断开类型: {arg.DisconnectType}\n\n");
                });

                server.ClientSubscribedTopicHandler = new MqttServerClientSubscribedHandlerDelegate(arg =>
                {
                    Console.WriteLine($"[{arg.ClientId}] 订阅: {string.Join(", ", arg.TopicFilter.Topic)}");
                });

                server.ClientUnsubscribedTopicHandler = new MqttServerClientUnsubscribedTopicHandlerDelegate(arg =>
                {
                    Console.WriteLine($"[{arg.ClientId}] 取消订阅: {string.Join(", ", arg.TopicFilter)}");
                });
            });

            app.Use((context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Request.Path = "/Index.html";
                }

                return next();
            });

            app.UseStaticFiles();

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = "/node_modules",
                FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "node_modules"))
            });
        }
    }
}
