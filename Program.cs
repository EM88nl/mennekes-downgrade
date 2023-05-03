using MennekesControl;
using MQTTnet;
using MQTTnet.Client;
using Serilog;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

class Program
{
    static System.Timers.Timer timer = new System.Timers.Timer(600000);
    static List<uint> measurements = new List<uint>();
    static bool downgradeActive = false;
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        Log.Information("Starting up the application");

        timer.Elapsed += OnTimerElapsed!;
        timer.Start();

        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            string server = "192.168.1.22";
            var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(server).Build();

            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                try
                {
                    DsmrTelegram dsmrTelegram = new DsmrTelegram(e.ApplicationMessage.ConvertPayloadToString());
                    measurements.Add(dsmrTelegram.Overproduction);
                }
                catch (FormatException ex)
                {
                    Log.Error(ex.Message);
                }

                return Task.CompletedTask;
            };

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            Log.Information(string.Format("Connected to server: {0}", server));

            string topic = "dsmr/json";
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(topic);
                    })
                .Build();
            await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
            Log.Information(string.Format("Subscribed to topic: {0}", topic));

            while (true) { }
        }
    }

    static void SetDowngrade(double overproduction)
    {
        if (overproduction >= 1000)
        {
            if (downgradeActive)
            {
                Log.Information("Downgrade deactivated");
            }
            downgradeActive = false;
        }
        else
        {
            if (!downgradeActive)
            {

                Log.Information("Downgrade activated");
            }
            downgradeActive = true;
        }
    }

    static void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (measurements.Count > 0)
        {
            double total = 0;
            foreach (var m in measurements)
            {
                total += m;
            }
            double average = total / measurements.Count;
            Log.Information(string.Format("Average overproduction: {0:F0} Watt ({1} measurements received)", average, measurements.Count));
            SetDowngrade(average);
        }
        else
        {
            Log.Warning("No measurements reveived");
        }
        measurements.Clear();
    }
}