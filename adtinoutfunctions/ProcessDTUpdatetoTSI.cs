using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using Azure.Messaging.EventHubs;

namespace SampleFunctionsApp
{
    public static class ProcessDTUpdatetoTSI
    {
        private static List<string> twins = new List<string>() {
            "ActualAxisPosition_A1", "ActualAxisPosition_A2", "ActualAxisPosition_A3",
            "ActualAxisPosition_A4", "ActualAxisPosition_A5", "ActualAxisPosition_A6",
            "ActualKarthPositon_X", "ActualKarthPositon_Y", "ActualKarthPositon_Z",
            "ActualKarthPositon_A", "ActualKarthPositon_B", "ActualKarthPositon_C"};

        [FunctionName("ProcessDTUpdatetoTSI")]
        public static async Task Run(
            [EventHubTrigger("twins-event-hub", Connection = "EventHubAppSetting-Twins")] EventData myEventHubMessage,
            [EventHub("tsi-event-hub", Connection = "EventHubAppSetting-TSI")] IAsyncCollector<string> outputEvents,
            ILogger log)
        {
            string twinID = myEventHubMessage.Properties["cloudEvents:subject"].ToString();
            if (twins.IndexOf(twinID) != -1)
            {
                JObject message = (JObject)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(myEventHubMessage.EventBody));
                log.LogInformation($"Reading event: {message}");

                // Read values that are replaced or added
                var tsiUpdate = new Dictionary<string, object>();
                foreach (var operation in message["patch"])
                {
                    if (operation["op"].ToString() == "replace" || operation["op"].ToString() == "add")
                    {
                        //Convert from JSON patch path to a flattened property for TSI
                        //Example input: /Front/Temperature
                        //        output: Front.Temperature
                        string path = operation["path"].ToString().Substring(1);
                        path = path.Replace("/", ".");
                        if (path.Equals("value")) {
                            double d1 = double.Parse(operation["value"].ToString(), CultureInfo.InvariantCulture);
                            tsiUpdate.Add(path, d1);
                        }
                    }
                }
                // Send an update if updates exist
                if (tsiUpdate.Count > 0)
                {
                    tsiUpdate.Add("$dtId", twinID);
                    await outputEvents.AddAsync(JsonConvert.SerializeObject(tsiUpdate));
                }
            }
        }
    }
}