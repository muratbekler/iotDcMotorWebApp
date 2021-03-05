using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.System.Threading;
using System.Diagnostics;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.Foundation;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace DcMotorApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            taskInstance.Canceled += OnCanceled;
            deferral = taskInstance.GetDeferral();

            stopwatch = Stopwatch.StartNew();
            InitGPIO();
            InitAppService();
        }

        private async void InitAppService()
        {
            var listing = await AppServiceCatalog.FindAppServiceProvidersAsync("AppComService");

            var packageName = "";
            // there may be cases where other applications could expose the same App Service Name, in our case
            // we only have the one
            if (listing.Count == 1)
            {
                packageName = listing[0].PackageFamilyName;
            }
            // Initialize the AppServiceConnection
            appServiceConnection = new AppServiceConnection();
            appServiceConnection.PackageFamilyName = packageName;
            appServiceConnection.AppServiceName = "AppComService";

            // Send a initialize request 
            var res = await appServiceConnection.OpenAsync();
            if (res == AppServiceConnectionStatus.Success)
            {
                var message = new ValueSet();
                message.Add("Command", "Initialize");
                var response = await appServiceConnection.SendMessageAsync(message);
                if (response.Status != AppServiceResponseStatus.Success)
                {
                    throw new Exception("Failed to send message");
                }
                appServiceConnection.RequestReceived += OnMessageReceived;
            }
        }

        private async void OnMessageReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string newState = message["State"] as string;
            string ipadress = message["Adress"] as string;

            switch (newState)
            {
                case "On":
                    {
                        isMotorRunning = true;
                        await ThreadPool.RunAsync(this.MotorThread, WorkItemPriority.High);
                        break;
                    }
                case "Off":
                    {
                        isMotorRunning = false;
                        InitMotor();
                        break;
                    }
                case "Unspecified":
                default:
                    {
                        InitMotor();
                        break;
                    }
            }
        }

        private void InitMotor()
        {
            pin1.Write(GpioPinValue.Low);
            pin2.Write(GpioPinValue.Low);
            pin3.Write(GpioPinValue.Low);
            pin4.Write(GpioPinValue.Low);
        }

        private void MotorThread(IAsyncAction action)
        {
            //This motor thread runs on a high priority task and loops forever to pulse the motor
            while (isMotorRunning)
            {
                switch (iCounter)
                {
                    // 8 stats stepping
                    case 0:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.High);
                        break;
                    case 1:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.High);
                        pin4.Write(GpioPinValue.High);
                        break;
                    case 2:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.High);
                        pin4.Write(GpioPinValue.Low);
                        break;
                    case 3:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.High);
                        pin3.Write(GpioPinValue.High);
                        pin4.Write(GpioPinValue.Low);
                        break;
                    case 4:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.High);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.Low);
                        break;
                    case 5:
                        pin1.Write(GpioPinValue.High);
                        pin2.Write(GpioPinValue.High);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.Low);
                        break;
                    case 6:
                        pin1.Write(GpioPinValue.High);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.Low);
                        break;
                    case 7:
                        pin1.Write(GpioPinValue.High);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.High);
                        break;
                    default:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.Low);
                        break;
                }
                if (iCounter == 7) iCounter = 0;
                else iCounter++;
                //Use the wait helper method to wait for the length of the pulse
                Wait(1);
            }
        }

        //A synchronous wait is used to avoid yielding the thread 
        //This method calculates the number of CPU ticks will elapse in the specified time and spins
        //in a loop until that threshold is hit. This allows for very precise timing.
        private void Wait(double milliseconds)
        {
            long initialTick = stopwatch.ElapsedTicks;
            long initialElapsed = stopwatch.ElapsedMilliseconds;
            double desiredTicks = milliseconds / 1000.0 * Stopwatch.Frequency;
            double finalTick = initialTick + desiredTicks;
            while (stopwatch.ElapsedTicks < finalTick)
            {

            }
        }

        private void InitGPIO()
        {
            controller = GpioController.GetDefault();
            if (controller == null)
            {
                pin1 = pin2 = pin3 = pin4 = null;
                return;
            }

            pin1 = GpioController.GetDefault().OpenPin(LED_PIN1);
            pin1.Write(GpioPinValue.Low);
            pin1.SetDriveMode(GpioPinDriveMode.Output);
            pin2 = GpioController.GetDefault().OpenPin(LED_PIN2);
            pin2.Write(GpioPinValue.Low);
            pin2.SetDriveMode(GpioPinDriveMode.Output);
            pin3 = GpioController.GetDefault().OpenPin(LED_PIN3);
            pin3.Write(GpioPinValue.Low);
            pin3.SetDriveMode(GpioPinDriveMode.Output);
            pin4 = GpioController.GetDefault().OpenPin(LED_PIN4);
            pin4.Write(GpioPinValue.Low);
            pin4.SetDriveMode(GpioPinDriveMode.Output);
        }

        BackgroundTaskDeferral deferral;
        GpioController controller;
        ThreadPoolTimer timer;
        Stopwatch stopwatch;
        AppServiceConnection appServiceConnection;

        private const int LED_PIN1 = 18;
        private const int LED_PIN2 = 23;
        private const int LED_PIN3 = 24;
        private const int LED_PIN4 = 25;
        private GpioPin pin1, pin2, pin3, pin4;
        private int iCounter = 0;
        private bool isMotorRunning = false;
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            throw new NotImplementedException();
        }
    }
}
