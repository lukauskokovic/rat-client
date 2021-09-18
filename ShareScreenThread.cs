using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

public static class ShareScreen 
{
    public static bool Running = false;
    public static Socket Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static void StartShareScreen()
    {
        if (!Running) 
        {
            Running = true;
            new Thread(() =>
            {
                try
                {
                    Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Socket.Connect(new IPEndPoint(ServerConnection.ServerAddress, 1421));
                    while (Running)
                    {

                        SendImage();
                        Thread.Sleep(100);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                Socket.Dispose();
                Console.WriteLine("Share screen end");
            }).Start();   
        }
    }

    static void SendImage()
    {
        Bitmap captureBitmap = new Bitmap(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height, PixelFormat.Format16bppRgb555);

        Rectangle captureRectangle = Screen.AllScreens[0].Bounds;
        Graphics captureGraphics = Graphics.FromImage(captureBitmap);
        captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
        MemoryStream ms = new MemoryStream();
        captureBitmap.Save(ms, ImageFormat.Png);
        byte[] buffer = ms.ToArray();
        try
        {
            Socket.Send(buffer);
        }
        catch (SocketException)
        {
            Running = false;
        }

        captureBitmap.Dispose();
        captureGraphics.Dispose();
        buffer = null;
        ms.Close();
        ms.Dispose();
    }
}