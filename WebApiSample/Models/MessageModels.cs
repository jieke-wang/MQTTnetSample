using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiSample.Models
{
    public class MessageModels
    {
        public class SendInput
        {
            public string Topic { get; set; }
            public string Payload { get; set; }
        }
    }
}
