using System;
using System.Collections.Generic;
using AllocsFixes.PersistentData;

namespace MinksMods.ModChallenge
{
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

                                // todo: replace steamID with player name

                                if (c.Stage == Challenge.stages.requested)
                                {
                                    SdtdConsole.Instance.Output("Open challenge request: " + c.Requester + " challenged " + c.Receiver + " at " + c.Time.ToString() + ".");
                                }
                                else if (c.Stage == Challenge.stages.running)
                                {
                                    SdtdConsole.Instance.Output("Running challenge: " + c.Requester + " vs " + c.Receiver + ". Started at " + c.Time.ToString() + ".");
                                }
                                else if (c.Stage == Challenge.stages.accepted)
                                {
                                    SdtdConsole.Instance.Output("Your challenge will start soon.");
                                }
                            }
                        }
                        else
                        {
                            SdtdConsole.Instance.Output("No challenges found.");
                        }
                        break;

                    // 1 parameter - invite for a challenge
                    case 1:

                        if (_params[0].ToLower() == senderinfo.RemoteClientInfo.playerName.ToLower())
                        {
#if DEBUG
                            SdtdConsole.Instance.Output("Debug mode. You can challenge yourself.");
#else
                            SdtdConsole.Instance.Output("You can not challenge yourself.");
                            return;
#endif
                        }

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
                                Challenge c = new Challenge(playerID, receiver.playerId, ++ModChallenge.counter);
                                ModChallenge.AddChallenge(c);
                                SdtdConsole.Instance.Output("You challanged " + receiver.playerName + ".");

                                senderinfo.RemoteClientInfo.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]You challenged " + receiver.playerName + ".[-]", "", false, null));
                                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]You were challenged by " + senderinfo.RemoteClientInfo.playerName + ".[-]", "", false, null));

                                c.Handler.SendSoundPackage(c.Handler.rec_ci, ModChallenge.SoundEvents.invite);
                                c.Handler.SendSoundPackage(c.Handler.req_ci, ModChallenge.SoundEvents.invite);
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
                            ClientInfo receiver = ConsoleHelper.ParseParamPlayerName(_params[0], true, true);

                            foreach (Challenge c in player_challenges)
                            {
                                if (c.Stage == Challenge.stages.requested)
                                {
                                    if (c.Receiver == playerID && _params[1] == "accept")
                                    {
                                        if (!c.IsTimedOut())
                                        {
                                            if (receiver != null)
                                            {
                                                c.Accept();
                                                SdtdConsole.Instance.Output("You accepted the challenge.");
                                                senderinfo.RemoteClientInfo.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]You accepted a challenge![-]", "", false, null));
                                                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + senderinfo.RemoteClientInfo.playerName + " accepted your challenge![-]", "", false, null));
                                                c.Handler.SendSoundPackage(c.Handler.rec_ci, ModChallenge.SoundEvents.accepted);
#if !DEBUG
                                                c.Handler.SendSoundPackage(c.Handler.req_ci, ModChallenge.SoundEvents.accepted);
#endif
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            SdtdConsole.Instance.Output("This challenge request has been timed out.");
                                            //c.Handler.PlaySounds();
                                            ModChallenge.DelChallenge(c);
                                            return;
                                        }
                                    }
                                    else if (c.Requester == playerID && _params[1] == "revoke")
                                    {
                                        SdtdConsole.Instance.Output("You revoked the challenge.");
                                        senderinfo.RemoteClientInfo.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]You revoked the challenge.[-]", "", false, null));
                                        receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + senderinfo.RemoteClientInfo.playerName + " has revoked the challenge invite![-]", "", false, null));
                                        c.Handler.SendSoundPackage(c.Handler.rec_ci, ModChallenge.SoundEvents.revoked);
#if !DEBUG
                                        c.Handler.SendSoundPackage(c.Handler.req_ci, ModChallenge.SoundEvents.revoked);
#endif
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


    public class ListAllChallenges : ConsoleCmdAbstract
    {
        public override string GetDescription()
        {
            return "List challenges in all states.";
        }

        public override string GetHelp()
        {
            return "ListAllChallenges" +
                   "Usage:\n" +
                   "   ListAllChallenges \n";
        }

        public override string[] GetCommands()
        {
            return new[] { "ListAllChallenges", "lc" };
        }

        public override int DefaultPermissionLevel
        {
            get { return 0; }
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                CommandSenderInfo senderinfo = _senderInfo;
                string playerID = senderinfo.RemoteClientInfo.playerId;
                Player p = PersistentContainer.Instance.Players[playerID, false];

                if (_params.Count > 0 && _params[0] == "settings")
                {
#if DEBUG
                    SdtdConsole.Instance.Output("Debug Mode enabled.");
#else
                    SdtdConsole.Instance.Output("Release Mode");
#endif
                    SdtdConsole.Instance.Output("RequestDuration: " + ModChallenge.request_duration.ToString() + " minutes");
                    SdtdConsole.Instance.Output("ChallengeDuration: " + ModChallenge.challenge_duration.ToString() + " minutes");
                    SdtdConsole.Instance.Output("InfoInterval: " + ModChallenge.info_interval.ToString() + " seconds");
                    return;
                }

                foreach (Challenge c in ModChallenge.Challenges)
                {
                    SdtdConsole.Instance.Output(c.ID.ToString() + " " + c.Time.ToString() + " " + c.Requester + " " + c.Receiver + " " + c.Stage.ToString() + " " + c.Winner.ToString());
                }
            }
            catch (Exception Ex)
            {
                SdtdConsole.Instance.Output(Ex.ToString());
            }
        }
    }
}