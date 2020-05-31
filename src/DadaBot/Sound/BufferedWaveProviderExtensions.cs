using NAudio.Wave;

namespace DadaBot.Sound
{    public static class BufferedWaveProviderExtensions
    {
        /// <summary>
        /// Returns the average number of bytes in a sample of the given duration
        /// </summary>
        /// <param name="prov"></param>
        /// <param name="sampleDuration">The sample duration in milliseconds</param>
        /// <returns></returns>
        public static int BytesPerSample(this BufferedWaveProvider prov, int sampleDuration)
        {
            return (prov.WaveFormat.AverageBytesPerSecond / 1000) * sampleDuration;
        }
    }
}
