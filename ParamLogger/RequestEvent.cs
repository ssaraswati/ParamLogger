using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParamLogger
{
    public class RequestEvent
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string Body { get; set; }
        public IDictionary<string, string> QueryParameters { get; set; }
        public IDictionary<string, string> PathParameters { get; set; }
        public DateTime CreatedTimestamp { get; set; }
    }
}
