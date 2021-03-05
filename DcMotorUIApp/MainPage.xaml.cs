using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.ApplicationModel.AppService;
using Windows.Devices.Gpio;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DcMotorUIApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            InitGPIO();
            InitAppSvc();
        }
        AppServiceConnection appServiceConnection;
        private int isMotorRunning = 0;
        private const int LED_PIN = 8;
        private GpioPin pin;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);
        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin = null;
                return;
            }

            pin = gpio.OpenPin(LED_PIN);
            pin.Write(GpioPinValue.Low);
            pin.SetDriveMode(GpioPinDriveMode.Output);
        }

        private async void InitAppSvc()
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
            string ipadress = message["adress"] as string;
            switch (newState)
            {
                case "On":
                    {
                        await Dispatcher.RunAsync(
                              CoreDispatcherPriority.High,
                             () =>
                             {
                                 TurnOnDcMotor();
                             });
                        break;
                    }
                case "Off":
                    {
                        await Dispatcher.RunAsync(
                        CoreDispatcherPriority.High,
                        () =>
                        {
                            TurnOffDcMotor();
                        });
                        break;
                    }
                case "Unspecified":
                default:
                    {
                        // Do nothing 
                        break;
                    }
            }
        }

        private void TurnOffDcMotor()
        {
            if (isMotorRunning == 1)
            {
                FlipGpio();
            }
        }

        private void TurnOnDcMotor()
        {
            if (isMotorRunning == 0)
            {
                FlipGpio();
            }
        }

        private void FlipGpio()
        {
            if (isMotorRunning == 0)
            {
                isMotorRunning = 1;
                if (pin != null)
                {
                    // to turn on the LED, we need to push the pin 'low'
                    pin.Write(GpioPinValue.High);
                }
            }
            else
            {
                isMotorRunning = 0;
                if (pin != null)
                {
                    pin.Write(GpioPinValue.Low);
                }
            }
        }
    }
}
