using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Web;
using System.Diagnostics;

namespace WikiDataAnalysis
{
    class SocketService
    {
        static StreamWriter logWriter = new StreamWriter(NewLogFileName());
        class HttpListener
        {
            Socket socket;
            NetworkStream stream;
            Func<byte[], byte[]> reaction;
            public HttpListener(Socket s,Func<string,string>ra)
            {
                socket = s;
                reaction = new Func<byte[], byte[]>((data) =>
                {
                    string t = "";
                    try
                    {
                        t = Encoding.UTF8.GetString(data);
                        logWriter.WriteLine($"Recv: {t}");
                        t = ra(t);
                        logWriter.WriteLine($"Send: {t}");
                    }
                    catch (Exception error) { t = error.ToString(); }
                    return Encoding.UTF8.GetBytes(t);
                });
            }
            public void Run()
            {
                logWriter.WriteLine("==========Connection Started==========");
                stream = new NetworkStream(socket);
                while (Request()) ;
                stream.Close();
                logWriter.WriteLine("==========Connection Finished==========");
            }
            bool Request()
            {
                try
                {
                    List<byte> data = new List<byte>();
                    const byte target = (byte)'\0';
                    while (true)
                    {
                        var _b = stream.ReadByte();
                        if (_b == -1)
                        {
                            logWriter.WriteLine("Connection closed.");
                            return false;
                        }
                        var b = (byte)_b;
                        if ( b == target)
                        {
                            logWriter.WriteLine($"receive_length={data.Count}");
                            var to_send = reaction(data.ToArray());
                            socket.Send(to_send);
                            socket.Send(new[] { (byte)'\0' });
                            return true;
                        }
                        else data.Add(b);
                    }
                }
                catch (Exception error)
                {
                    logWriter.WriteLine($"Request Error:\r\n{error}");
                    return false;
                }
            }
        }
        static string NewLogFileName()
        {
            return "log_"+DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fffffff")+ ".txt";
        }
        static SocketService()
        {
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    logWriter.Flush();
                }
            }).Start();
        }
        public static void StartService(int port,Func<string,string>reaction)
        {
            try
            {
                Trace.Indent();
                new Thread(() =>
                {
                    IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);
                    Socket server_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    server_sock.Bind(ipep);
                    server_sock.Listen(10);
                    logWriter.WriteLine("==========Ready==========");
                    while (true)
                    {
                        Socket client = server_sock.Accept();
                        logWriter.WriteLine("==========Accepted==========");
                        IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
                        var listener = new HttpListener(client, reaction);
                        new Thread(new ThreadStart(listener.Run)).Start();
                    }
                }).Start();
                logWriter.WriteLine($"Service started at port {port}.");
            }
            finally { Trace.Unindent(); }
        }
    }
}
