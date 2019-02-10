#define RELEASE

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
                   "  Challenge [ <player_name> | giveu p]  [ accept | cancel ] \n\n" +
                   "Alias: \n" +
                   " C  [ <player_name> | giveup ]  [ accept | cancel ] \n\n" +
                   "Simple Examples:\n" +
                   "  You use:  \"Challenge Joe\" \n" +
                   "  Joe uses: \"Challenge YourName accept\" \n\n" +
                   "  Challenge without any parameters shows your current challenge invites. \n" +
                   "  To withdraw a challenge invite: \"Challenge JoeFarmer cancel\" (can only be done before challenge started) \n" +
                   "  To deny a challenge request: \"Challenge RequesterName cancel\" \n" +
                   "  To giveup a challenge: \"Challenge giveup\" (can only be done in a running challange) \n\n" +
                   "If you want to challenge a user with a space in its name, use \"Some Name\".\n" +
                   "An unanswerd challenge invite will time out in " + ModChallenge.request_duration.ToString() + " minutes.\n";
        }

        public override string[] GetCommands()
        {
            return new[] { "Challenge", "c" };
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; }
        }

        public void ChallengeInfos(string playerID, out List<Challenge>players_challenges, out Dictionary<string, string>bussy_players)
        {
            players_challenges = new List<Challenge>();
            bussy_players = new Dictionary<string, string>();

            // use a copy of the list (ToArray()) to be able to modify the original (delete timed out requests)
            foreach (Challenge c in ModChallenge.Challenges.ToArray())
            {
                // clear out timedout requests
                if (c == null || (c.IsTimedOut() && c.Stage == Challenge.stages.requested))
                {
                    ModChallenge.DelChallenge(c);
                    continue;
                }

                // build new List with only current challenges where command issuer is part of
                if ((c.Receiver == playerID || c.Requester == playerID) && c.Stage != Challenge.stages.over)
                {
                    // do not include timed out requests
                    if (!(c.IsTimedOut() && c.Stage == Challenge.stages.requested))
                    {
                        players_challenges.Add(c);
                    }
                }

                // build a list/dict of bussy players
                if (c.Stage == Challenge.stages.running || c.Stage == Challenge.stages.accepted)
                {
                    if (!bussy_players.ContainsKey(c.Receiver))
                    {
                        bussy_players.Add(c.Receiver, c.Handler.rec_ci.playerName);
                    }

                    if (!bussy_players.ContainsKey(c.Requester))
                    {
                        bussy_players.Add(c.Requester, c.Handler.req_ci.playerName);
                    }
                }
            }
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                CommandSenderInfo senderinfo = _senderInfo;
                string playerID = senderinfo.RemoteClientInfo.playerId;

                // all active challenges the player is involved
                List<Challenge> players_challenges;
                // All players with running or accepted challenge: Dict< playerID, playerName >
                Dictionary<string, string> bussy_players;

                ChallengeInfos(playerID, out players_challenges, out bussy_players);

#if DEBUG
                Log.Out("ModChallengeDebug: players_challenges.Count: " + players_challenges.Count);
#endif

                switch (_params.Count)
                {
                    // no parameter - show requested/running challenges
                    case 0:

                        ShowChallengesOnConsole(playerID);
                        break;

                    // 1 parameter - [ <PlayerNameToInvite> | "giveup" ]
                    case 1:

                        Invite_and_GiveUp_Handler(playerID, senderinfo.RemoteClientInfo.playerName, _params, bussy_players, players_challenges);
                        break;

                    // 2 parameter: <PlayerName> [ accept | cancel | revoke | withdraw ]
                    case 2:

                        Accept_and_deny_Handler(playerID, _params, players_challenges, bussy_players);
                        break;

                    default:
                        SdtdConsole.Instance.Output(GetHelp());
                        break;
                }
            }
            catch (Exception Ex)
            {
                SdtdConsole.Instance.Output("Something wicked happened. Check logs.");
                Log.Exception(Ex);
            }
        }


        public void Accept_and_deny_Handler(string playerID, List<string> _params, List<Challenge> players_challenges, Dictionary<string, string> bussy_players)
        {
            // none of the 2 parameter option is allowed to be executed while in a running challenge.
            if (bussy_players.ContainsKey(playerID))
            {
                if (SdtdConsole.Instance != null)
                {
                    SdtdConsole.Instance.Output("You can not do that while in a running or accepted challenge.");
                }
                return;
            }

            // check first parameter for valid player name
            ClientInfo receiver = ConsoleHelper.ParseParamPlayerName(_params[0], true, true);
            if (receiver == null)
            {
                if (SdtdConsole.Instance != null)
                {
                    SdtdConsole.Instance.Output("No such user: " + _params[0]);
                }
                return;
            }

            // check second parameter for valid options
            if (_params[1] == "accept" || _params[1] == "cancel" || _params[1] == "revoke" || _params[1] == "withdraw" || _params[1] == "deny")
            {
                foreach (Challenge c in players_challenges)
                {
                    // accept handling
                    if (c.Stage == Challenge.stages.requested)
                    {
                        if (c.Receiver == playerID && _params[1] == "accept")
                        {
                            if (bussy_players.ContainsKey(c.Receiver))
                            {
                                if (SdtdConsole.Instance != null)
                                {
                                    SdtdConsole.Instance.Output(c.Handler.rec_ci.playerName + " is already in a running challenge. Aborted.");
                                }
                                c.Handler.rec_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + c.Handler.rec_ci.playerName + " is already in a running challenge. Aborted.[-]", "", false, null));
                                return;
                            }

                            // all checks passed, accept the challenge
                            c.Accept();
                            if (SdtdConsole.Instance != null)
                            {
                                SdtdConsole.Instance.Output("You accepted the challenge.");
                            }
                            c.Handler.rec_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]You accepted a challenge vs "+ c.Handler.req_ci.playerName + "![-]", "", false, null));
                            c.Handler.req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + c.Handler.rec_ci.playerName + " accepted your challenge![-]", "", false, null));
                            c.Handler.SendSoundPackage(c.Handler.rec_ci, ModChallenge.SoundEvents.accepted);
