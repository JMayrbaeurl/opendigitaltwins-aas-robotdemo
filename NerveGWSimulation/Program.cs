// See https://aka.ms/new-console-template for more information
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

Console.WriteLine("Hello, World!");

string deviceKey = "Y4lXfZ2j5Lm5yslQSA34WsgTL3R1pWQ03wUSj8W5USw=";
string deviceId = "NerveGateway";
string iotHubHostName = "iothub32tsq7s6gfcpw.azure-devices.net";
var deviceAuthentication = new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey);

DeviceClient deviceClient = DeviceClient.Create(iotHubHostName, deviceAuthentication, TransportType.Mqtt);


    string messageString = "{ \"event\": {\"origin\": \"nerve-demo-cnc-machine\",\"module\": \"\",\"interface\": \"\",\"component\": \"\"," + 
        "\"payload\": \"{\"variables\":{\"MachineError\":false,\"MachinePause\":false,\"MachineStarted\":true,\"MeasuredDiameter\":22.095077514648438,\"MeasuredHoleDiameter\":12.309970855712891,\"MeasuredLength\":100.41339111328125,\"SerialNumber\":1902705,\"SpindlePower\":50}}";
    var eventMsg = new EventMsg() {
        Origin = "nerve-demo-cnc-machine", Module = "", Interface = "", Component = "",
        Payload = "{\"variables\":{\"MachineError\":false,\"MachinePause\":false,\"MachineStarted\":true,\"MeasuredDiameter\":22.095077514648438,\"MeasuredHoleDiameter\":12.309970855712891,\"MeasuredLength\":100.41339111328125,\"SerialNumber\":1902705,\"SpindlePower\":30}}"
    };
    //messageString = JsonConvert.SerializeObject(new PayloadMsg() { Event = eventMsg});
    messageString = eventMsg.Payload; // Just send payload
    Message message = new Message(Encoding.UTF8.GetBytes(messageString)){
        ContentEncoding = Encoding.UTF8.ToString(),
        ContentType = "application/json"
    };

    await deviceClient.SendEventAsync(message);
    Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

    await Task.Delay(1000);


internal class EventMsg {
    [JsonProperty("origin")]
    public string Origin { get; set; }

    [JsonProperty("module")]
    public string Module { get; set; }

    [JsonProperty("interface")]
    public string Interface { get; set; }
    
    [JsonProperty("component")]
    public string Component { get; set; }
    
    [JsonProperty("payload")]
    public string Payload { get; set; }
}

internal class PayloadMsg {
    [JsonProperty("event")]
    public EventMsg Event {get; set; }
}