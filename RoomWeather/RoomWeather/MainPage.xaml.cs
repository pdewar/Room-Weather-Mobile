using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Xamarin.Forms;

namespace RoomWeather
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            initGaugeRimColors();
            Start();
        }

        public void initGaugeRimColors()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                rimcolor1.RimColor = Color.FromRgba(0, 0, 0, 0.42);
                rimcolor2.RimColor = Color.FromRgba(0, 0, 0, 0.42);
                rimcolor3.RimColor = Color.FromRgba(0, 0, 0, 0.42);
            });
        }


        IMqttClient client = new MqttFactory().CreateMqttClient();

        public void Start()
        {
            var options = new MqttClientOptionsBuilder()
                                .WithClientId(Guid.NewGuid().ToString())
                                .WithTcpServer("test.mosquitto.org", 1883)
                                .WithCleanSession()
                                .Build();

            var reconnectTask = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (!client.IsConnected)
                        {
                            await client.ConnectAsync(options, CancellationToken.None);
                            Console.WriteLine("The MQTT client is connected.");
                        }
                    }
                    catch
                    {
                        // Handle the exception properly (logging etc.).
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        Console.WriteLine("Every 5 seconds perform a reconnect if required");
                    }
                }
            });

            client.ConnectedAsync += async e =>
            {
                Console.WriteLine("Connected to the broker successfully");
                var topicfilter = new MqttTopicFilterBuilder()
                                        .WithTopic("csc113/serialdata/student15/publish")
                                        .Build();
                await client.SubscribeAsync(topicfilter);
            };

            client.ApplicationMessageReceivedAsync += e =>
            {
                StringBuilder str = new StringBuilder();
                StringBuilder str1 = new StringBuilder();
                StringBuilder str2 = new StringBuilder();

                string jsonString = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                JsonNode forecastNode = JsonNode.Parse(jsonString)!;

                JsonNode fahrenheit = forecastNode!["Fahrenheit"]!;
                Console.WriteLine($"Fahrenheit={Convert.ToInt32(fahrenheit[0].ToString())}");
                str.AppendLine(fahrenheit[0].ToString());

                JsonNode celcius = forecastNode!["Celcius"]!;
                Console.WriteLine($"Celcius={Convert.ToInt32(celcius[0].ToString())}");
                str1.AppendLine(celcius[0].ToString());

                JsonNode humidity = forecastNode!["Humidity"]!;
                Console.WriteLine($"Humidity={Convert.ToInt32(humidity[0].ToString())}");
                str2.AppendLine(humidity[0].ToString());

                DisplayMeasurementValues(str.ToString(), str1.ToString(), str2.ToString());

                visualTest(Convert.ToInt32(fahrenheit[0].ToString()),
                           Convert.ToInt32(celcius[0].ToString()),
                           Convert.ToInt32(humidity[0].ToString()));

                return Task.CompletedTask;
            };

            client.DisconnectedAsync += e =>
            {
                Console.WriteLine("The MQTT client is disconnected.");
                return Task.CompletedTask;
            };

        }

        void DisplayMeasurementValues(string msg, string msg1, string msg2)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                HeaderMesaurementTemperatureFahenheit.Text = msg;
                HeaderMesaurementTemperatureCelcius.Text = msg1;
                HeaderMesaurementHumidity.Text = msg2;
            });
        }

        void visualTest(int inFahrenheit = 0, int inCelcius = 0, int inHumidity = 0)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (inFahrenheit < 68)
                {
                    range1.Color = Color.FromHex("#1e88e5");
                }
                else if (inFahrenheit >= 68 && inFahrenheit <= 77)
                {
                    range1.Color = Color.FromHex("#43a047");
                }
                else if (inFahrenheit > 77)
                {
                    range1.Color = Color.FromHex("#e53935");
                }

                if (inCelcius < 20)
                {
                    range2.Color = Color.FromHex("#1e88e5");
                }
                else if (inCelcius >= 20 && inCelcius <= 25)
                {
                    range2.Color = Color.FromHex("#43a047");
                }
                else if (inCelcius > 25)
                {
                    range2.Color = Color.FromHex("#e53935");
                }

                if (inHumidity < 40)
                {
                    range3.Color = Color.FromHex("#fb8c00");
                }
                else if (inHumidity >= 40 && inHumidity <= 60)
                {
                    range3.Color = Color.FromHex("#43a047");
                }
                else if (inHumidity > 60)
                {
                    range3.Color = Color.FromHex("#1e88e5");
                }

                range1.Value = inFahrenheit;
                needle1.Value = inFahrenheit;

                range2.Value = inCelcius;
                needle2.Value = inCelcius;

                range3.Value = inHumidity;
                needle3.Value = inHumidity;
            });

        }

        async void PublishBtn_Clicked(object sender, EventArgs e)
        {
            var message = "Test:Activated";
            var messageSend = new MqttApplicationMessageBuilder()
                              .WithTopic("csc113/serialdata/student15/subscribe/test")
                              .WithPayload(Encoding.UTF8.GetBytes(message))
                              .Build();

            await client.PublishAsync(messageSend);
        }

        async void onDragCompleted(object sender, EventArgs e)
        {
            if (sender == TemperatureThresholdSlider)
            {
                var val = Math.Round(TemperatureThresholdSlider.Value);
                var messageSend = new MqttApplicationMessageBuilder()
                               .WithTopic("csc113/serialdata/student15/subscribe/temp")
                               .WithPayload(Encoding.UTF8.GetBytes(val.ToString()))
                               .Build();

                await client.PublishAsync(messageSend);
            }
            else if (sender == HumidityThresholdSlider)
            {
                var val = Math.Round(HumidityThresholdSlider.Value);
                var messageSend = new MqttApplicationMessageBuilder()
                               .WithTopic("csc113/serialdata/student15/subscribe/humid")
                               .WithPayload(Encoding.UTF8.GetBytes(val.ToString()))
                               .Build();

                await client.PublishAsync(messageSend);
            }
        }

        void OnSliderValueChanged(object sender, ValueChangedEventArgs args)
        {

            if (sender == TemperatureThresholdSlider)
            {
                var val = Math.Round(args.NewValue);
                updateTemperatureThresholdLabels(val.ToString());
            }
            else if (sender == HumidityThresholdSlider)
            {
                var val = Math.Round(args.NewValue);
                updateHumidityThresholdLabels(val.ToString());
            }

        }

        void updateHumidityThresholdLabels(string in2)
        {
            HumidityThresholdLabel.Text = in2;
        }

        void updateTemperatureThresholdLabels(string in1)
        {
            TemperatureThresholdLabel.Text = in1;
        }

        void OnToggled(object sender, ToggledEventArgs e)
        {
            if (sender == switchAlarm)
            {
                if (e.Value == true)
                {
                    ALARMALERT("Activated");
                }
                else
                {
                    ALARMALERT("Deactivated");
                }
            }
            else
            {
                if (e.Value == true)
                {
                    LEDALERT("Activated");
                }
                else
                {
                    LEDALERT("Deactivated");
                }
            }

        }

        async void LEDALERT(string invalue)
        {
            var messageSend = new MqttApplicationMessageBuilder()
                           .WithTopic("csc113/serialdata/student15/subscribe/led")
                           .WithPayload(Encoding.UTF8.GetBytes($"LED:{invalue}"))
                           .Build();

            await client.PublishAsync(messageSend);
        }

        async void ALARMALERT(string invalue)
        {
            var messageSend = new MqttApplicationMessageBuilder()
                           .WithTopic("csc113/serialdata/student15/subscribe/alert")
                           .WithPayload(Encoding.UTF8.GetBytes($"Alarm:{invalue}"))
                           .Build();

            await client.PublishAsync(messageSend);
        }
    }
}


