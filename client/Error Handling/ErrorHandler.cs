using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDP_FTP.Models;
using static UDP_FTP.Models.Enums;

namespace UDP_FTP.Error_Handling
{
    public static class ErrorHandler
    {
        public static ErrorType VerifyGreeting( HelloMSG hello, ConSettings C)
        {
            if ( hello.To != C.To || hello.Type != Messages.HELLO)
                return ErrorType.BADREQUEST;
            return ErrorType.NOERROR;
        }

        public static ErrorType VerifyGreetingReply (HelloMSG hello, ConSettings C)
        {
            if (hello.To != C.To || hello.Type != Messages.HELLO_REPLY)
                return ErrorType.BADREQUEST;
            return ErrorType.NOERROR;
        }
        public static ErrorType VerifyRequest( RequestMSG req, ConSettings C)
        {
            if (req.ConID != C.ConID || req.From != C.From || req.To != C.To || req.Type != Messages.REQUEST)
                return ErrorType.BADREQUEST;
            return ErrorType.NOERROR;
        }

        public static ErrorType VerifyData(DataMSG data, ConSettings C)
        {
            if (data.ConID != C.ConID || data.From != C.From || data.To != C.To || data.Type != Messages.DATA)
                return ErrorType.BADREQUEST;
            return ErrorType.NOERROR;
        }

        public static ErrorType VerifyRequestReply(RequestMSG req, ConSettings C)
        {
            if (req.ConID != C.ConID || req.From != C.From || req.To != C.To || req.Type != Messages.REPLY)
                return ErrorType.BADREQUEST;
            return ErrorType.NOERROR;
        }
        public static ErrorType VerifyAck( AckMSG ack, ConSettings C)
        {
            if (ack.ConID != C.ConID || ack.From != C.From || ack.To != C.To || ack.Type != Messages.ACK || ack.Sequence < C.Sequence || ack.Sequence > C.Sequence + (int)Params.WINDOW_SIZE )
                return ErrorType.BADREQUEST;
            return ErrorType.NOERROR;
        }

        public static ErrorType VerifyCloseRequest(CloseMSG close, ConSettings C)
        {
            if (close.ConID != C.ConID || close.From != C.From || close.To != C.To || close.Type != Messages.CLOSE_REQUEST)
                return ErrorType.BADREQUEST;
            return ErrorType.NOERROR;
        }
        public static ErrorType VerifyClose( CloseMSG cls, ConSettings C)
        {
            if (cls.ConID != C.ConID || cls.From != C.From || cls.To != C.To || cls.Type != Messages.CLOSE_CONFIRM)
                return ErrorType.BADREQUEST;
            return ErrorType.NOERROR;
        }
    }
}
