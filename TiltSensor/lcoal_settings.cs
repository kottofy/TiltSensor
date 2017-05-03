/*
 * This is a secret place for the connection string to an IOT device.
 * It is recommended to keep this file and connection string private for security reasons.
 * 
 * 
 * 1. Paste in your device connection string in the ""
 * 2. Save the file 
 * 
 * */


class local_settings
{
    const string deviceConnectionString = "";

    public static string getDeviceConnectionString()
    {
        return deviceConnectionString;
    }
}