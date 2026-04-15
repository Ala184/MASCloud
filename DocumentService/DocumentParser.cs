using Common.Enums;
using Common.Models.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DocumentService
{
    public static class DocumentParser
    {
        // Regex obrasci za prepoznavanje strukture
        private static readonly Regex ClanRegex = new(@"^Član\s+(\d+)\.?", RegexOptions.Multiline);
        private static readonly Regex StavRegex = new(@"^\((\d+)\)", RegexOptions.Multiline);
        private static readonly Regex TackaRegex = new(@"^([a-z])\)", RegexOptions.Multiline);

        public static List<DocumentSection> Parse(string fullText, int versionId)
        {
            var sections = new List<DocumentSection>();
            int orderIndex = 0;

            // Razdvoji na članove
            var clanMatches = ClanRegex.Matches(fullText);

            for (int i = 0; i < clanMatches.Count; i++)
            {
                var clanMatch = clanMatches[i];
                int clanNumber = int.Parse(clanMatch.Groups[1].Value);
                string clanIdentifier = $"Član {clanNumber}";

                // Odredi tekst između ovog i sljedećeg člana
                int startIndex = clanMatch.Index + clanMatch.Length;
                int endIndex = (i + 1 < clanMatches.Count)
                    ? clanMatches[i + 1].Index
                    : fullText.Length;
                string clanContent = fullText[startIndex..endIndex].Trim();

                var clanSection = new DocumentSection
                {
                    DocumentVersionId = versionId,
                    SectionIdentifier = clanIdentifier,
                    SectionType = SectionType.Clan,
                    ParentSectionId = null,
                    OrderIndex = orderIndex++,
                    Content = clanContent
                };
                sections.Add(clanSection);

                var stavMatches = StavRegex.Matches(clanContent);
                for (int j = 0; j < stavMatches.Count; j++)
                {
                    var stavMatch = stavMatches[j];
                    int stavNumber = int.Parse(stavMatch.Groups[1].Value);
                    string stavIdentifier = $"{clanIdentifier}, Stav {stavNumber}";

                    int stavStart = stavMatch.Index + stavMatch.Length;
                    int stavEnd = (j + 1 < stavMatches.Count)
                        ? stavMatches[j + 1].Index
                        : clanContent.Length;
                    string stavContent = clanContent[stavStart..stavEnd].Trim();

                    var stavSection = new DocumentSection
                    {
                        DocumentVersionId = versionId,
                        SectionIdentifier = stavIdentifier,
                        SectionType = SectionType.Stav,
                        ParentSectionId = null, //This task is delegated to DB
                        OrderIndex = orderIndex++,
                        Content = stavContent
                    };
                    sections.Add(stavSection);

                    var tackaMatches = TackaRegex.Matches(stavContent);
                    foreach (Match tackaMatch in tackaMatches)
                    {
                        string tackaLetter = tackaMatch.Groups[1].Value;
                        string tackaIdentifier = $"{stavIdentifier}, Tačka {tackaLetter})";

                        var tackaSection = new DocumentSection
                        {
                            DocumentVersionId = versionId,
                            SectionIdentifier = tackaIdentifier,
                            SectionType = SectionType.Tacka,
                            ParentSectionId = null,
                            OrderIndex = orderIndex++,
                            Content = tackaMatch.Value
                        };
                        sections.Add(tackaSection);
                    }
                }
            }

            return sections;
        }
    }
}
