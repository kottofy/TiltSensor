using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;

static class AzureIoTHub
{
    public static async Task SendDeviceToCloudMessageAsync()
    {
        //get the event hub connection string from local file
        string deviceConnectionString = local_settings.getDeviceConnectionString();
        Debug.WriteLine("Device connection string: " + deviceConnectionString);

        //create the Device Client object with the connection string
        var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Amqp);

        //do some fancy JSON stuff to convert the message to a JSON
        Dictionary<string, string> json_message = new Dictionary<string, string>();
        json_message.Add("alert", "Alert occured");
        string str = JsonConvert.SerializeObject(json_message);

        //this is the message to send to event hub in JSON form
        var message = new Message(Encoding.ASCII.GetBytes(str));

        try
        {
            //send the message
            await deviceClient.SendEventAsync(message);
        }
        catch (Exception e)
        {
            //print error
            Debug.WriteLine("Error:" + e);
        }
    }
}
