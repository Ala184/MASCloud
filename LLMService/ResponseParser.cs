using Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LLMService
{
    public static class ResponseParser
    {
        private static readonly Regex ConfidenceRegex =
            new(@"POUZDANOST:\s*([\d.]+)", RegexOptions.IgnoreCase);

        private static readonly Regex ReferencesRegex =
            new(@"REFERENCE:\s*(.+)", RegexOptions.IgnoreCase);

        private static readonly Regex InlineSectionRefRegex =
            new(@"\[(Član\s+\d+[^]]*)\]");

        public static LLMResult Parse(string llmOutput)
        {
            var result = new LLMResult();

            var confidenceMatch = ConfidenceRegex.Match(llmOutput);
            if (confidenceMatch.Success)
            {
                if (double.TryParse(confidenceMatch.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double conf))
                {
                    result.ConfidenceLevel = Math.Clamp(conf, 0.0, 1.0);
                }
            }

            var referencesMatch = ReferencesRegex.Match(llmOutput);
            if (referencesMatch.Success)
            {
                var refs = referencesMatch.Groups[1].Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList();
                result.ReferencedSectionIds = refs;
            }

            if (result.ReferencedSectionIds.Count == 0)
            {
                var inlineRefs = InlineSectionRefRegex.Matches(llmOutput)
                    .Select(m => m.Groups[1].Value)
                    .Distinct()
                    .ToList();
                result.ReferencedSectionIds = inlineRefs;
            }

            string interpretationText = llmOutput;
            var firstMetaLine = new Regex(@"^(POUZDANOST:|REFERENCE:)", RegexOptions.Multiline);
            var metaMatch = firstMetaLine.Match(llmOutput);
            if (metaMatch.Success)
            {
                interpretationText = llmOutput[..metaMatch.Index].Trim();
            }
            result.InterpretationText = interpretationText;

            result.InsufficientInformation =
                llmOutput.Contains("nedovoljno informacija", StringComparison.OrdinalIgnoreCase) ||
                llmOutput.Contains("nema dovoljno", StringComparison.OrdinalIgnoreCase) ||
                llmOutput.Contains("ne mogu sa sigurnošću", StringComparison.OrdinalIgnoreCase);

            return result;
        }
    }
}
