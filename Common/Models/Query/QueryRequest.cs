using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models.Query
{
    [DataContract]
    public class QueryRequest
    {
        [DataMember]
        public string QuestionText { get; set; } = string.Empty;

        [DataMember]
        public DateTime? ContextDate { get; set; }

        [DataMember]
        public string ContextInfo { get; set; } = string.Empty;  // npr. "tip_organizacije=javna_ustanova"
    }
}
