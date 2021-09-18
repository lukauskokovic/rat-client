using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public static class ServerConnection 
{
    static int port = 1420;
    public static Socket Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static IPAddress ServerAddress;
    public static void ConnectToServer()
    {
        ShareScreen.Running = false;
        string ip = GetIP();
        ServerAddress = null;
        if (!IPAddress.TryParse(ip, out ServerAddress))
            goto Error;

        var point = new IPEndPoint(ServerAddress, port);
        
        if (Socket != null)
            Socket.Dispose();
        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            Console.WriteLine("Trying to connect");
            Socket.Connect(point);
            Console.WriteLine("Connected to server");
            byte[] userNameBuffer = System.Text.Encoding.Unicode.GetBytes(Environment.UserName);
            Socket.Send(userNameBuffer);

        }
        catch (SocketException)
        {
            goto Error;
        }
        return;
    Error:
        Console.WriteLine(" -No connection (5 secs)");
        Thread.Sleep(5000);
        ConnectToServer();
    }

    static string GetIP()
    {
        return "";
    }
}