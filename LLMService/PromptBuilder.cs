using Common.Models.Document;
using Common.Models.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMService
{
    public static class PromptBuilder
    {
        public static string BuildSystemPrompt()
        {
            return @"Ti si AI pravni asistent specijalizovan za tumačenje zakona, pravilnika i internih politika.
                    PRAVILA:
                    1. Odgovaraj ISKLJUČIVO na osnovu priloženih dijelova dokumenata.
                    2. Za svaki dio odgovora OBAVEZNO navedi referencu u formatu [SectionIdentifier].
                    3. Ako nemaš dovoljno informacija za potpun odgovor, EKSPLICITNO to naglasi.
                    4. Ne donosi pravne odluke — samo tumači tekst propisa.
                    5. Odgovor formuliši jasno i razumljivo za krajnjeg korisnika.
                    6. Na SAMOM KRAJU odgovora, u zasebnom redu, napiši:
                       POUZDANOST: X.X (broj od 0.0 do 1.0)
                    7. U zasebnom redu napiši:
                       REFERENCE: SectionId1, SectionId2, ...";
        }

        public static string BuildUserPrompt(QueryRequest request, List<DocumentSection> sections)
        {
            var sb = new StringBuilder();

            sb.AppendLine("DOKUMENTI:");
            sb.AppendLine(new string('─', 40));

            foreach (var section in sections)
            {
                sb.AppendLine($"[{section.SectionIdentifier}]:");
                sb.AppendLine(section.Content);
                sb.AppendLine();
            }

            sb.AppendLine(new string('─', 40));
            sb.AppendLine();
            sb.AppendLine($"PITANJE: {request.QuestionText}");

            if (request.ContextDate.HasValue)
                sb.AppendLine($"KONTEKST DATUM: {request.ContextDate.Value:yyyy-MM-dd}");

            if (!string.IsNullOrEmpty(request.ContextInfo))
                sb.AppendLine($"DODATNI KONTEKST: {request.ContextInfo}");

            return sb.ToString();
        }
    }
}
