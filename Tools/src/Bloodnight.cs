using System;
using System.Collections.Generic;
using AllocsFixes.PersistentData;

namespace MinksMods.Tools
{
    public class Bloodnight : ConsoleCmdAbstract
    {
        public override string GetDescription()
        {
            return "Shows how many days till bloodnight.";
        }

        public override string GetHelp()
        {
            return "Bloodnight \n" +
                   "Usage:\n" +
                   "   Bloodnight \n";
        }

        public override string[] GetCommands()
        {
            return new[] { "Bloodnight", string.Empty };
        }

        public override int DefaultPermissionLevel
        {
            get { return 0; }
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                ClientInfo ci = _senderInfo.RemoteClientInfo;
                if (ci != null)
                {
                    //todo: implement
                    // HOT TO GET THE FUCKING GAME TIME ?!?
                    if (SdtdConsole.Instance != null)
                    {
                        SdtdConsole.Instance.Output("Not implemented yet.");
                    }
                }
            }
            catch (Exception Ex)
            {
                SdtdConsole.Instance.Output(Ex.ToString());
            }
        }
    }
}
