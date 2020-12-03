using NAudio.Wave;

namespace DadaBot.Configuration
{
    public class SoundSettings
    {
        public static readonly WaveFormat DiscordFormat = new WaveFormat(48000, 16, 2);

        //public string InputDeviceId { get; set; } = "{0.0.0.00000000}.{41515c9a-8763-4df6-adc0-ff6412e2519e}";
        //public string InputDeviceId { get; set; } = "{0.0.1.00000000}.{20dbf978-26cb-4b33-bd0a-94c6233a5aca}";
        public string? InputDeviceId { get; set; }
        public string InputDeviceName { get; set; } = "Digital Audio (S/PDIF)";
        public bool PlayLocally { get; set; } = false;        
        public int SampleSize { get; set; } = 40; //Valid values are 5, 10, 20, 40 and 60
        public int BufferDurationMs { get; set; } = 1000; //Library only support buffer of up to 5000ms        
    }
}
