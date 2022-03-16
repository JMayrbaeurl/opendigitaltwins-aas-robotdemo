using Azure;
using Azure.Core.Pipeline;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace SampleFunctionsApp
{
    // This class processes telemetry events from IoT Hub, reads temperature of a device
    // and sets the "Temperature" property of the device with the value of the telemetry.
    public class ProcessHubToDTEvents
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string adtServiceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");

        private static List<string> tagNames = new List<string>() {
            "ActualAxisPosition_A1", "ActualAxisPosition_A2", "ActualAxisPosition_A3",
            "ActualAxisPosition_A4", "ActualAxisPosition_A5", "ActualAxisPosition_A6",
            "ActualKarthPositon_X", "ActualKarthPositon_Y", "ActualKarthPositon_Z",
            "ActualKarthPositon_A", "ActualKarthPositon_B", "ActualKarthPositon_C"};

        [FunctionName("ProcessHubToDTEvents")]
        public void Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            // After this is deployed, you need to turn the Managed Identity Status to "On",
            // Grab Object Id of the function and assigned "Azure Digital Twins Owner (Preview)" role
            // to this function identity in order for this function to be authorized on ADT APIs.


            if (eventGridEvent != null && eventGridEvent.Data != null)
            {
                log.LogInformation(eventGridEvent.Data.ToString());

                this.doProcessOPCUAMessages(eventGridEvent.Data.ToString(), log);
            }
        }

        private void doProcessOPCUAMessages(string msgArray, ILogger log)
        {
            if (msgArray != null && msgArray.Length > 0)
            {
                IList<JObject> deviceMessages = null;

                JObject fullmsg = JObject.Parse(msgArray);
                JToken bodyToken = fullmsg["body"];
                
                if (bodyToken != null) {
                    if (bodyToken.Type == JTokenType.String) {
                        // Looks like base64 decoded
                        byte[] data = Convert.FromBase64String(bodyToken.Value<string>());
                        string decodedString = Encoding.UTF8.GetString(data);
                        deviceMessages = JsonConvert.DeserializeObject<List<JObject>>(decodedString);
                    } else {
                        JArray deviceMessagesArray = (JArray)fullmsg["body"];
                        deviceMessages = deviceMessagesArray.ToObject<IList<JObject>>();
                    }
                } else {
                    log.LogInformation($"No body of message.");
                    return;
                }

                if (deviceMessages != null && deviceMessages.Count > 0)
                {
                    //Authenticate with Digital Twins
                    var credentials = new DefaultAzureCredential();
                    DigitalTwinsClient client = new DigitalTwinsClient(new Uri(adtServiceUrl),
                        credentials, new DigitalTwinsClientOptions { Transport = new HttpClientTransport(httpClient) });
                    log.LogInformation($"ADT service client connection created.");

                    foreach (JObject msg in deviceMessages)
                    {
                        string messageId = (string)msg["MessageId"];
                        string messageType = (string)msg["MessageType"];
                        if (messageId != null && messageType != null && messageType.Equals("ua-data"))
                        {
                            log.LogInformation("Processing message with id: " + messageId);
                            foreach (var opcMsg in msg["Messages"])
                            {
                                foreach(string tag in tagNames) {
                                    this.doUpdateTwinProperty(client, opcMsg, tag, log);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void doUpdateTwinProperty(DigitalTwinsClient client, JToken opcMsg, string propName, ILogger log)
        {
            var prop = opcMsg["Payload"][propName];
            if (prop != null)
            {
                string value = prop["Value"].ToString();
                
                //Update twin using device temperature
                var updateTwinData = new JsonPatchDocument();
                updateTwinData.AppendReplace("/value", value);
                try
                {
                    client.UpdateDigitalTwin(propName, updateTwinData);
                    log.LogInformation("Successfully updated the twin " + propName);
                }
                catch (RequestFailedException exc)
                {
                    log.LogError($"*** Error:{exc.Status}/{exc.Message}");
                }
            }
        }

        public async void OriginalRun([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            // After this is deployed, you need to turn the Managed Identity Status to "On",
            // Grab Object Id of the function and assigned "Azure Digital Twins Owner (Preview)" role
            // to this function identity in order for this function to be authorized on ADT APIs.

            //Authenticate with Digital Twins
            var credentials = new DefaultAzureCredential();
            DigitalTwinsClient client = new DigitalTwinsClient(
                new Uri(adtServiceUrl), credentials, new DigitalTwinsClientOptions
                { Transport = new HttpClientTransport(httpClient) });
            log.LogInformation($"ADT service client connection created.");

            if (eventGridEvent != null && eventGridEvent.Data != null)
            {
                log.LogInformation(eventGridEvent.Data.ToString());

                // Reading deviceId and temperature for IoT Hub JSON
                JObject deviceMessage = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
                string deviceId = (string)deviceMessage["systemProperties"]["iothub-connection-device-id"];
                var temperature = deviceMessage["body"]["Temperature"];

                log.LogInformation($"Device:{deviceId} Temperature is:{temperature}");

                //Update twin using device temperature
                var updateTwinData = new JsonPatchDocument();
                updateTwinData.AppendReplace("/Temperature", temperature.Value<double>());
                await client.UpdateDigitalTwinAsync(deviceId, updateTwinData);
            }
        }
    }
}