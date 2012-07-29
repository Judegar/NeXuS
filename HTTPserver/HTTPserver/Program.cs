using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;


namespace HTTPserver
{

    class MyWebserver
    {
        string dir_path;
        string[,] mime_types;
        
        const bool LOG_REQUEST = true;
        const bool LOG_REQUEST_SHORT = false;
        const bool LOG_RESPONSE = true;
        const bool LOG_RESPONSE_SHORT = true;
        const bool LOG_CONNECTIONS = true;


        public MyWebserver(string path_to_dir)
        {
            dir_path = path_to_dir;
        }
        
        
        public void Start()
        {
            IPAddress ipAddress = Dns.Resolve("localhost").AddressList[0];
            TcpListener listener = new TcpListener(ipAddress, 80);
            listener.Start();

            Console.WriteLine("Server started ...");

            while (true)
            {
                Socket socket = listener.AcceptSocket();
                
                Thread t = new Thread(new ParameterizedThreadStart(ClientThread));

                t.Start(socket);
            }
        }

        private string ReceiveRequest()
        { 
            // pokud obsahuje content lenght tak musi byt prijato vsechno
            // takze prijmej dal jink vrat

            return "";
        }



        private void ClientThread(object parameter)
        {
            Socket socket = (Socket)parameter;
            
            byte[] data = new byte[100000];

                int count = socket.Receive(data);

                string request = Encoding.ASCII.GetString(data, 0, count);

                DebugRequest(request);

                int index = request.Length-1;
                if (request.Contains("POST") && index > 3 
                    )
                {
                    /*&& request[index - 3] == '\r' && request[index - 2] == '\n' &&
                   request[index-1]=='\r' && request[index]=='\n'*/
                    //count = socket.Receive(data);
                    //request = Encoding.ASCII.GetString(data, 0, count);
                    byte[] r = MakeHttpResponse(null, "200 OK", null);

                    socket.Send(r);

                    socket.Close();

                    return;
                }

                string url;

                bool result = ParseHttpRequest(request, out url);

                if (!result)
                {
                    BinaryReader brd = new BinaryReader(new FileStream(dir_path + @"\error.html", FileMode.Open));

                    byte[] contenterr = brd.ReadBytes((int)brd.BaseStream.Length);

                    brd.Close();

                    byte[] error_response = MakeHttpResponse(contenterr, "404 Eror", GetMimeTypes("html"));

                    socket.Send(error_response);

                    socket.Close();
                    
                    return;
                }

                if (url == "/") url = "/index.html";
                url = url.Replace(@"/", @"\");
                url = dir_path + url;

                

                if (url.Contains('?'))
                {
                    url = dir_path + "/index.html";
                }

                FileInfo fileinfo = new FileInfo(url);


                if (!fileinfo.Exists)
                {
                    BinaryReader brd = new BinaryReader(new FileStream(dir_path + @"\error.html", FileMode.Open));

                    byte[] contenterr = brd.ReadBytes((int)brd.BaseStream.Length);

                    brd.Close();

                    byte[] error_response = MakeHttpResponse(contenterr, "404 Eror", GetMimeTypes("html"));

                    socket.Send(error_response);

                    socket.Close();

                    return;
                }

                BinaryReader rd = new BinaryReader(new FileStream(url, FileMode.Open));

                byte[] content = rd.ReadBytes((int)rd.BaseStream.Length);

                rd.Close();

                string mime_type = GetMimeTypes(fileinfo.Extension);

                byte[] response = MakeHttpResponse(content,"200 OK", mime_type);

                socket.Send(response);

                socket.Close();
        }

        private byte[] MakeHttpResponse(byte[] content,string code, string mime_type)
        {
            string response = "HTTP/1.1 " + code + "\r\n";
            response += "Date: Thu, 20 May 2004 21:15:12 GMT\r\n";
            response += "Connection: close\r\n";
            response += "Server: MyWebServer\r\n";
            if (content != null)
            {
                response += "Accept-Ranges: bytes\r\n";
                response += "Content-Type: " + mime_type + "\r\n";
                response += "Content-Lenght: " + content.Length + "\r\n";
                response += "Last-Modified: Thu, 20 May 2004 21:15:12 GMT\r\n\r\n";
            }
            else
            {
                response += "\r\n";
            }

            Console.WriteLine(response);

            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            byte[] header = enc.GetBytes(response);

            int size = header.Length;
            if (content != null)
            {
                size+= content.Length;
            }

            byte[] complete = new byte[ size ];
            
            int index = 0;
            
            for (int i = 0; i < header.Length; i++)
            {
                complete[index] = header[i]; 
                index++;
            }

            if (content != null)
            {
                for (int i = 0; i < content.Length; i++)
                {
                    complete[index] = content[i];
                    index++;
                }
            }


            return complete;
        }

        private byte[] MakeHttpErrorResponse()
        {
            return null;
        }


        private bool ParseHttpRequest(string request, out string url)
        {
            url = "";
            string[] lines = request.Split('\n');

            if(!lines[0].Contains("GET") || !lines[0].Contains("HTTP/1.1")) return false;

            string _url = lines[0].Remove(0,3);
            _url = _url.Substring(0,_url.IndexOf("HTTP/1.1"));
            _url = _url.Trim();

            url = _url;

            return true;
        }


        private string GetMimeTypes(string suffix)
        {
            suffix = suffix.Remove(0, 1);

            if (suffix == "html" || suffix == "htm") return "text/html; charset=utf-8";
            else if (suffix == "css") return "text/css; charset=utf-8";
            else if (suffix == "png") return "image/png";
            else if (suffix == "js") return "application/x-javascript";
            else return "unknown";
        }


        private void DebugRequest(string request)
        {
            if (LOG_REQUEST)
            {
                if (LOG_REQUEST_SHORT)
                {
                    string[] lines = request.Split('\n');

                    Console.WriteLine("HTTP request: " + lines[0].Trim());
                }
                else 
                {
                    Console.WriteLine("HTTP request:\r\n" + request);
                }

                Console.WriteLine();
            }
        }

        private void DebugResponse(byte[] response)
        { 
        
        }


    }




    class Program
    {
        static void Main(string[] args)
        {
            MyWebserver myserver = new MyWebserver(@"C:\Users\user\Desktop\SimpleWebServer");
            myserver.Start();
        }
    }
}