#if !DEBUG
                            c.Handler.SendSoundPackage(c.Handler.req_ci, ModChallenge.SoundEvents.accepted);
#endif
                            return;

                        }
                        // todo: invalid cancel message
                        else if (_params[1] == "cancel" || _params[1] == "revoke" || _params[1] == "withdraw" || _params[1] == "deny")
                        {
                            if (c.Requester == playerID)
                            {
                                if (SdtdConsole.Instance != null)
                                {
                                    SdtdConsole.Instance.Output("You have withdrawn the challenge invite.");
                                }
                                c.Handler.req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]You withdrawn the challenge invite.[-]", "", false, null));
                                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + c.Handler.req_ci.playerName + " has withdrawn the challenge invite![-]", "", false, null));
                            }
                            else if (c.Receiver == playerID)
                            {
                                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]You denied the challenge invite.[-]", "", false, null));
                                c.Handler.req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + c.Handler.rec_ci.playerName + " has denied the challenge invite![-]", "", false, null));
                            }

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
                if (SdtdConsole.Instance != null)
                {
                    SdtdConsole.Instance.Output("Valid second paramerers are: accept, cancel (and the following aliases for canel as well: revoke, withdraw, deny). \n" + GetHelp());
                }
                return;
            }
        }


        public void Invite_and_GiveUp_Handler(string IssuerID, string IssuerName, List<string> _params, Dictionary<string, string> bussy_players, List<Challenge> players_challenges)
        {
            if (_params[0].ToLower() == IssuerName.ToLower())
            {
#if DEBUG
                if (SdtdConsole.Instance != null)
                {
                    SdtdConsole.Instance.Output("Debug mode. You can challenge yourself.");
                }
#else
                if (SdtdConsole.Instance != null)
                {
                    SdtdConsole.Instance.Output("You can not challenge yourself.");
                }
                return;
#endif
            }

            foreach (Challenge c in players_challenges)
            {
                // giveup
                if (_params[0] == "giveup" && (c.Stage == Challenge.stages.running || c.Stage == Challenge.stages.accepted))
                {
                    if (IssuerID == c.Receiver)
                    {
                        if (SdtdConsole.Instance != null)
                        {
                            SdtdConsole.Instance.Output("You gave up.");
                        }
                        c.Handler.req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + c.Handler.rec_ci.playerName +" gave up.[-]", "", false, null));
                        c.Handler.Win(c.Handler.req_ci);
                        return;
                    }
                    else if (IssuerID == c.Requester)
                    {
                        if (SdtdConsole.Instance != null)
                        {
                            SdtdConsole.Instance.Output("You gave up.");
                        }
                        c.Handler.rec_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + c.Handler.req_ci.playerName + " gave up.[-]", "", false, null));
                        c.Handler.Win(c.Handler.rec_ci);
                        return;
                    }
                }

                // lets use this iteration to also check if there is already an invite for that player
                if (c.Stage == Challenge.stages.requested)
                {
                    if (c.Handler.rec_ci.playerName.ToLower() == _params[0].ToLower())
                    {
                        if (SdtdConsole.Instance != null)
                        {
                            SdtdConsole.Instance.Output("You already invited " + _params[0] + " for a challenge.");
                        }
                        return;
                    }
                    else if (c.Handler.req_ci.playerName.ToLower() == _params[0].ToLower())
                    {
                        if (SdtdConsole.Instance != null)
                        {
                            SdtdConsole.Instance.Output("No need. You have an active invitation from " + _params[0] + ". Just accept it.");
                        }
                        return;
                    }
                }
            }

            // no challenge invite while in a running or accepted challenge
            if (bussy_players.ContainsKey(IssuerID))
            {
                if (SdtdConsole.Instance != null)
                {
                    SdtdConsole.Instance.Output("You can not challenge someone while in an accepted or running challenge.");
                }
                return;
            }

            // invite vor challenge: search name from user input, validate and create a new challenge (+send out challenge request)
            ClientInfo receiver = ConsoleHelper.ParseParamPlayerName(_params[0], true, true);
            if (receiver != null)
            {
                Log.Out(receiver.ToString());

                // check if challenge receiver is already in an accepted or running challenge
                if (bussy_players.ContainsKey(receiver.playerId))
                {
                    if (SdtdConsole.Instance != null)
                    {
                        SdtdConsole.Instance.Output(_params[0] + " is in a running or shortly starting challenge and can not be challenged again until the current challenge is over.");
                    }
                    return;
                }

                Challenge c = new Challenge(IssuerID, receiver.playerId, ++ModChallenge.counter);
                ModChallenge.AddChallenge(c);
                if (SdtdConsole.Instance != null)
                {
                    SdtdConsole.Instance.Output("You challanged " + receiver.playerName + ".");
                }

                c.Handler.req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]You challenged " + receiver.playerName + ".[-]", "", false, null));
                receiver.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]You were challenged by " + c.Handler.req_ci.playerName + ".[-]", "", false, null));

                c.Handler.SendSoundPackage(c.Handler.rec_ci, ModChallenge.SoundEvents.invite);
                c.Handler.SendSoundPackage(c.Handler.req_ci, ModChallenge.SoundEvents.invite);
            }
            else
            {
                if (SdtdConsole.Instance != null)
                {
                    SdtdConsole.Instance.Output("No such player.");
                }
            }
        }


        public void ShowChallengesOnConsole(string playerID)
        {
            bool found_one = false;

            // every player can see the table of requests and running challenges. thats why we use the original ModChallenge.Challenges List and not the copyed one (players_challenges)
            foreach (Challenge c in ModChallenge.Challenges)
            {
                if (c.Handler == null || c.Handler.rec_ci == null || c.Handler.req_ci == null)
                {
                    continue;
                }

                if (c.Stage == Challenge.stages.requested)
                {
                    SdtdConsole.Instance.Output("Open challenge request: " + c.Handler.req_ci.playerName + " challenged " + c.Handler.rec_ci.playerName + " at " + c.Time.ToString() + ".");
                    found_one = true;
                }
                else if (c.Stage == Challenge.stages.running)
                {
                    SdtdConsole.Instance.Output("Running challenge: " + c.Handler.req_ci.playerName + " vs " + c.Handler.rec_ci.playerName + ". Started at " + c.Time.ToString() + ".");
                    found_one = true;
                }
                else if (c.Stage == Challenge.stages.accepted)
                {
                    found_one = true;
                    if (c.Receiver == playerID || c.Requester == playerID)
                    {
                        SdtdConsole.Instance.Output("Your challenge vs " + ((playerID == c.Receiver) ? c.Handler.req_ci.playerName : c.Handler.rec_ci.playerName) + " will start soon.");
                    }
                    else
                    {
                        SdtdConsole.Instance.Output("Challenge will start soon: " + c.Handler.req_ci.playerName + " vs " + c.Handler.rec_ci.playerName);
                    }

                }
            }

            if (!found_one)
            {
                SdtdConsole.Instance.Output("No challenges found.");
            }
        }
    }

    public class ChatCommand
    {
        // all active challenges the player is involved
        List<Challenge> players_challenges;
        // All players with running or accepted challenge: Dict< playerID, playerName >
        Dictionary<string, string> bussy_players;

        public ChatCommand(string chatmsg, ClientInfo Issuer)
        {
            if (Issuer == null || string.IsNullOrEmpty(chatmsg))
            {
                return;
            }

            String[] Command = chatmsg.Split(' ');

            // just 3 parameters allowed
            if (Command.Length > 3)
            {
                return;
            }

            for (int i=1; i < Command.Length; i++)
            {
                // 2 parameter: invite or giveup
                if (i == 1 && Command.Length == 2)
                {
                    new ChallengeCmd().Invite_and_GiveUp_Handler(Issuer.playerId, Issuer.playerName,GetParameter(Issuer.playerId, Command), bussy_players, players_challenges);
                }

                // 3 parameter: accept or deny
                else if (i == 2 && Command.Length == 3)
                {
                    new ChallengeCmd().Accept_and_deny_Handler(Issuer.playerId, GetParameter(Issuer.playerId, Command), players_challenges, bussy_players);
                }
            }
        }

        public List<string> GetParameter(string _IssuerID, string[] _command)
        {
            new ChallengeCmd().ChallengeInfos(_IssuerID, out players_challenges, out bussy_players);
            List<string> Cmd = new List<string>(_command);
            Cmd.Remove("/c");
            Cmd.Remove("/challenge");
            return Cmd;
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

                if (ModChallenge.Challenges.Count > 0)
                {
                    foreach (Challenge c in ModChallenge.Challenges)
                    {
                        SdtdConsole.Instance.Output("ID: " + c.ID.ToString() +
                                                    " Date: " + c.Time.ToString() +
                                                    " Requester: " + c.Handler.req_ci.playerName + " (" + c.Requester + ")" +
                                                    " Receiver: " + c.Handler.rec_ci.playerName + " (" + c.Receiver + ")" +
                                                    " Stage: " + c.Stage.ToString() +
                                                    " Winner: " + c.Winner.ToString());
                    }
                }
                else
                {
                    SdtdConsole.Instance.Output("No challenges found.");
                    return;
                }
            }
            catch (Exception Ex)
            {
                SdtdConsole.Instance.Output(Ex.ToString());
            }
        }
    }
}