using Azure;
using Azure.Core.Pipeline;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Azure.Messaging.EventGrid;
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
        private static string adtTenantId = Environment.GetEnvironmentVariable("ADT_TENANT_ID");

        enum InputMessageFormat {
            OPC_UA, NerveGateway
        }

        private static InputMessageFormat currentMsgFormat = 
            Environment.GetEnvironmentVariable("InputMessageFormat") != null ? 
                Enum.Parse<InputMessageFormat>(Environment.GetEnvironmentVariable("InputMessageFormat")) : InputMessageFormat.OPC_UA;

        private static List<string> tagNames = new List<string>() {
            "ActualAxisPosition_A1", "ActualAxisPosition_A2", "ActualAxisPosition_A3",
            "ActualAxisPosition_A4", "ActualAxisPosition_A5", "ActualAxisPosition_A6",
            "ActualKarthPositon_X", "ActualKarthPositon_Y", "ActualKarthPositon_Z",
            "ActualKarthPositon_A", "ActualKarthPositon_B", "ActualKarthPositon_C"};

        private static List<string> tagNamesNerveGW = new List<string>() {
            "MachineError", "MachinePause", "MachineStarted",
            "MeasuredDiameter", "MeasuredHoleDiameter", "MeasuredLength",
            "SerialNumber", "SpindlePower"};

        [FunctionName("ProcessHubToDTEvents")]
        public void Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            // After this is deployed, you need to turn the Managed Identity Status to "On",
            // Grab Object Id of the function and assigned "Azure Digital Twins Owner (Preview)" role
            // to this function identity in order for this function to be authorized on ADT APIs.


            if (eventGridEvent != null && eventGridEvent.Data != null)
            {
                log.LogInformation(eventGridEvent.Data.ToString());

                if (currentMsgFormat == InputMessageFormat.OPC_UA)
                    this.doProcessOPCUAMessages(eventGridEvent.Data.ToString(), log);
                else if (currentMsgFormat == InputMessageFormat.NerveGateway)
                    this.doProcessNerveGWMessages(eventGridEvent.Data.ToString(), log);
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
                    DigitalTwinsClient client = CreateADTClient(log);

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

        private DigitalTwinsClient CreateADTClient(ILogger log)
        {
            //Authenticate with Digital Twins
            //var credentials = new DefaultAzureCredential();
            var credentials = new ChainedTokenCredential(
                        new EnvironmentCredential(),
                        new ManagedIdentityCredential(),
                        new AzureCliCredential(new AzureCliCredentialOptions() { TenantId = adtTenantId }));

            DigitalTwinsClient client = new DigitalTwinsClient(new Uri(adtServiceUrl),
                credentials, new DigitalTwinsClientOptions { Transport = new HttpClientTransport(httpClient) });
            log.LogInformation($"ADT service client connection created.");

            return client;
        }

        private void doProcessNerveGWMessages(string msgArray, ILogger log)
        {
            if (msgArray != null && msgArray.Length > 0)
            {
                JObject fullmsg = JObject.Parse(msgArray);
                JToken bodyToken = fullmsg["body"];

                if (bodyToken != null) {
                    JToken payloadToken = null;

                    try { 
                    if (bodyToken.Type == JTokenType.String) {
                        // Looks like base64 decoded
                        // log.LogError($"encoded: {bodyToken.Value<string>()}");
                        byte[] data = Convert.FromBase64String(bodyToken.Value<string>());
                        string decodedString = Encoding.UTF8.GetString(data);
                        // log.LogError($"decoded: {decodedString}");
                        payloadToken = (JsonConvert.DeserializeObject<JObject>(decodedString));
                    } else
                        payloadToken = fullmsg["body"];
                    } catch (Exception ex)
                    {
                        log.LogError($"Error parsing msg '${bodyToken.Value<string>()}' : ${ex.Message}");
                    }

                    NerveGWEventPayload payload = payloadToken?.ToObject<NerveGWEventPayload>();
                    
                    if (payload != null)
                    {
                        DigitalTwinsClient client = CreateADTClient(log);

                        doUpdateTwinPropertyWithValue(client, payload.Variables.MachineError.ToString(), "MachineError", log);
                        doUpdateTwinPropertyWithValue(client, payload.Variables.MachinePause.ToString(), "MachinePause", log);
                        doUpdateTwinPropertyWithValue(client, payload.Variables.MachineStarted.ToString(), "MachineStarted", log);
                        doUpdateTwinPropertyWithValue(client, payload.Variables.MeasuredDiameter.ToString(), "MeasuredDiameter", log);
                        doUpdateTwinPropertyWithValue(client, payload.Variables.MeasuredHoleDiameter.ToString(), "MeasuredHoleDiameter", log);
                        doUpdateTwinPropertyWithValue(client, payload.Variables.MeasuredLength.ToString(), "MeasuredLength", log);
                        doUpdateTwinPropertyWithValue(client, payload.Variables.SerialNumber.ToString(), "SerialNumber", log);
                        doUpdateTwinPropertyWithValue(client, payload.Variables.SpindlePower.ToString(), "SpindlePower", log);
                    } else {
                        if (payloadToken == null)
                            log.LogWarning("Could not read payload token from body");
                        else if (payload == null)
                            log.LogWarning($"Could not ready payload object from token '${payloadToken.Value<string>()}'");
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

                doUpdateTwinPropertyWithValue(client, value, propName, log);
            }
        }

        private void doUpdateTwinPropertyWithValue(DigitalTwinsClient client, string value, string propName, ILogger log)
        {
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

    public class NerveGWEvent {

        public string Origin { get; set; }
        public string Module { get; set; }
        public string Interface { get; set; }
        public string component { get; set; }
        public string Payload { get; set; }
    }

    public class NerveGWEventPayload {
        public NerveGWVariables Variables { get; set; }
    }

    public class NerveGWVariables {
        public bool MachineError {get; set;}
        public bool MachinePause { get; set; }
        public bool MachineStarted { get; set; }
        public double MeasuredDiameter { get; set; }
        public double MeasuredHoleDiameter { get; set; }
        public double MeasuredLength { get; set; }
        public long SerialNumber { get; set; }
        public long SpindlePower { get; set; }
    }
}