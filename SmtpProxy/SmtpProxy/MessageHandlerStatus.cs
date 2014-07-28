using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmtpProxy
{
    public enum MessageHandlerStatus
    {
        Open,
        Greeting,
        MailFrom,
        Recipient,
        Data,
        EndData,
        Closed
    }
}