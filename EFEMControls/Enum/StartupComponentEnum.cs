using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFEM.GUIControls.Enum
{
    public class StartupComponentEnum
    {
        public enum ProcedureStatus
        {
            Pending = 0,
            Working = 1,
            Finish = 2
        }

        public enum CommType
        {
            RS232,
            TCPIP
        }
    }
}
