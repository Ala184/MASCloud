using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models.Query
{
    [DataContract]
    public class QueryResponse
    {
        [DataMember]
        public string Explanation { get; set; } = string.Empty;

        [DataMember]
        public List<Citation> Citations { get; set; } = new List<Citation>();

        [DataMember]
        public double ConfidenceLevel { get; set; }  // 0.0 – 1.0

        [DataMember]
        public List<string> Warnings { get; set; } = new List<string>();

        [DataMember]
        public int ProcessingTimeMs { get; set; }
    }
}
