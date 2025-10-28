using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SerpAPI_Bot;

// -------------------
// Load config.txt
// -------------------
ConfigReader.Load("config.txt");

// Read API keys from config
string serpApiKey = ConfigReader.Get("SERPAPI_KEY");
string openAiKey = ConfigReader.Get("OPENAI_API_KEY"); // optional for AI labeling

if (string.IsNullOrEmpty(serpApiKey))
{
    WriteError("❌ SERPAPI_KEY missing in config.txt");
    return;
}

// Directories from config
var dataDir = ConfigReader.Get("DATA_DIR", "data");
var fineTuneDir = ConfigReader.Get("OUTPUT_DIR", "finetune");
Directory.CreateDirectory(dataDir);
Directory.CreateDirectory(fineTuneDir);

// Default query/location from config
string defaultQuery = ConfigReader.Get("DEFAULT_QUERY", "Software Engineer");
string defaultLocation = ConfigReader.Get("DEFAULT_LOCATION", "Remote");

// -------------------
// Parse CLI arguments
// -------------------
var argsSet = args.Select(a => a.ToLowerInvariant()).ToHashSet();
bool showHelp = argsSet.Contains("--help") || argsSet.Contains("-h");
bool fetchMode = argsSet.Contains("--fetch");
bool batchMode = argsSet.Contains("--batch");
bool mergeMode = argsSet.Contains("--merge");
bool useAiLabels = argsSet.Contains("--ai-labels");
bool generateDashboard = argsSet.Contains("--dashboard");

string query = args.FirstOrDefault(a => a.StartsWith("--query="))?.Split('=')[1] ?? defaultQuery;
string location = args.FirstOrDefault(a => a.StartsWith("--location="))?.Split('=')[1] ?? defaultLocation;

// Show help and exit
if (showHelp || args.Length == 0)
{
    ShowHelp();
    return;
}

// -------------------
// Display active mode
// -------------------
WriteInfo("🚀 Job Dataset Converter starting...");
WriteInfo($"Mode: {(fetchMode ? "Fetch, " : "")}{(batchMode ? "Batch Conversion, " : "")}{(mergeMode ? "Merge" : "")}");
WriteInfo($"AI Labels: {(useAiLabels ? "Enabled" : "Disabled")}");
WriteInfo($"Dashboard: {(generateDashboard ? "Enabled" : "Disabled")}");

// -------------------
// Create JobDataConverter
// -------------------
var converter = new JobDataConverter();

// -------------------
// Fetch jobs from SerpAPI
// -------------------
if (fetchMode)
{
    WriteInfo($"🌐 Fetching jobs: query='{query}', location='{location}'...");
    await FetchJobsFromSerpApi(query, location, serpApiKey, dataDir);
}

// -------------------
// Run batch conversion
// -------------------
if (batchMode)
{
    if (useAiLabels && !batchMode)
        WriteWarning("⚠️ Warning: --ai-labels is usually used with --batch. Proceeding anyway...");

    WriteInfo("📂 Running batch conversion...");
    await converter.BatchConvertAsync(dataDir, fineTuneDir, useAiLabels);
}

// -------------------
// Merge JSONL files
// -------------------
if (mergeMode)
{
    WriteInfo("📦 Merging JSONL files...");
    await converter.MergeJsonlFilesAsync(
        fineTuneDir,
        Path.Combine(fineTuneDir, "all_jobs.jsonl"),
        generateDashboard
    );
}

// -------------------
// CLI Help
// -------------------
void ShowHelp()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(@"
Job Dataset Converter v1.0

Usage:
  SerpAPIBot run [options]

Options:
  --fetch           : Fetch jobs from Google Jobs via SerpAPI
  --query=TITLE     : Job title or keyword for SerpAPI fetch (default from config.txt)
  --location=LOC    : Location for SerpAPI fetch (default from config.txt)
  --batch           : Process all JSON files in data/ and convert to JSONL
  --merge           : Merge all JSONL files into a single file (all_jobs.jsonl)
  --ai-labels       : Enrich each job with AI-assisted labels (requires OPENAI_API_KEY in config.txt)
  --dashboard       : Generate metadata.json and interactive dashboard.html
  --help, -h        : Show this help message
");
    Console.ResetColor();
}

// -------------------
// Colored console helpers
// -------------------
void WriteInfo(string message)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(message);
    Console.ResetColor();
}

void WriteWarning(string message)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(message);
    Console.ResetColor();
}

void WriteError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ResetColor();
}

// -------------------
// SerpAPI fetch helper
// -------------------
async Task FetchJobsFromSerpApi(string query, string location, string apiKey, string dataDir)
{
    string url = $"https://serpapi.com/search.json?engine=google_jobs&q={Uri.EscapeDataString(query)}&location={Uri.EscapeDataString(location)}&api_key={apiKey}";

    using var client = new HttpClient();
    string response;

    try
    {
        response = await client.GetStringAsync(url);
    }
    catch (Exception ex)
    {
        WriteError($"❌ Failed to fetch jobs: {ex.Message}");
        return;
    }

    Directory.CreateDirectory(dataDir);
    string filePath = Path.Combine(dataDir, $"google_jobs_{query.Replace(" ", "_")}_{location.Replace(" ", "_")}.json");
    await File.WriteAllTextAsync(filePath, response);

    WriteInfo($"✅ Saved raw job data → {filePath}");
}
