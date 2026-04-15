using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    [DataContract]
    public class Citation
    {
        [DataMember]
        public string SectionIdentifier { get; set; } = string.Empty;  // "Clan 12, Stav 1"

        [DataMember]
        public string Content { get; set; } = string.Empty;

        [DataMember]
        public string DocumentTitle { get; set; } = string.Empty;

        [DataMember]
        public int VersionNumber { get; set; }
    }
}
