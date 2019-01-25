using System;
using System.Collections.Generic;
using AllocsFixes.PersistentData;


namespace MinksMods.ModChallenge
{
    public static class ModChallenge
    {
        public static List<Challenge> Challenges = new List<Challenge>();

        public static void EntityKilled(Entity p1, Entity p2)
        {
            foreach (KeyValuePair<string, Player> kvp in PersistentContainer.Instance.Players.Dict)
            {
                Player p = kvp.Value;
                ClientInfo receiver = ConsoleHelper.ParseParamIdOrName(kvp.Key);

                if (receiver == null)
                    return;

                // debug
                receiver.SendPackage(new NetPackageGameMessage(EnumGameMessages.PlainTextLocal, "a killed b", "ModChallenge", false, "", false));
                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "foobar", "blarg", false, null));

                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, p1.ToString(), "blarg", false, null));
                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, p1.GetType().ToString(), "blarg", false, null));
                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, p2.ToString(), "blarg", false, null));
                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, p2.GetType().ToString(), "blarg", false, null));
            }
        }
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

        private int id = 0;
        private stages stage;
        private string requester, receiver;
        private DateTime time = new DateTime();

        public Challenge(string _requester, string _receiver)
        {
            stage = stages.requested;
            requester = _receiver;
            receiver = _receiver;
            time = DateTime.MinValue;
            id = 1;
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

                switch (_params.Count)
                {
                    // no parameter - show requested/running challenges
                    case 0:
                        bool foundone = false;
                        foreach (Challenge c in ModChallenge.Challenges)
                        {
                            if (c.Requester == playerID || c.Receiver == playerID)
                            {
                                if (c.Stage == Challenge.stages.requested)
                                {
                                    SdtdConsole.Instance.Output("Open challenge request: " + c.Requester + " challenged " + c.Receiver + " at " + c.Time.ToString() + ".");
                                    foundone = true;
                                }
                                else if (c.Stage == Challenge.stages.running)
                                {
                                    SdtdConsole.Instance.Output("Running challenge: " + c.Requester + " vs " + c.Receiver + ". Started at " + c.Time.ToString() + ".");
                                    foundone = true;
                                }
                            }
                        }
                        if (!foundone)
                        {
                            SdtdConsole.Instance.Output("No open challenges found.");
                        }
                        break;

                    // 1 parameter - invite for a challenge
                    case 1:
                        break;

                    // 2 parameter - accept or revoke challenge
                    case 2:
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
                // debug todo -> replace || with && for production
                if (isPlayer(a) || isPlayer(b))
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
