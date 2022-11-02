using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using UDP_FTP.Models;
using UDP_FTP.Error_Handling;
using static UDP_FTP.Models.Enums;
using System.Text.Json.Nodes;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: add the student number of your group members as a string value... Done
            // string format example: "Jan Jansen 09123456" 
            // If a group has only one member fill in an empty string for the second student
            string student_1 = "Timo van der Ven 1024454";
            string student_2 = "Mike Dudok 1026366";

            byte[] buffer = new byte[1000];
            byte[] msg = new byte[100];
            Socket sock;
            // TODO: Initialise the socket/s as needed from the description of the assignment ???
            // Create a an endpoint with port 5004 and local ip address... Done
            string serverIp = "127.0.0.1";
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), 5004);
            EndPoint serverEP = (EndPoint)serverEndpoint;
            
            string clientIp = "127.0.0.1";
            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Parse(clientIp), 5010);
            EndPoint localEP = (EndPoint)localEndpoint;

            string clientName = "MyClient";
            string serverName = "MyServer";

            HelloMSG h;
            RequestMSG r;
            DataMSG D = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();
            
            try
            {
                Console.WriteLine("Client started");
                // TODO: Instantiate and initialize your socket... Done
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.Bind(localEP);
                sock.Connect(serverEndpoint);


                // TODO: Send hello msg... Done
                h = new HelloMSG() { Type = Messages.HELLO, From = clientName, To = serverName, ConID = 0 };
                msg = JsonSerializer.SerializeToUtf8Bytes(h, typeof(HelloMSG)); // Serialize and encode
                sock.SendTo(msg, serverEndpoint); // Send
#if DEBUG 
                Console.WriteLine("Sent Hello message");
#endif

                // TODO: Receive and verify a HelloMSG 
                int b = sock.ReceiveFrom(buffer, buffer.Length, SocketFlags.None, ref serverEP);
                string response = Encoding.ASCII.GetString(buffer, 0, b);
                HelloMSG receivedHello = JsonSerializer.Deserialize<HelloMSG>(response);
                var sessionId = receivedHello.ConID;

                ConSettings helloCon = new ConSettings() { To = clientName, Type = Messages.HELLO };
                if (ErrorHandler.VerifyGreeting(receivedHello, helloCon) == ErrorType.BADREQUEST)
                {
                    throw new Exception("Bad request");
                }
                else
                {
                    Console.WriteLine("Received HelloMSG from {0}", serverEP.ToString());
                }

                // TODO: Send the RequestMSG message requesting to download a file name
                r = new RequestMSG() { Type = Messages.REQUEST, From = clientName, To = serverName, ConID = sessionId, FileName = "test.txt" };
                msg = JsonSerializer.SerializeToUtf8Bytes(r, typeof(RequestMSG)); // Serialize and encode
                sock.SendTo(msg, serverEndpoint); // Send
#if DEBUG
                Console.WriteLine("Sent Request message");
#endif

                // TODO: Receive a RequestMSG from remoteEndpoint
                // receive the message and verify if there are no errors

                b = sock.ReceiveFrom(buffer, buffer.Length, SocketFlags.None, ref serverEP);
                response = Encoding.ASCII.GetString(buffer, 0, b);
                RequestMSG receivedRequest = JsonSerializer.Deserialize<RequestMSG>(response);

                ConSettings requestCon = new ConSettings() { ConID = sessionId, From = serverName, To = clientName };
                if (ErrorHandler.VerifyRequest(receivedRequest, requestCon) == ErrorType.BADREQUEST)
                {
                    throw new Exception("Bad request");
                }
                else
                {
                    Console.WriteLine("Received RequestMSG from {0}", serverEP.ToString());
                }


                // TODO: Check if there are more DataMSG messages to be received 
                // receive the message and verify if there are no errors

                // TODO: Send back AckMSG for each received DataMSG 


                // TODO: Receive close message
                // receive the message and verify if there are no errors

                // TODO: confirm close message

            }
            catch (Exception e)
            {
                Console.WriteLine("\n " + e.ToString());
            }

            Console.WriteLine("Download Complete!");
           
        }
    }
}
