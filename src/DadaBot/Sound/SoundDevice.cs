using DadaBot.Configuration;
using NAudio.CoreAudioApi;
using System;
using System.Linq;

namespace DadaBot.Sound
{
    public class SoundDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public MMDevice Device { get; set; }

        public SoundDevice(string id, string name)
        {
            Id = id;
            Name = name;

            using (var devices = new MMDeviceEnumerator())
            {
                try
                {
                   Device = devices.GetDevice(Id);
                }
                catch (System.Runtime.InteropServices.COMException e) when (e.Message.Contains("Not found"))
                {
                    Console.WriteLine($"Could not find device with id {Id}, searching by name {Name} instead...");

                    Device = devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).FirstOrDefault(dev => dev.FriendlyName.Contains(Name));
                }
            }

            if (Device == null)
            {
                Console.WriteLine($"Could not find digital audio output, terminating...");
                throw new Exception($"Could not find audio input device");
            }
        }
    }
}
