using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using static UDP_FTP.Error_Handling.ErrorHandler;
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

        const int client_port = 5004;
        const int server_port = 5010;


        public Communicate()
        {
            // TODO: Initializes another instance of the IPEndPoint for the remote host
            IPAddress server_ip = IPAddress.Parse("127.0.0.1");
            remoteEndpoint = new IPEndPoint(server_ip, client_port);
            remoteEP = (EndPoint)remoteEndpoint;



            // TODO: Specify the buffer size
            buffer = new byte[1000];


            // TODO: Get a random SessionID
            Random rnd = new Random();
            SessionID = rnd.Next(1, 1000);


            // TODO: Create local IPEndpoints and a Socket to listen 
            //       Keep using port numbers and protocols mention in the assignment description
            //       Associate a socket to the IPEndpoints to start the communication
            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Any, server_port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(localEndpoint);


        }

        public ErrorType StartDownload()
        {
            // TODO: Instantiate and initialize different messages needed for the communication
            // required messages are: HelloMSG, RequestMSG, DataMSG, AckMSG, CloseMSG
            // Set attribute values for each class accordingly 
        
            HelloMSG Greetback = new HelloMSG() { Type = Messages.HELLO_REPLY, From = "Server", To = "Client", ConID = SessionID }; // Server  -> Client
            RequestMSG req = new RequestMSG();
            DataMSG data = new DataMSG();
            AckMSG ack = new AckMSG();
            CloseMSG cls = new CloseMSG();


            // TODO: Start the communication by receiving a HelloMSG message
            // Receive and deserialize HelloMSG message 
            // Verify if there are no errors
            // Type must match one of the ConSettings' types and receiver address must be the server address

            int b = socket.ReceiveFrom(buffer, ref remoteEP);
            string msg = Encoding.ASCII.GetString(buffer, 0, b);
            Console.WriteLine("Client said {0}", msg);
            HelloMSG Greet = JsonSerializer.Deserialize<HelloMSG>(msg);
            C = new ConSettings() { To = "Server", Type = Messages.HELLO };
            if (VerifyGreeting(Greet, C) == ErrorType.NOERROR)
            {
                Console.WriteLine("Hello message received");
                Status = ErrorType.NOERROR;
            }
            else {
                Console.WriteLine("Hello message not received");
                Status = ErrorType.BADREQUEST;
            }
            // TODO: If no error is found then HelloMSG will be sent back
            if (Status == ErrorType.NOERROR){
                socket.SendTo(Encoding.ASCII.GetBytes(JsonSerializer.Serialize(Greetback)), remoteEP);
            }
            else
            {
                Console.WriteLine("Error in Hello Message");
            }


            // TODO: Receive the next message
            // Expected message is a download RequestMSG message containing the file name
            // Receive the message and verify if there are no errors
            b = socket.ReceiveFrom(buffer, ref remoteEP);
            msg = Encoding.ASCII.GetString(buffer, 0, b);
            Console.WriteLine("Client said {0}", msg);
            req = JsonSerializer.Deserialize<RequestMSG>(msg);
            C = new ConSettings() { Type = Messages.REQUEST, To = "Server", From = "Client", ConID = SessionID }; 
            if (VerifyRequest(req, C) == ErrorType.NOERROR)
            {
                Console.WriteLine("Request message received");
                Status = ErrorType.NOERROR;
            }
            else
            {
                Console.WriteLine("Error in Request Message");
                Status = ErrorType.BADREQUEST;
            }


            // TODO: Send a RequestMSG of type REPLY message to remoteEndpoint verifying the status



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

            string student_1 = "";
            string student_2 = "";
            Console.WriteLine("Group members: {0} | {1}", student_1, student_2);
            return ErrorType.NOERROR;
        }
    }
}
