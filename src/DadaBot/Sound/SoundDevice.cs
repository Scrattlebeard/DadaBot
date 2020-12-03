using NAudio.CoreAudioApi;
using System;
using System.Diagnostics;
using System.Linq;

namespace DadaBot.Sound
{
    public class SoundDevice
    {
        public string? Id { get; set; }
        public string Name { get; set; }
        public MMDevice Device { get; set; }

        public SoundDevice(string name, string? id = null)
        {            
            Name = name;
            Id = id;

            using (var devices = new MMDeviceEnumerator())
            {
                try
                {
                    if (id != null)
                    {
                        InitDeviceById(devices);
                    }

                    if(Device == null)
                    {
                        InitDeviceByName(devices);
                    }
                }
                catch (System.Runtime.InteropServices.COMException e) when (e.Message.Contains("0x8889000A")) // 0x8889000A = AUDCLNT_E_DEVICE_IN_USE
                {
                    Trace.WriteLine($"Could not get device with id {Id} since it's marked as being in use. Please try disabling exclusive application access to the device.");
                    throw;
                }
            }

            if (Device == null)
            {
                Trace.WriteLine($"Could not find digital audio output, terminating...");
                throw new Exception($"Could not find audio input device");
            }
        }

        private void InitDeviceById(MMDeviceEnumerator devices)
        {
            try
            {
                Device = devices.GetDevice(Id);
            }
            catch (System.Runtime.InteropServices.COMException e) when (e.Message.ToLower().Contains("not found"))
            {
                Trace.WriteLine($"Could not find device with id {Id}, searching by name {Name} instead...");
            }
        }

        private void InitDeviceByName(MMDeviceEnumerator devices)
        {
            Device = devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).FirstOrDefault(dev => dev.FriendlyName.Contains(Name));
        }

        public static void DetectDevices()
        {
            using var devices = new MMDeviceEnumerator();
            foreach (var device in devices.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
            {
                try
                {
                    Trace.WriteLine($"Found {device.DeviceFriendlyName} with id {device.ID} of type {device.DataFlow}. It is currently {device.State}");
                }
                catch(Exception e)
                {
                    Trace.WriteLine($"Error getting data from device {device.ID}: {e}");
                }
            }
        }
    }
}
