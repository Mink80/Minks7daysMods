/*
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.

    Written in Jan 2019 by Kai Sassmannshausen <minkio@sassie.de>
 */

using System;
using System.Collections.Generic;
using AllocsFixes.PersistentData;

namespace MinksMods.Tools
{
    public class Servertime : ConsoleCmdAbstract
    {
        public override string GetDescription()
        {
            return "Shows the server time.";
        }

        public override string GetHelp()
        {
            return "Servertime \n" +
                   "Usage:\n" +
                   "   Servertime \n";
        }

        public override string[] GetCommands()
        {
            return new[] { "Servertime", string.Empty };
        }

        public override int DefaultPermissionLevel
        {
            get { return 500; }
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                SdtdConsole.Instance.Output(DateTime.Now.ToString());
            }
            catch (Exception Ex)
            {
                SdtdConsole.Instance.Output(Ex.ToString());
            }
        }
    }
}