using System.Runtime.Serialization;

namespace Common.Models.Query
{
    [DataContract]
    public class QueryLog
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string QuestionText { get; set; } = string.Empty;

        [DataMember]
        public DateTime? ContextDate { get; set; }

        [DataMember]
        public string ContextInfo { get; set; } = string.Empty;

        [DataMember]
        public string ResponseText { get; set; } = string.Empty;

        [DataMember]
        public double ConfidenceLevel { get; set; }

        [DataMember]
        public string ReferencedSections { get; set; } = string.Empty;  // JSON

        [DataMember]
        public DateTime CreatedAt { get; set; }

        [DataMember]
        public int ProcessingTimeMs { get; set; }
    }
}
