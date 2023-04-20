using Newtonsoft.Json;

namespace MennekesControl
{
    internal class DsmrTelegram
    {
        public uint Overproduction { get; set; }

        public DsmrTelegram(string json)
        {
            DsmrTelegramData? data = JsonConvert.DeserializeObject<DsmrTelegramData>(json);

            if (data == null)
            {
                throw new ArgumentNullException("Failed to deserialize JSON string");
            }

            if (!uint.TryParse(data.electricity_currently_delivered?.Replace(".", ""), out uint delivered))
            {
                throw new FormatException("Failed to parse electricity_currently_delivered data");
            }

            if (!uint.TryParse(data.electricity_currently_returned?.Replace(".", ""), out uint returned))
            {
                throw new FormatException("Failed to parse electricity_currently_delivered data");
            }

            if (returned > delivered)
            {
                Overproduction = returned - delivered;
            }
        }
    }
}