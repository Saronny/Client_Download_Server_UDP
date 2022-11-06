using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using UDP_FTP.Error_Handling;
using UDP_FTP.Models;
using static UDP_FTP.Models.Enums;

namespace UDP_FTP.File_Handler
{
    class Communicate
    {
        private const string Server = "MyServer";
        private string Client;
        private int SessionID;
        private Socket socket;
        private IPEndPoint remoteEndpoint;
        private EndPoint remoteEP;
        private ErrorType Status;
        private byte[] buffer;
        byte[] msg;
        private string file;
        ConSettings C;

        private int b;
        private string response;

        public Communicate()
        {
            // TODO: Initializes another instance of the IPEndPoint for the remote host
            string remoteIpAddress = "127.0.0.1";
            remoteEndpoint = new IPEndPoint(IPAddress.Parse(remoteIpAddress), 5010);
            remoteEP = (EndPoint)remoteEndpoint;


            // TODO: Specify the buffer size
            buffer = new byte[1000];

            // TODO: Get a random SessionID
            Random rnd = new Random();
            SessionID = rnd.Next(1, 1000);

            // TODO: Create local IPEndpoints and a Socket to listen 
            //       Keep using port numbers and protocols mention in the assignment description
            //       Associate a socket to the IPEndpoints to start the communication

            // create a udp socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // create a local endpoint
            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Any, 5004); 

            // bind the socket to the local endpoint
            socket.Bind(localEndpoint);

            Console.WriteLine("Waiting for clients...");
        }

        private static byte[] getSpecificBytes(byte[] arr, int start, int end)
        {
            byte[] result = new byte[end - start];
            try
            {
                for (int i = start; i < end; i++)
                {
                    result[i - start] = arr[i];
                }
            } catch (System.IndexOutOfRangeException e)
            {
                for (int i = start; i < arr.Length; i++)
                {
                    result[i - start] = arr[i];
                }
            }
            return result;
        }

        public ErrorType StartDownload()
        {
            // TODO: Instantiate and initialize different messages needed for the communication
            // required messages are: HelloMSG, RequestMSG, DataMSG, AckMSG, CloseMSG
            // Set attribute values for each class accordingly 
            HelloMSG GreetBack;
            RequestMSG req;
            DataMSG data;
            AckMSG ack;
            CloseMSG cls;

            // TODO: Start the communication by receiving a HelloMSG message... Done
            // Receive and deserialize HelloMSG message... Done
            // Verify if there are no errors... Done
            // Type must match one of the ConSettings' types and receiver address must be the server address... Done
            b = socket.ReceiveFrom(buffer, buffer.Length, SocketFlags.None, ref remoteEP);
            response = Encoding.ASCII.GetString(buffer, 0, b);
            HelloMSG receivedHello = JsonSerializer.Deserialize<HelloMSG>(response);
            Client = receivedHello.From; // Get the IP address of the sender
            
            C = new ConSettings() { Type = Messages.HELLO, To = Server };

            if (ErrorHandler.VerifyGreeting(receivedHello, C) == ErrorType.BADREQUEST)
            {
                return ErrorHandler.VerifyGreeting(receivedHello, C);
            } 
            else
            {
                Console.WriteLine("Received HelloMSG from {0}", remoteEP.ToString());
            }

            // TODO: If no error is found then HelloMSG will be sent back
            // socket.Connect(remoteEndpoint);
            GreetBack = new HelloMSG() { Type = Messages.HELLO_REPLY, From = Server, To = Client, ConID = SessionID };
            msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(GreetBack));
            socket.SendTo(msg, remoteEP);


            // TODO: Receive the next message
            // Expected message is a download RequestMSG message containing the file name
            // Receive the message and verify if there are no errors
            b = socket.ReceiveFrom(buffer, buffer.Length, SocketFlags.None, ref remoteEP);
            response = Encoding.ASCII.GetString(buffer, 0, b);
            RequestMSG receivedRequest = JsonSerializer.Deserialize<RequestMSG>(response);
            string fileName = receivedRequest.FileName;

            C = new ConSettings() { Type = Messages.REQUEST, From = Client, To = Server, ConID = SessionID };

            if (ErrorHandler.VerifyRequest(receivedRequest, C) == ErrorType.BADREQUEST)
            {
                return ErrorHandler.VerifyRequest(receivedRequest, C);
            }
            else
            {
                Console.WriteLine("Received RequestMSG from {0}", remoteEP.ToString());
            }

