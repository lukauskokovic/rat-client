using NAudio.Wave;
using System;
using System.Net;
using System.Net.Sockets;

public static class Microphone 
{
    static WaveInEvent waveInDevice;
    static UdpClient client = new UdpClient();
    public static bool Running = false;
    public static void Init()
    {
        waveInDevice = new WaveInEvent();
        waveInDevice.DeviceNumber = 0;
        waveInDevice.WaveFormat = new WaveFormat(44100, WaveIn.GetCapabilities(waveInDevice.DeviceNumber).Channels);
        waveInDevice.StartRecording();
        waveInDevice.DataAvailable += (s, args) => client.Send(args.Buffer, args.BytesRecorded, ServerConnection.MainEndPoint);
    }
}