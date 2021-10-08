using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace logging_middleware.ApiModels
{
    public class Output
    {
        public string Message { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
