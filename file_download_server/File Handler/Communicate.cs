using System;
using System.IO;
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


        private const int client_port = 5004;
        private const int server_port = 5010;


        public Communicate()
        {
            // TODO: Initializes another instance of the IPEndPoint for the remote host
            IPAddress server_ip = IPAddress.Parse("127.0.0.1");
            remoteEndpoint = new IPEndPoint(IPAddress.Any, server_port);
            remoteEP = (EndPoint)remoteEndpoint;



            // TODO: Specify the buffer size
            buffer = new byte[(int)Enums.Params.BUFFER_SIZE];


            // TODO: Get a random SessionID
            Random rnd = new Random();
            SessionID = rnd.Next(1, 1000);


            // TODO: Create local IPEndpoints and a Socket to listen 
            //       Keep using port numbers and protocols mention in the assignment description
            //       Associate a socket to the IPEndpoints to start the communication
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), client_port));


        }

        public ErrorType StartDownload()
        {
            // TODO: Instantiate and initialize different messages needed for the communication
            // required messages are: HelloMSG, RequestMSG, DataMSG, AckMSG, CloseMSG
            // Set attribute values for each class accordingly 

            HelloMSG Greetback = new HelloMSG() { Type = Messages.HELLO_REPLY, From = "Server", To = "Client", ConID = SessionID }; // Server  -> Client
            HelloMSG Greet = new HelloMSG() { Type = Messages.HELLO, From = "Client", To = "Server", ConID = SessionID }; // Client -> Server
            RequestMSG req = new RequestMSG() { Type = Messages.REQUEST, From = "Client", To = "Server", ConID = SessionID, FileName = "test.txt", Status = ErrorType.NOERROR }; // Client -> Server
            RequestMSG req2 = new RequestMSG() { Type = Messages.REPLY, From = "Server", To = "Client", ConID = SessionID, FileName = "test.txt", Status = ErrorType.NOERROR }; // Server -> Client
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
            Greet = JsonSerializer.Deserialize<HelloMSG>(msg);
            C = new ConSettings() { To = "Server", Type = Messages.HELLO };
            if (VerifyGreeting(Greet, C) == ErrorType.NOERROR)
            {
                Console.WriteLine("Hello message received");
                Status = ErrorType.NOERROR;
            }
            else
            {
                Console.WriteLine("Hello message not received");
                Status = ErrorType.CONNECTION_ERROR;
            }
            // TODO: If no error is found then HelloMSG will be sent back
            if (Status == ErrorType.NOERROR)
            {
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
            if (Status == ErrorType.NOERROR)
            {
                msg = JsonSerializer.Serialize(req2);
                socket.SendTo(Encoding.ASCII.GetBytes(msg), remoteEP);
            }
            else
            {
                Console.WriteLine("Error in Request Message");
            }






            // TODO:  Start sending file data by setting first the socket ReceiveTimeout value
            socket.ReceiveTimeout = 1000;





            // TODO: Open and read the text-file first
            // Make sure to locate a path on windows and macos platforms

            var file = Encoding.ASCII.GetBytes(File.ReadAllText(req2.FileName));




            // TODO: Sliding window with go-back-n implementation
            // Calculate the length of data to be sent
            // Send the data in segments of size SEGMENT_SIZE
            // Send file-content as DataMSG message as long as there are still values to be sent
            // Consider the WINDOW_SIZE and SEGMENT_SIZE when sending a message  
            // Make sure to address the case if remaining bytes are less than WINDOW_SIZE
            //
            // Suggestion: while there are still bytes left to send,
            // first you send a full window of data
            // second you wait for the acks
            // then you start again.

            int send_base = 0;
            int nextseqnum = 0;

            int N = (int)Enums.Params.WINDOW_SIZE;
            int S = (int)Enums.Params.SEGMENT_SIZE;


            int total_segments = file.Length / S;
            int last_segment = file.Length % S;
            int segments_sent = 0;

            byte[] data_buffer = new byte[S];

            bool Transmission = true;
            while (Transmission)
            {
                
                //Sending a full window of data
                if (nextseqnum < send_base + N)
                {
                    //setting the data message
                    data = new DataMSG() { };
                    data.Type = Messages.DATA;
                    data.ConID = SessionID;
                    data.To = "Client";
                    data.From = "Server";
                    data.Sequence = segments_sent++;
                    if (segments_sent == total_segments)
                    {
                        data.More = false;
                        data.Size = last_segment;
                        data_buffer = new byte[last_segment];
                    }
                    else
                    {
                        data.More = true;
                        data.Size = S;
                    }
                    for(int i = 0; i < data.Size; i++) {
                        data_buffer[i] = file[i + (nextseqnum * S)];
                    }
                    data.Data = data_buffer;
                }
                //sending the data message
                msg = JsonSerializer.Serialize(data);
                socket.SendTo(Encoding.ASCII.GetBytes(msg), remoteEP);
                nextseqnum++;

            }
            // TODO: Receive and verify the acknowledgements (AckMSG) of sent messages
            // Your client implementation should send an AckMSG message for each received DataMSG message  
            for (int i = 0; i < N; i++)
            {
                try
                {
                    b = socket.ReceiveFrom(buffer, ref remoteEP);
                    msg = Encoding.ASCII.GetString(buffer, 0, b);
                    Console.WriteLine("Client said {0}", msg);
                    ack = JsonSerializer.Deserialize<AckMSG>(msg);
                    C = new ConSettings() { Type = Messages.ACK, To = "Server", From = "Client", ConID = SessionID };
                    if (VerifyAck(ack, C) == ErrorType.NOERROR)
                    {
                        Console.WriteLine("Ack message received");
                        Status = ErrorType.NOERROR;
                    }
                    else
                    {
                        Console.WriteLine("Error in Ack Message");
                        Status = ErrorType.BADREQUEST;
                    }
                    if (Status == ErrorType.NOERROR)
                    {
                        send_base = ack.Sequence + 1;
                    }
                    else
                    {
                        Console.WriteLine("Error in Ack Message");
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Timeout");

                }
            }
        










        // TODO: Print each confirmed sequence in the console
        // receive the message and verify if there are no errors


        // TODO: Send a CloseMSG message to the client for the current session
        // Send close connection request
        cls = new CloseMSG() { Type = Messages.CLOSE_REQUEST, From = "Server", To = "Client", ConID = SessionID };
        msg = JsonSerializer.Serialize(cls);
            socket.SendTo(Encoding.ASCII.GetBytes(msg), remoteEP);

            // TODO: Receive and verify a CloseMSG message confirmation for the current session
            // Get close connection confirmation
            // Receive the message and verify if there are no errors
            b = socket.ReceiveFrom(buffer, ref remoteEP);
            msg = Encoding.ASCII.GetString(buffer, 0, b);
            cls = JsonSerializer.Deserialize<CloseMSG>(msg);
            C = new ConSettings() { Type = Messages.CLOSE_CONFIRM, To = "Server", From = "Client", ConID = SessionID };
            if (VerifyClose(cls, C) == ErrorType.NOERROR)
            {
                Console.WriteLine("Close message received");
                Status = ErrorType.NOERROR;
            }
            else
            {
                Console.WriteLine("Error in Close Message");
                Status = ErrorType.BADREQUEST;
            }

string student_1 = "Mike Dudok 1026366";
string student_2 = "Timo van der Ven 1024454";
Console.WriteLine("Group members: {0} | {1}", student_1, student_2);
return ErrorType.NOERROR;
        }
    }
}


