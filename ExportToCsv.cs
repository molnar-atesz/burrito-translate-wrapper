using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BurritoTranslate
{
    public static class ExportToCsv
    {
        [FunctionName("ExportToCsv")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Export request processing.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Glossary glossary = JsonConvert.DeserializeObject<Glossary>(requestBody);
            var fileName = Uri.EscapeUriString($"glossary-{glossary.Id}-{glossary.Created}.csv");

            var csv = TransformToCsv(glossary);

            byte[] fileBytes = Encoding.UTF8.GetBytes(csv);
            // for local testing need to set CORS, see: https://stackoverflow.com/a/48069299/8076798
            return new FileContentResult(fileBytes, "text/csv") { FileDownloadName = fileName };
        }

        private static string TransformToCsv(Glossary glossary)
        {
            StringBuilder csvBuilder = new StringBuilder();
            csvBuilder.AppendLine($"{glossary.Id.Replace("-", ",")},note");
            foreach (var item in glossary.Items)
            {
                csvBuilder.AppendLine($"{item.Original},{item.Translation},{item.Note}");
            }
            return csvBuilder.ToString();
        }
    }
    public class Glossary
    {
        public string Id { get; set; }
        public DateTime Created { get; set; }
        public List<GlossaryItem> Items { get; set; }

        public override string ToString()
        {
            return $"{Id} - {Created}\n{string.Join("\n\t", Items)}";
        }
    }

    public class GlossaryItem
    {
        public string Original { get; set; }
        public string Translation { get; set; }
        public string Note { get; set; }

        public override string ToString()
        {
            return $"{Original} -> {Translation}, {Note}";
        }
    }
}
