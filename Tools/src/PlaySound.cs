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
    public class PlaySound : ConsoleCmdAbstract
    {
        public override string GetDescription()
        {
            return "Plays a game soundfile.";
        }

        public override string GetHelp()
        {
            return "Playsound\n" +
                   "Usage:\n" +
                   "   Playsound <sound name>  \n";
        }

        public override string[] GetCommands()
        {
            return new[] { "Playsound", string.Empty };
        }

        public override int DefaultPermissionLevel
        {
            get { return 0; }
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                ClientInfo ci = ConsoleHelper.ParseParamIdOrName(_senderInfo.RemoteClientInfo.playerId);
                Player player = PersistentContainer.Instance.Players[_senderInfo.RemoteClientInfo.playerId, false];

                if (ci == null || player == null)
                    return;

                if (_params == null || _params.Count != 1)
                {
                    if (SdtdConsole.Instance != null)
                    {
                        SdtdConsole.Instance.Output(GetHelp());
                    }
                    return;
                }

                _senderInfo.RemoteClientInfo.SendPackage(new NetPackageSoundAtPosition( player.LastPosition.ToVector3(), _params[0], UnityEngine.AudioRolloffMode.Linear, 10, ci.entityId));
            }
            catch (Exception Ex)
            {
                SdtdConsole.Instance.Output(Ex.ToString());
            }
        }
    }
}