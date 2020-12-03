using DadaBot.Configuration;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NLog;
using System;

namespace DadaBot.Sound
{
    public class DefaultResampler : IResampler
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        
        private readonly WaveFormat _inFormat;        

        public DefaultResampler (WaveFormat inFormat)
        {
            _inFormat = inFormat;
        }

        public IWaveProvider Resample(IWaveProvider inputProvider)
        {
            var output = inputProvider;

            if(_inFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                //This needs to change if the Discord sound format changes
                output = new WaveFloatTo16Provider(inputProvider);
                _log.Debug("Resampling from IeeeFloat using FloatTo16Provider");
            }
            else if(_inFormat.BitsPerSample != SoundSettings.DiscordFormat.BitsPerSample)
            {             
                output = new WaveFormatConversionProvider(new WaveFormat(_inFormat.SampleRate, SoundSettings.DiscordFormat.BitsPerSample, _inFormat.Channels), output);
                _log.Debug($"Resampling from {_inFormat.BitsPerSample} to {SoundSettings.DiscordFormat.BitsPerSample} bit using FormatConversionProvider");
            }

            if(_inFormat.Channels != SoundSettings.DiscordFormat.Channels)            {
                output = new WaveFormatConversionProvider(new WaveFormat(_inFormat.SampleRate, SoundSettings.DiscordFormat.BitsPerSample, SoundSettings.DiscordFormat.Channels), output);
                _log.Debug($"Resampling from {_inFormat.Channels} to {SoundSettings.DiscordFormat.Channels} using FormatConversionProbider");
            }

            if(_inFormat.SampleRate != SoundSettings.DiscordFormat.SampleRate)
            {
                output = new WaveFormatConversionProvider(SoundSettings.DiscordFormat, output);
                _log.Debug($"Resampling from {_inFormat.SampleRate} to {SoundSettings.DiscordFormat.SampleRate} using FormatConversionProbider");
            }

            return output;
        }
    }
}
