

using System;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.System.Threading;
using System.Diagnostics;


namespace TiltSensor
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;
        private const int TILT_SENSOR_PIN = 4;              // pin of the tilt sensor input
        private const int TILT_LED_PIN = 17;                // blue led = constant check on tilt, if tilt turn on, if no tile turn off
        private const int TILT_CHECK_PIN = 27;              // green led = tilt check led, ex. every 5 seconds turn on to signal checking for tilt
        private const int NOTIFICATION_LED_PIN = 22;        // red led = indicates when notification is sent to event hub
        private GpioPin tilt;                               // tilt sensor input pin
        private GpioPin tilt_led;                           // tilt sensor output led
        private GpioPin tilt_check_led;                     // tilt check output led
        private GpioPin notification_led;                   // send notification output led
        private ThreadPoolTimer tilt_sensor_check_timer;    // timer for tilt sensor input
        private ThreadPoolTimer tilt_sensor_led_timer;      // timer for turning the tilt sensor led on or off
        private ThreadPoolTimer tilt_check_timer;           // timer for turning the tilt check led on and off
        private ThreadPoolTimer sent_notification_timer;    // time for turning the sent notificiation led on and off
        private int tilts = 0;                              // counter for number of tilts collected at each tilt check
        private const int TILT_SENSOR_CHECK_TIME = 5000;    // number of milliseconds to get input from the tilt sensor
        private const int TILT_SENSOR_LED_TIME = 100;       // number of milliseconds to send output to tilt sensor LED
        private const int TILT_CHECK_LED_TIME = 1000;       // number of milliseconds to turn tilt check led on for
        private const int SENT_NOTIFICATION_LED_TIME = 1000;// number of milliseconds to turn LEDs on for notifications
        private const int NUMBER_OF_TILTS_TO_NOTIFY = 2;   // number of tilts to count until notification should be sent


        //initialize all the things
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            InitGPIO();

            // timer to check for when tilting or not
            tilt_sensor_check_timer = ThreadPoolTimer.CreatePeriodicTimer(Tilt_Sensor_Check_Timer_Tick, TimeSpan.FromMilliseconds(TILT_SENSOR_CHECK_TIME));

            // timer for blue led - keeps constant check on tilt
            tilt_sensor_led_timer = ThreadPoolTimer.CreatePeriodicTimer(Tilt_Sensor_LED_Timer_Tick, TimeSpan.FromMilliseconds(TILT_SENSOR_LED_TIME));

        }


        /* Iniitalize all leds to off */
        private void InitGPIO()
        {
            tilt = GpioController.GetDefault().OpenPin(TILT_SENSOR_PIN);
            tilt_led = GpioController.GetDefault().OpenPin(TILT_LED_PIN);
            tilt_check_led = GpioController.GetDefault().OpenPin(TILT_CHECK_PIN);
            notification_led = GpioController.GetDefault().OpenPin(NOTIFICATION_LED_PIN);

            tilt_check_led.Write(GpioPinValue.High);
            tilt_check_led.SetDriveMode(GpioPinDriveMode.Output);

            tilt_led.Write(GpioPinValue.High);
            tilt_led.SetDriveMode(GpioPinDriveMode.Output);

            notification_led.Write(GpioPinValue.High);
            notification_led.SetDriveMode(GpioPinDriveMode.Output);
        }


        /* Turn the tilt sensor led on or off based on input from the tilt sensor */
        private void Tilt_Sensor_LED_Timer_Tick(ThreadPoolTimer timer)
        {
            Debug.WriteLine(tilt.Read());

            // if the sensor is on, turn the LED on, otherwise, turn the LED off
            if (tilt.Read() == GpioPinValue.High)
            {
                tilt_led.Write(GpioPinValue.High);
            }
            else
            {
                tilt_led.Write(GpioPinValue.Low);
            }
        }


        /* Check if 20 tilts have been made at tilt checks, then send message */
        private void Tilt_Sensor_Check_Timer_Tick(ThreadPoolTimer timer)
        {
            turnTiltCheckLEDOnAndOff();

            //Debug.WriteLine("Tilt: " + tilt.Read());

            if (tilt.Read() == GpioPinValue.Low)
            {
                tilts++;
                Debug.WriteLine("tilt #: " + tilts);

                if (tilts >= NUMBER_OF_TILTS_TO_NOTIFY)
                {
                    Debug.WriteLine("Tilts resetting");
                    tilts = 0;

                    turnSentNotificationLEDOnAndOff();

                    sendMessage();
                }
            }
        }



        /* turns sent notification led on and off after designated length of time */
        private void turnSentNotificationLEDOnAndOff()
        {
            notification_led.Write(GpioPinValue.Low);
            sent_notification_timer = ThreadPoolTimer.CreateTimer(Sent_Notification_LED_Timer_Tick, TimeSpan.FromMilliseconds(SENT_NOTIFICATION_LED_TIME));
        }

        /* called by turnSentNotificationLEDOnAndOff to turn off LED when timer is done */
        private void Sent_Notification_LED_Timer_Tick(ThreadPoolTimer timer)
        {
            notification_led.Write(GpioPinValue.High);
        }



        /* turns tilt check led on and off after designated length of time */
        private void turnTiltCheckLEDOnAndOff()
        {
            tilt_check_led.Write(GpioPinValue.Low);
            tilt_check_timer = ThreadPoolTimer.CreateTimer(Tilt_Check_LED_Timer_Tick, TimeSpan.FromMilliseconds(TILT_CHECK_LED_TIME));
        }

        /* called by turnTiltCheckLEDOnAndOff to turn off LED when timer is done */
        private void Tilt_Check_LED_Timer_Tick(ThreadPoolTimer timer)
        {
            tilt_check_led.Write(GpioPinValue.High);
        }



        /* Call the IoTHub send message method */
        private void sendMessage()
        {
            AzureIoTHub.SendDeviceToCloudMessageAsync();
        }
    }
}