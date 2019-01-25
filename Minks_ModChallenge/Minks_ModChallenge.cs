/*
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    .
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    .
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
    .
    Written in Jan 2019 by Kai Sassmannshausen <minkio@sassie.de>
    .
    Feature Ideas:
    * (global) Scorboard
    * 
*/
#define DEBUG

using System;
using System.Collections.Generic;
using AllocsFixes.PersistentData;

namespace MinksMods.ModChallenge
{
    public static class ModChallenge
    {
        public static List<Challenge> Challenges = new List<Challenge>();
        public static int counter = 0;

        // settings
        public static int request_timeout = 2;      // minutes (default 15)a
        public static int challenge_timeout = 3;    // minutes (default 45)
        public static int info_interval = 1;        // minutes (default 3)
                                                    // ---

#if DEBUG
        public static string mySteamID = "76561197981703289";
#endif

        public static void AddChallenge(Challenge c)
        {
            Challenges.Add(c);
        }

        public static void DelChallenge(Challenge c)
        {
            Challenges.Remove(c);
        }

        public static void EntityKilled(Entity p1, Entity p2)
        {
            foreach (KeyValuePair<string, Player> kvp in PersistentContainer.Instance.Players.Dict)
            {
                Player p = kvp.Value;
                ClientInfo receiver = ConsoleHelper.ParseParamIdOrName(kvp.Key);

                if (receiver == null)
                    return;

                #if DEBUG
                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "foobar", "blarg", false, null));
                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, p1.ToString(), "blarg", false, null));
                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, p1.GetType().ToString(), "blarg", false, null));
                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, p2.ToString(), "blarg", false, null));
                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, p2.GetType().ToString(), "blarg", false, null));
                #endif
            }
        }

#if DEBUG
        public static void print_debug(string text)
        {
            ClientInfo mink = ConsoleHelper.ParseParamIdOrName(ModChallenge.mySteamID);
            mink.SendPackage(new NetPackageChat(EChatType.Whisper, -1, text, "debug", false, null));
        }
