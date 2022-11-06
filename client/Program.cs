using System;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using UDP_FTP.Models;
using static UDP_FTP.Error_Handling.ErrorHandler;
using System.Collections.Generic;
using System.Linq;
using static UDP_FTP.Models.Enums;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            
            


            byte[] buffer = new byte[1000];
            byte[] msg = new byte[100];
            Socket sock;
            // TODO: Initialise the socket/s as needed from the description of the assignment

            const int client_port = 5004;
            const int server_port = 5010;
            IPAddress server_ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint server_endpoint = new IPEndPoint(server_ip, client_port);
            IPEndPoint client_endpoint = new IPEndPoint(IPAddress.Any, 0);
            EndPoint remoteEP = (EndPoint)server_endpoint;
            ConSettings C;


            

            HelloMSG h = new HelloMSG() { Type = Messages.HELLO, From = "Client", To = "Server", ConID = 0 }; //Client to Server
            RequestMSG r = new RequestMSG() { Type = Messages.REQUEST, From = "Client", To = "Server", ConID = 0, FileName = "test.txt" }; //Client to Server
            DataMSG D = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();

            try
            {
                // TODO: Instantiate and initialize your socket 
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        

                // TODO: Send hello message to the server
                msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(h));
                sock.SendTo(msg, remoteEP);
                Console.WriteLine("Sent Hello Message");
                

                // TODO: Receive and verify a HelloMSG
                int b = sock.ReceiveFrom(buffer, ref remoteEP);
                string data = Encoding.ASCII.GetString(buffer, 0, b);
                HelloMSG h2 = JsonSerializer.Deserialize<HelloMSG>(data);
                C = new ConSettings() { To = "Client" };     
                if (VerifyGreetingReply(h2, C) == ErrorType.NOERROR)
                {
                    Console.WriteLine("Hello message received");
                    r = new RequestMSG() { Type = Messages.REQUEST, From = "Client", To = "Server", ConID = h2.ConID, FileName = "test.txt", Status = ErrorType.NOERROR };
                }
                else
                {
                    Console.WriteLine("Hello message not received");
                }


                // TODO: Send the RequestMSG message requesting to download a file name
                msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(r));
                sock.SendTo(msg, remoteEP);
                Console.WriteLine("Request sent");

                // TODO: Receive a RequestMSG from remoteEndpoint
                // receive the message and verify if there are no errors
                b = sock.ReceiveFrom(buffer, ref remoteEP);
                data = Encoding.ASCII.GetString(buffer, 0, b);
                RequestMSG r2 = JsonSerializer.Deserialize<RequestMSG>(data);
                C = new ConSettings() { ConID = r.ConID, From = "Server", To = "Client" };
                if (VerifyRequestReply(r2, C) == ErrorType.NOERROR)
                {
                    Console.WriteLine("Request reply message received");
                
                }
                else
                {
                    Console.WriteLine("Request reply message not received");
                }


                // TODO: Check if there are more DataMSG messages to be received 
                // receive the message and verify if there are no errors
                bool Transferring = true;
                var dataMessages = new List<DataMSG>();

                while(Transferring) {
                    b = sock.ReceiveFrom(buffer, ref remoteEP);
                    D = JsonSerializer.Deserialize<DataMSG>(data);
                    dataMessages.Add(D);
                    Console.WriteLine("Data message received");
                        
                    if (D.More == false)
                    {
                        Transferring = false;
                    }
                }



                // TODO: Send back AckMSG for each received DataMSG 

                foreach (DataMSG d in dataMessages)
                {
                    ack = new AckMSG() { Type = Messages.ACK, From = "Client", To = "Server", ConID = d.ConID, Sequence = d.Sequence };
                    msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(ack));
                    sock.SendTo(msg, remoteEP);
                    Console.WriteLine("Ack message sent");
                }


                // TODO: Receive close message
                // receive the message and verify if there are no errors
                b = sock.ReceiveFrom(buffer, ref remoteEP);
                data = Encoding.ASCII.GetString(buffer, 0, b);
                cls = JsonSerializer.Deserialize<CloseMSG>(data);
                // TODO: confirm close message
                C = new ConSettings() { ConID = r.ConID, From = "Server", To = "Client", Type = Messages.CLOSE_REQUEST };
                if (VerifyCloseRequest(cls, C) == ErrorType.NOERROR)
                {
                    Console.WriteLine("Close message received");
                    cls = new CloseMSG() { Type = Messages.CLOSE_CONFIRM, From = "Client", To = "Server", ConID = cls.ConID };
                    msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(cls));
                    sock.SendTo(msg, remoteEP);
                    Console.WriteLine("Close sent");
                }
                else
                {
                    Console.WriteLine("Close message not received");
                }   

            }
            catch
            {
                Console.WriteLine("\n Socket Error. Terminating");
            }

            Console.WriteLine("Download Complete!");
           
        }
    }
}
