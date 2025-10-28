using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerpAPI_Bot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class JobDataConverter : IJobDataConverter
    {
        /// <summary>
        /// Batch converts raw job JSON files in dataDirectory into cleaned JSONL files in outputDirectory.
        /// Optional AI labeling can enrich each job with structured fields.
        /// </summary>
        public async Task BatchConvertAsync(string dataDirectory, string outputDirectory, bool useAiLabels = false)
        {
            if (!Directory.Exists(dataDirectory))
            {
                Console.WriteLine("❌ Data directory does not exist.");
                return;
            }

            var files = Directory.GetFiles(dataDirectory, "*.json");
            if (files.Length == 0)
            {
                Console.WriteLine("❌ No JSON files found in data directory.");
                return;
            }

            Directory.CreateDirectory(outputDirectory);

            int fileIndex = 0;
            foreach (var file in files)
            {
                fileIndex++;
                string jsonText = await File.ReadAllTextAsync(file);
                List<Dictionary<string, object>> jobs;
                try
                {

                    var root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonText);

                    // Access the jobs array
                    var jobsJson = root["jobs_results"];

                    // Deserialize jobs into list of dictionaries
                    jobs = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jobsJson.GetRawText()); 
                }
                catch
                {
                    Console.WriteLine($"❌ Skipping invalid JSON file: {file}");
                    continue;
                }

                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                string outputPath = Path.Combine(outputDirectory, fileNameWithoutExt + ".jsonl");

                int totalJobs = jobs.Count;
                int written = 0;

                using var writer = new StreamWriter(outputPath);
                foreach (var job in jobs)
                {
                    // Normalize company name from fetched JSON
                    string company = "";
                    if (job.ContainsKey("companyName"))
                        company = job["companyName"]?.ToString()?.Trim() ?? "";
                    else if (job.ContainsKey("company_name"))
                        company = job["company_name"]?.ToString()?.Trim() ?? "";

                    string title = job.ContainsKey("title") ? job["title"]?.ToString()?.Trim() ?? "" : "";
                    string description = job.ContainsKey("description") ? job["description"]?.ToString()?.Trim() ?? "" : "";
                    string location = job.ContainsKey("location") ? job["location"]?.ToString()?.Trim() ?? "" : "";

                    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description)) continue;

                    description = Regex.Replace(description, "<.*?>", string.Empty);

                    var cleanJob = new Dictionary<string, object>
                    {
                        ["title"] = title,
                        ["description"] = description,
                        ["companyName"] = company, // always filled now
                        ["location"] = location
                    };

                    if (useAiLabels)
                        cleanJob["labels"] = await GetAiLabelsAsync(cleanJob);
                    else
                        cleanJob["labels"] = GetRuleBasedLabels(cleanJob);

                    string jsonLine = JsonSerializer.Serialize(cleanJob);
                    await writer.WriteLineAsync(jsonLine);
                    written++;
                    ConsoleProgress.DrawProgress(written, totalJobs);
                }

                Console.WriteLine($"✅ Converted {written}/{totalJobs} jobs → {outputPath}");
            }
        }

        /// <summary>
        /// Merges all JSONL files in inputDirectory into a single outputFile.
        /// Optionally generates a dashboard.json/html for preview.
        /// </summary>
        public async Task MergeJsonlFilesAsync(string inputDirectory, string outputFile, bool generateDashboard = false)
        {
            if (!Directory.Exists(inputDirectory))
            {
                Console.WriteLine("❌ Input directory does not exist.");
                return;
            }

            var files = Directory.GetFiles(inputDirectory, "*.jsonl");
            if (files.Length == 0)
            {
                Console.WriteLine("❌ No JSONL files found to merge.");
                return;
            }

            var allJobs = new List<Dictionary<string, object>>();

            foreach (var file in files)
            {
                string[] lines = await File.ReadAllLinesAsync(file);
                foreach (var line in lines)
                {
                    try
                    {
                        var job = JsonSerializer.Deserialize<Dictionary<string, object>>(line);
                        if (job != null) allJobs.Add(job);
                    }
                    catch { continue; }
                }
            }

            // Remove duplicates
            allJobs = allJobs
                .GroupBy(j => $"{j["title"]}_{j["companyName"]}_{j["location"]}")
                .Select(g => g.First())
                .ToList();

            using var writer = new StreamWriter(outputFile);
            int totalJobs = allJobs.Count;
            int written = 0;
            foreach (var job in allJobs)
            {
                string line = JsonSerializer.Serialize(job);
                await writer.WriteLineAsync(line);
                written++;
                ConsoleProgress.DrawProgress(written, totalJobs);
            }

            Console.WriteLine($"✅ Merged {written} jobs → {outputFile}");

            if (generateDashboard)
            {
                await GenerateDashboardAsync(allJobs, Path.Combine(Path.GetDirectoryName(outputFile), "dashboard.html"));
            }
        }

        // -------------------
        // Helper functions
        // -------------------

        private Dictionary<string, string> GetRuleBasedLabels(Dictionary<string, object> job)
        {
            var labels = new Dictionary<string, string>();
            string title = job["title"].ToString().ToLower();
            string desc = job["description"].ToString().ToLower();

            labels["industry"] = "Software";
            labels["seniority"] = title.Contains("senior") ? "Senior" :
                                 title.Contains("junior") ? "Junior" : "Mid-Level";
            labels["employment_type"] = desc.Contains("full-time") ? "Full-time" :
                                        desc.Contains("part-time") ? "Part-time" : "Contract";

            return labels;
        }

        private async Task<Dictionary<string, string>> GetAiLabelsAsync(Dictionary<string, object> job)
        {
            // Placeholder for AI labeling integration
            // In production, call OpenAI API with job content to generate structured labels
            await Task.Delay(10); // simulate async call
            return GetRuleBasedLabels(job);
        }

        private async Task GenerateDashboardAsync(List<Dictionary<string, object>> jobs, string outputHtmlPath)
        {
            int total = jobs.Count;
            var companies = jobs.Select(j => j["companyName"].ToString()).Distinct().Count();
            var locations = jobs.Select(j => j["location"].ToString()).Distinct().Count();

            string html = $@"
<html>
<head><title>Job Dashboard</title></head>
<body>
<h1>Job Dataset Dashboard</h1>
<p>Total jobs: {total}</p>
<p>Unique companies: {companies}</p>
<p>Unique locations: {locations}</p>
</body>
</html>";

            await File.WriteAllTextAsync(outputHtmlPath, html);
            Console.WriteLine($"✅ Dashboard generated → {outputHtmlPath}");
        }
    }
}
