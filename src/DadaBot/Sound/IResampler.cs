using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace DadaBot.Sound
{
    public interface IResampler
    {
        IWaveProvider Resample(IWaveProvider input);
    }
}
