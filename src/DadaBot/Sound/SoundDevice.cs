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
                catch (System.Runtime.InteropServices.COMException e) when (e.Message.Contains("0x8889000A")) // 0x8889000A = AUDCLNT_E_DEVICE_IN_USE
                {
                    Console.WriteLine($"Could not get device with id {Id} since it's marked as being in use. Please try disabling exclusive application access to the device.");
                    throw;
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
