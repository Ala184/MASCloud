using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public class LLMResult
    {
        [DataMember]
        public string InterpretationText { get; set; } = string.Empty;

        [DataMember]
        public List<string> ReferencedSectionIds { get; set; } = new List<string>();

        [DataMember]
        public double ConfidenceLevel { get; set; }

        [DataMember]
        public bool InsufficientInformation { get; set; }
    }
}
