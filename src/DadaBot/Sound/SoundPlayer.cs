using DadaBot.Configuration;
using DadaBot.Sound;
using DSharpPlus;
using DSharpPlus.VoiceNext;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
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
        private bool _disposing = false;

        private readonly SoundSettings _soundSettings;
        private readonly DebugLogger _log;
        private readonly MMDevice _inputDevice;
        private readonly WasapiLoopbackCapture _capture;
        private readonly WaveInProvider _captureProvider;
        
        private readonly BufferedWaveProvider _remoteBuffer;
        private readonly WaveFloatTo16Provider _remoteOutput;

        private readonly BufferedWaveProvider? _localBuffer;
        private WasapiOut? _localOutput;        

        private readonly byte[] buf = new byte[64000];
        private byte[]? streamBuffer;

        private bool _playing = false;

        public SoundPlayer(SoundDevice device, SoundSettings settings, DebugLogger log)
        {
            _log = log;
            _soundSettings = settings;
            _inputDevice = device.Device;

            _capture = new WasapiLoopbackCapture(_inputDevice)
            {
                ShareMode = AudioClientShareMode.Shared
            };

            _captureProvider = new WaveInProvider(_capture);
            
            _remoteOutput = new WaveFloatTo16Provider(_captureProvider);
            _remoteBuffer = new BufferedWaveProvider(_remoteOutput.WaveFormat);

            _capture.DataAvailable += (s, e) => { _remoteOutput.Read(buf, 0, e.BytesRecorded/2); _remoteBuffer.AddSamples(buf, 0, e.BytesRecorded/2); };
            
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
            var stream = voiceStream.GetTransmitStream(_soundSettings.SampleSize);

            var bytesPerSample = _remoteBuffer.BytesPerSample(stream.SampleDuration);
            streamBuffer = new byte[bytesPerSample];

            _capture.StartRecording();
            _localOutput?.Play();

            _playing = true;
            Task.Run(async () => await SendToStream(stream, bytesPerSample));
        }

        public void Stop()
        {
            _playing = false;

            _capture.StopRecording();
            _localOutput?.Stop();
        }

        public async Task SendToStream(VoiceTransmitStream stream, int bytesPerSample)
        {
            try
            {
                await Task.Delay(_soundSettings.BufferDurationMs);

                while (_playing)
                {
                    if (_remoteBuffer.BufferedBytes >= bytesPerSample)
                    {
                        _remoteBuffer.Read(streamBuffer, 0, bytesPerSample);
                        stream.Write(streamBuffer, 0, bytesPerSample);
                    }
                }
            }
            finally
            {
                await stream.DisposeAsync();
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