            // TODO: Send a RequestMSG of type REPLY message to remoteEndpoint verifying the status
            req = new RequestMSG() { Type = Messages.REPLY, From = Server, To = Client, FileName = fileName, ConID = SessionID };
            msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(req));
            socket.SendTo(msg, remoteEP);


            // TODO:  Start sending file data by setting first the socket ReceiveTimeout value
            socket.ReceiveTimeout = 1000;

            // TODO: Open and read the text-file first
            // Make sure to locate a path on windows and macos platforms

            // hoe tf moeten we weten waar de file staat op macos
            byte[] lines;
            try
            {
                string path = "..\\..\\..\\" + fileName;
                lines = System.IO.File.ReadAllBytes(path);
                Console.WriteLine(lines);
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                return ErrorType.BADREQUEST;
            }
            int segmentNum = 1;
            int segmentAmount = lines.Length / (int)Params.SEGMENT_SIZE;
            segmentAmount += lines.Length % (int)Params.SEGMENT_SIZE != 0 ? 1 : 0;
            int windowsLeft = segmentAmount / (int)Params.WINDOW_SIZE;
            List<int> dataMSGs = new List<int>(); // a list of the sent data messages
            List<int> ackMSGs = new List<int>(); // a list of the received ack messages
            while (windowsLeft > 0) 
            {
                for (int i = 1; segmentNum <= segmentAmount; i++)
                {
                    byte[] dataToSend;
                    if (segmentNum < segmentAmount)
                    {
                        dataToSend = getSpecificBytes(lines, (segmentNum - 1) * (int)Params.SEGMENT_SIZE, segmentNum * (int)Params.SEGMENT_SIZE);
                    } else
                    {
                        dataToSend = getSpecificBytes(lines, (segmentNum - 1) * (int)Params.SEGMENT_SIZE, lines.Length);
                    }
                    data = new DataMSG() { ConID = SessionID, From = Server, To = Client, Type = Messages.DATA, Data = dataToSend, More = segmentNum < segmentAmount, Size = lines.Length, Sequence = segmentNum-1 };
                    msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(data));
                    socket.SendTo(msg, remoteEP);
                    segmentNum++;
                    dataMSGs.Add(data.Sequence);
                }
                try
                {
                    for (int i = 0; i < (int)Params.WINDOW_SIZE; i++)
                    {
                        b = socket.ReceiveFrom(buffer, buffer.Length, SocketFlags.None, ref remoteEP);
                        response = Encoding.ASCII.GetString(buffer, 0, b);
                        AckMSG receivedAck = JsonSerializer.Deserialize<AckMSG>(response);

                        Console.WriteLine($"Received ack Nr {receivedAck.Sequence}");
                        ackMSGs.Add(receivedAck.Sequence);

                        C = new ConSettings() { Type = Messages.ACK, From = Client, To = Server, ConID = SessionID, Sequence = receivedAck.Sequence };

                        if (ErrorHandler.VerifyAck(receivedAck, C) == ErrorType.BADREQUEST)
                        {
                            return ErrorHandler.VerifyAck(receivedAck, C);
                        }
                    }
                } catch (SocketException e)
                {
                    Console.WriteLine("Timeout on ");
                    segmentNum = dataMSGs.Except(ackMSGs).ToList().Min(); // Get the lost ack
                    windowsLeft++;
                }
                windowsLeft--;
            }




            // TODO: Sliding window with go-back-n implementation
            // Calculate the length of data to be sent
            // Send file-content as DataMSG message as long as there are still values to be sent
            // Consider the WINDOW_SIZE and SEGMENT_SIZE when sending a message  
            // Make sure to address the case if remaining bytes are less than WINDOW_SIZE
            //
            // Suggestion: while there are still bytes left to send,
            // first you send a full window of data
            // second you wait for the acks
            // then you start again.



            // TODO: Receive and verify the acknowledgements (AckMSG) of sent messages
            // Your client implementation should send an AckMSG message for each received DataMSG message   



            // TODO: Print each confirmed sequence in the console
            // receive the message and verify if there are no errors


            // TODO: Send a CloseMSG message to the client for the current session
            // Send close connection request

            // TODO: Receive and verify a CloseMSG message confirmation for the current session
            // Get close connection confirmation
            // Receive the message and verify if there are no errors


            // TODO: Uncomment deze shit
            // Console.WriteLine("Group members: {0} | {1}", student_1, student_2);
            return ErrorType.NOERROR;
        }
    }
}
