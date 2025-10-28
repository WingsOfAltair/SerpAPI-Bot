using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace SerpAPI_Bot
{

    public interface IJobDataConverter
    {
        Task BatchConvertAsync(string dataDirectory, string outputDirectory, bool useAiLabels = false);
        Task MergeJsonlFilesAsync(string inputDirectory, string outputFile, bool generateDashboard = false);
    }

}
