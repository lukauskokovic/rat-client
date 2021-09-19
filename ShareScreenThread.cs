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
    static Graphics captureGraphics;
    static Bitmap captureBitmap;
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
                    Socket.Connect(new IPEndPoint(ServerConnection.ServerAddress, ServerConnection.Port+1));
                    while (Running)
                    {
                        SendImage();
                        Thread.Sleep(60);
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
    static MemoryStream ms;
    static void SendImage()
    {
        Screen MainScreen = Screen.AllScreens[0];

        captureBitmap = new Bitmap(MainScreen.Bounds.Width, MainScreen.Bounds.Height, PixelFormat.Format16bppRgb555);
        captureGraphics = Graphics.FromImage(captureBitmap);
        Rectangle captureRectangle = MainScreen.Bounds;
        captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
        var resized = new Bitmap(captureBitmap, new Size(1280, 720));
        captureBitmap.Dispose();

        ms = new MemoryStream();
        resized.Save(ms, ImageFormat.Jpeg);

        resized.Dispose();
        captureBitmap.Dispose();
        captureGraphics.Dispose();
        byte[] Buffer = ms.ToArray();
        ms.Dispose();
        try 
        {
            Socket.Send(Buffer);
        }
        catch(SocketException) 
        {
            Running = false;
        }
        Buffer = null;
    }
}