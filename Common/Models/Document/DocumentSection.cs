using System.Runtime.Serialization;
using Common.Enums;

namespace Common.Models.Document
{
    [DataContract]
    public class DocumentSection
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public int DocumentVersionId { get; set; }

        [DataMember]
        public string SectionIdentifier { get; set; } = string.Empty;

        [DataMember]
        public SectionType SectionType { get; set; }

        [DataMember]
        public int? ParentSectionId { get; set; }

        [DataMember]
        public int OrderIndex { get; set; }

        [DataMember]
        public string Content { get; set; } = string.Empty;
    }
}
