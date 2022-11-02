using System;
using System.Collections.Generic;
using System.IO.Enumeration;
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
            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Any, 5004); // Of 127.0.0.1 ipv ipaddress.any

            // bind the socket to the local endpoint
            socket.Bind(localEndpoint);

            Console.WriteLine("Waiting for clients...");
        }

        public ErrorType StartDownload()
        {
            // TODO: Instantiate and initialize different messages needed for the communication
            // required messages are: HelloMSG, RequestMSG, DataMSG, AckMSG, CloseMSG
            // Set attribute values for each class accordingly 
            HelloMSG GreetBack;
            RequestMSG req;
            DataMSG data = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();

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
            socket.Connect(remoteEndpoint);
            GreetBack = new HelloMSG() { Type = Messages.HELLO, From = Server, To = Client, ConID = SessionID };
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
            req = new RequestMSG() { Type = Messages.REQUEST, From = Server, To = Client, FileName = fileName, ConID = SessionID };
            msg = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(req));
            socket.SendTo(msg, remoteEP);


            // TODO:  Start sending file data by setting first the socket ReceiveTimeout value



            // TODO: Open and read the text-file first
            // Make sure to locate a path on windows and macos platforms



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
