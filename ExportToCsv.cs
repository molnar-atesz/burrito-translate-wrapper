using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Storage.Blob;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BurritoTranslate
{
    public static class ExportToCsv
    {
        [FunctionName("ExportToCsv")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Blob("burritocontainer", Connection = "StorageConnectionString")] CloudBlobContainer outputContainer,
            ILogger log)
        {
            log.LogInformation("Export request processing.");

            await outputContainer.CreateIfNotExistsAsync();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Glossary glossary = JsonConvert.DeserializeObject<Glossary>(requestBody);
            var blobName = Uri.EscapeUriString($"glossary-{glossary.Id}-{glossary.Created}.csv");

            var csv = TransformToCsv(glossary);

            var cloudBlockBlob = outputContainer.GetBlockBlobReference(blobName);
            await cloudBlockBlob.UploadTextAsync(csv);
            System.Console.WriteLine($"Uri: {cloudBlockBlob.Uri}");
            System.Console.WriteLine($"storageUri: {cloudBlockBlob.StorageUri}");

            string responseMessage = "";

            return new OkObjectResult(responseMessage);
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
