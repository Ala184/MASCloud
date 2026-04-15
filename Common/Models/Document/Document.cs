using Common.Enums;
using System.Runtime.Serialization;

namespace Common.Models.Document
{
    [DataContract]
    public class Document
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Title { get; set; } = string.Empty;

        [DataMember]
        public DocumentType DocumentType { get; set; }

        [DataMember]
        public DateTime CreatedAt { get; set; }

        [DataMember]
        public string CreatedBy { get; set; } = string.Empty;

        [DataMember]
        public DateTime? ValidUntil { get; set; }
    }
}