#endif
    }

    [Serializable]
    public class Challenge
    {
        public enum stages
        {
            requested,
            accepted,
            running,
            over
        };

        private int id;
        private stages stage;
        private string requester, receiver;
        private DateTime time = new DateTime();

        public Challenge(string _requester, string _receiver, int _id)
        {
            stage = stages.requested;
            requester = _receiver;
            receiver = _receiver;
            time = DateTime.Now;
            id = _id;
        }

        public string Requester
        {
            get { return requester; }
        }

        public string Receiver
        {
            get { return receiver; }
        }

        public stages Stage
        {
            get { return stage; }
        }

        public DateTime Time
        {
            get { return time; }
        }

        public void Accept()
        {
            //stage = stages.accepted;
            stage = stages.running;
            time = DateTime.Now;
            //toto: start "game start timer"
        }

        public bool IsTimedOut()
        {
            TimeSpan age = new TimeSpan(DateTime.Now.Subtract(time).Ticks);
            
            if (stage == stages.requested && age.TotalMinutes > TimeSpan.FromMinutes(ModChallenge.request_timeout).TotalMinutes)
            {
                return true;
            }
            else if (stage == stages.running && age > TimeSpan.FromMinutes(ModChallenge.challenge_timeout))
            {
                return true;
            }

            return false;
        }

    }

    public class ChallengeCmd : ConsoleCmdAbstract
    {
        public override string GetDescription()
        {
            return "Send out and accept challenges.";
        }

        public override string GetHelp()
        {
            return "Challenge \n" +
                   "Usage:\n" +
                   "  Challenge [player] [accept|revoke] \n\n" +
                   "Examples:\n" +
                   "  You use:  Challenge Joe \n" +
                   "  Joe uses: Challenge You accept \n\n" +
                   "  Challenge without any parameters shows your current challenge invites\n" +
                   "  To revoke a challenge: Challenge Joe revoke \n";
        }

        public override string[] GetCommands()
        {
            return new[] { "Challenge", "c" };
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; }
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                CommandSenderInfo senderinfo = _senderInfo;
                string playerID = senderinfo.RemoteClientInfo.playerId;
                List<Challenge> player_challenges = new List<Challenge>();

                foreach (Challenge c in ModChallenge.Challenges)
                {
                    if ((c.Receiver == playerID || c.Requester == playerID) && c.Stage != Challenge.stages.over)
                    {
                        player_challenges.Add(c);
                    }
                }

                switch (_params.Count)
                {
                    // no parameter - show requested/running challenges
                    case 0:

                        if (player_challenges.Count > 0)
                        {
                            foreach (Challenge c in player_challenges)
                            {
                                if (c.IsTimedOut())
                                {
                                    ModChallenge.DelChallenge(c);
                                    continue;
                                }

                                if (c.Stage == Challenge.stages.requested)
                                {
                                    SdtdConsole.Instance.Output("Open challenge request: " + c.Requester + " challenged " + c.Receiver + " at " + c.Time.ToString() + ".");
                                }
                                else if (c.Stage == Challenge.stages.running)
                                {
                                    SdtdConsole.Instance.Output("Running challenge: " + c.Requester + " vs " + c.Receiver + ". Started at " + c.Time.ToString() + ".");
                                }
                            }
                        }  
                        else
                        {
                            SdtdConsole.Instance.Output("No open challenges found.");
                        }
                        break;

                    // 1 parameter - invite for a challenge
                    case 1:

                        // string receiver = _params[0];

                        if (player_challenges.Count > 0)
                        {
                            foreach (Challenge c in player_challenges)
                            {
                                if (c.Stage == Challenge.stages.running || c.Stage == Challenge.stages.accepted)
                                {
                                    SdtdConsole.Instance.Output("You can not do that while you are in an accepted or running challenge.");
                                    return;
                                }
                            }
                        }
                        else
                        {
                            ClientInfo receiver = ConsoleHelper.ParseParamPlayerName(_params[0], true, true);
                            if (receiver != null)
                            {
                                ModChallenge.AddChallenge(new Challenge(playerID, receiver.playerId, ++ModChallenge.counter));
                                SdtdConsole.Instance.Output("You challanged " + receiver.playerName + ".");
                            }
                            else
                            {
                                SdtdConsole.Instance.Output("No such player.");
                            }

                        }
                       
                        break;

                    // 2 parameter - accept or revoke challenge
                    case 2:

                        if (_params[1] == "accept" || _params[1] == "revoke")
                        {
                            foreach (Challenge c in player_challenges)
                            {
                                if (c.Stage == Challenge.stages.requested)
                                {
                                    if (c.Receiver == playerID && _params[1] == "accept")
                                    {
                                        if (!c.IsTimedOut())
                                        {
                                            c.Accept();
                                            SdtdConsole.Instance.Output("You accepted the challenge.");
                                            return;
                                        }
                                        else
                                        {
                                            SdtdConsole.Instance.Output("This challenge request has been timed out.");
                                            ModChallenge.DelChallenge(c);
                                            return;
                                        }
                                    }
                                    else if (c.Requester == playerID && _params[1] == "revoke")
                                    {
                                        SdtdConsole.Instance.Output("You revoked the challenge.");
                                        ModChallenge.DelChallenge(c);
                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            SdtdConsole.Instance.Output("Second parameter must be 'accept' or 'revoke'.\n" + GetHelp());
                        }

                        break;


                    default:
                        SdtdConsole.Instance.Output(GetHelp());
                        break;
                }
            }
            catch (Exception Ex)
            {
                SdtdConsole.Instance.Output(Ex.ToString());
            }
        }
    }


    public class API : IModApi
    {
        public void InitMod()
        {
            ModEvents.EntityKilled.RegisterHandler(EntityKilled);
            ModEvents.ChatMessage.RegisterHandler(ChatMessage);
        }
        public void EntityKilled(Entity a, Entity b)
        {
            try
            {
                // while developing its enuf if the killer is a player (not killer _and_ victim must be a player)
#if DEBUG
                if (isPlayer(a) || isPlayer(b))
#else
                if (isPlayer(a) && isPlayer(b))
#endif
                {
                    ModChallenge.EntityKilled( a, b );
                }
            }
            catch(Exception Ex)
            {
                Log.Exception(Ex);
            }
        }

        public bool isPlayer(Entity e)
        {
            if ( e != null && e.GetType() == typeof(EntityPlayer) )
            {
                return true;
            }
            
            return false;
        }

        public bool ChatMessage(ClientInfo _cInfo, EChatType _type, int _senderId, string _msg, string _mainName, bool _localizeMain, List<int> _recipientEntityIds)
        {
            if (string.IsNullOrEmpty(_msg) || !_msg.EqualsCaseInsensitive("/mink"))
            {
                return true;
            }

            if (_cInfo != null)
            {
                Log.Out("Sent chat hook reply to {0}", _cInfo.playerId);
                _cInfo.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "Thats the author of ModChallenge!", "", false, null));
            }
            else
            {
                Log.Error("ChatHookExample: Argument _cInfo null on message: {0}", _msg);
            }

            return false;
        }
    }
}
