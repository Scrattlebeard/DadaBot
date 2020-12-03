using DadaBot.Configuration;
using DadaBot.Sound;
using DSharpPlus.VoiceNext;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NLog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DadaBot
{
    public interface ISoundPlayer
    {
        void Start(VoiceNextConnection voiceStream);
        void Stop();
    }

    public class SoundPlayer : ISoundPlayer, IDisposable
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private bool _disposing = false;

        private readonly SoundSettings _soundSettings;
        private readonly MMDevice _inputDevice;
        private readonly WasapiCapture _capture;
        private readonly WaveInProvider _captureProvider;
        
        private readonly BufferedWaveProvider _remoteBuffer;
        private readonly IWaveProvider _remoteOutput;

        private readonly BufferedWaveProvider? _localBuffer;
        private WasapiOut? _localOutput;        

        private readonly byte[] buf = new byte[176400];
        private byte[]? streamBuffer;

        private bool _playing = false;

        public SoundPlayer(SoundDevice device, SoundSettings settings)
        {            
            _soundSettings = settings;
            _inputDevice = device.Device;

            if (_inputDevice.DataFlow == DataFlow.Render)
            {
                _capture = new WasapiLoopbackCapture(_inputDevice) { ShareMode = AudioClientShareMode.Shared };                    

                _log.Debug($"Initialized WasapiLoopbackCapture for device {_inputDevice.FriendlyName} of type {_inputDevice.DataFlow}. ShareMode: {_capture.ShareMode}, State: {_inputDevice.State}");

                _log.Debug($"Capturing in format: {_capture.WaveFormat} {_capture.WaveFormat.BitsPerSample}bit {_capture.WaveFormat.SampleRate}Hz {_capture.WaveFormat.Channels} channels");

                _captureProvider = new WaveInProvider(_capture);

                _remoteOutput = new DefaultResampler(_capture.WaveFormat).Resample(_captureProvider);
                
                _remoteBuffer = new BufferedWaveProvider(SoundSettings.DiscordFormat);
                _capture.DataAvailable += (s, e) => { _remoteOutput.Read(buf, 0, e.BytesRecorded / 2); _remoteBuffer.AddSamples(buf, 0, e.BytesRecorded / 2); };
            }
            else
            {
                _capture = new WasapiCapture(_inputDevice) { ShareMode = AudioClientShareMode.Shared };

                _log.Debug($"Initialized WasapiCapture for device {_inputDevice.FriendlyName} of type {_inputDevice.DataFlow}. ShareMode: {_capture.ShareMode}, State: {_inputDevice.State}.");

                _log.Debug($"Capturing in format: {_capture.WaveFormat} {_capture.WaveFormat.BitsPerSample}bit {_capture.WaveFormat.SampleRate}Hz {_capture.WaveFormat.Channels} channels");

                _captureProvider = new WaveInProvider(_capture);
                
                _remoteOutput = new DefaultResampler(_capture.WaveFormat).Resample(_captureProvider);

                var captureRate = _capture.WaveFormat.AverageBytesPerSecond;
                var outputRate = SoundSettings.DiscordFormat.AverageBytesPerSecond;

                _log.Info($"Capture rate is {captureRate}, output rate is {outputRate}, that gives a ratio of {(float)captureRate / (float)outputRate}");
                _log.Debug($"Outputting in format: {_remoteOutput.WaveFormat} {_remoteOutput.WaveFormat.BitsPerSample}bit {_remoteOutput.WaveFormat.SampleRate}Hz {_remoteOutput.WaveFormat.Channels} channels");

                var rate = (float) SoundSettings.DiscordFormat.AverageBytesPerSecond / _capture.WaveFormat.AverageBytesPerSecond;

                _remoteBuffer = new BufferedWaveProvider(_remoteOutput.WaveFormat);
                _capture.DataAvailable += (s, e) => { _remoteOutput.Read(buf, 0, (int) Math.Round(e.BytesRecorded * rate)); _remoteBuffer.AddSamples(buf, 0, (int)Math.Round(e.BytesRecorded * rate)); };
            }            
            
            if(_soundSettings.PlayLocally)
            {
                _localOutput = new WasapiOut();
                _localBuffer = new BufferedWaveProvider(_remoteOutput.WaveFormat);                                              

                _localOutput.Init(_localBuffer);
                _capture.DataAvailable += (s, e) => { _localBuffer.AddSamples(buf, 0, e.BytesRecorded/2); };                
            }            
        }

        public void Start(VoiceNextConnection voiceStream)
        {                        
            try
            {
                _capture.StartRecording();
                _localOutput?.Play();

                _playing = true;

                Task.Run(async () => await SendToStream(voiceStream));
            }
            catch(Exception e)
            {
                _log.Error(e, $"An error occured in the stream task: {e.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            _playing = false;

            _capture.StopRecording();
            _localOutput?.Stop();
            _remoteBuffer.ClearBuffer();
        }

        public async Task SendToStream(VoiceNextConnection voiceStream)
        {
            try
            {                
                var sink = voiceStream.GetTransmitSink(_soundSettings.SampleSize);
                var bytesPerSample = _remoteBuffer.BytesPerSample(sink.SampleDuration);
                streamBuffer = new byte[bytesPerSample];

                await Task.Delay(_soundSettings.BufferDurationMs);

                while (_playing)
                {
                    if (_remoteBuffer.BufferedBytes >= bytesPerSample)
                    {
                        _remoteBuffer.Read(streamBuffer, 0, bytesPerSample);
                        await sink.WriteAsync(streamBuffer, 0, bytesPerSample);
                    }
                }
            }
            catch(Exception e)
            {
                _log.Error(e, $"An error occured during transmission to stream: {e.Message}");
                Stop();
            }               
        }

        public void Dispose()
        {
            if (!_disposing)
            {
                _disposing = true;
                
                _capture?.Dispose();                                
                _inputDevice?.Dispose();
                                
                _localOutput?.Dispose();
                _localOutput = null;
            }
        }        
    }
}
