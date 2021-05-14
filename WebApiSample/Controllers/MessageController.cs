using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using WebApiSample.Libs;
using WebApiSample.Models;

namespace WebApiSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> SendAsync(MessageModels.SendInput input, [FromServices] MqttSender mqttSender)
        {
            var result = await mqttSender.SendAsync(input.Topic, input.Payload);
            return Ok(new { result.ReasonCode, result.ReasonString });
        }
    }
}
