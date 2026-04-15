using System.Runtime.Serialization;

namespace Common.Models.Document
{
    [DataContract]
    public class DocumentVersion
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public int DocumentId { get; set; }

        [DataMember]
        public int VersionNumber { get; set; }

        [DataMember]
        public string FullText { get; set; } = string.Empty;

        [DataMember]
        public DateTime ValidFrom { get; set; }

        [DataMember]
        public DateTime? ValidTo { get; set; }

        [DataMember]
        public string ChangeDescription { get; set; } = string.Empty;

        [DataMember]
        public DateTime CreatedAt { get; set; }
    }
}
