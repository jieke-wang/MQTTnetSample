# MQTTnetSample
MQTT简单示例

> https://mqtt.org/

> https://github.com/chkr1011/MQTTnet
>
> https://www.nuget.org/packages/MQTTnet
>
> https://www.nuget.org/packages/MQTTnet.Extensions.Rpc/
>
> https://www.nuget.org/packages/MQTTnet.AspNetCore/

> MQTT（一）C# 使用 MQTTnet 快速实现 MQTT 通信
> https://blog.csdn.net/panwen1111/article/details/79245161
>
> 使用 MQTTnet 部署 MQTT 服务
> https://www.cnblogs.com/wx881208/p/14325011.html
>
> 使用MQTTNET搭建MQTT服务器
> https://www.freesion.com/article/775633390/
>
> 基于 MQTT 的 RPC 协议
> https://blog.csdn.net/yaojiawan/article/details/101282825

> https://github.com/chkr1011/MQTTnet/issues/72
>
> https://github.com/chkr1011/MQTTnet/blob/master/Tests/MQTTnet.Core.Tests/RPC_Tests.cs
>
> https://github.com/chkr1011/MQTTnet/blob/master/Tests/MQTTnet.TestApp.AspNetCore2/Program.cs
>
> https://github.com/chkr1011/MQTTnet/blob/master/Source/MQTTnet.Extensions.Rpc/MqttRpcClient.cs
>
> https://github.com/chkr1011/MQTTnet/blob/master/Source/MQTTnet.Extensions.WebSocket4Net/WebSocket4NetMqttChannel.cs

```csharp
// 构建结果通知结合, key为通知标识, value为结果容器
ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _waitingCalls = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();

string topic = "xxxxxx";
try
{
    // 构建存储结果的容器
    var promise = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
    // 将容器添加到通知集合中
    _waitingCalls.TryAdd(topic, promise);

    // 应用超时取消机制
    using (cancellationToken.Register(() => { promise.TrySetCanceled(); }))
    {	// 异步等待结果
        return await promise.Task.ConfigureAwait(false);
    }
}
finall
{	// 从通知集合中移除
    _waitingCalls.TryRemove(topic, out _);
}

// 异步设置结果
promise.TrySetResult(eventArgs.ApplicationMessage.Payload);
```

