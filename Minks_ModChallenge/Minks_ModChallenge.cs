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
    * use markers
    * commands also work from chatwindow
    * save challanges in history class
    * giveup/cancel command
    * time left ticker
    * 
    Known Issues:
    * Start fresh server with this mod: 1. start will crash (directory does not exist)
    * 
*/
#define RELEASE

using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Threading;
//using UnityEngine;
using UnityEngine.Audio;
using AllocsFixes.PersistentData;

namespace MinksMods.ModChallenge
{
    public delegate void TimerCallback(object state);

    public static class ModChallenge
    {
        public static List<Challenge> Challenges = new List<Challenge>();
        public static string filepath;
        public static int counter = 0;

        // settings
        public static int request_duration;                 // minutes (default 15)
        public static int challenge_duration;               // minutes (default 45)
        public static int info_interval;                    // seconds (default 10)
        public static string message_color = "ff0000";      // rgb
        // ---

//#if DEBUG
        public static string mySteamID = "76561197981703289";
//#endif

        public static void init()
        {
            filepath = GameUtils.GetSaveGameDir() + Path.DirectorySeparatorChar;
            LoadSettingsFromXml();
            Log.Begin();
#if DEBUG
            Log.Out("Warning! ModChallange loaded in Debug Mode!");
#endif
        }


        public static void LoadSettingsFromXml()
        {
            XmlDocument doc = new XmlDocument();
            string configfile = Path.Combine(filepath, "Minks_ModChallenge.xml");
            if (File.Exists(configfile))
            {
                doc.Load(configfile);

                XmlNode setting_request_duration, setting_challenge_timeout, setting_info_inverval;
                XmlElement root = doc.DocumentElement;

                setting_request_duration = root.SelectSingleNode("RequestDuration");
                setting_challenge_timeout = root.SelectSingleNode("ChallengeDuration");
                setting_info_inverval = root.SelectSingleNode("InfoInterval");

                if (Int32.TryParse(setting_request_duration.InnerText, out request_duration) && 
                    Int32.TryParse(setting_challenge_timeout.InnerText, out challenge_duration) && 
                    Int32.TryParse(setting_info_inverval.InnerText, out info_interval)
                    )
                {
                    Log.Out("ModChallenge: Minks_ModChallenge.xml loaded.");
                    return;
                }
            }
            else
            {
                WriteSettingsFile(configfile);
            }

            // xml load failed, restoring defaults
            request_duration = 15;
            challenge_duration = 45;
            info_interval = 10;
            Log.Out("ModChallenge: Error loading Minks_ModChallenge.xml. Using defaults.");
        }

        public static void WriteSettingsFile(string _xmlfile)
        {
            try
            {
                string content = "<?xml version='1.0'?>\n" +
                                "<MinksModChallenge>\n\n" +
                                "\t<!-- in Minutes (default 15) -->\n" +
                                "\t<RequestDuration> 15 </RequestDuration>\n\n" +
                                "\t<!-- in Minutes (default 45) -->\n" +
                                "\t<ChallengeDuration> 45 </ChallengeDuration>\n\n" +
                                "\t<!-- in Seconds (default 10) -->\n" +
                                "\t<InfoInterval> 10 </InfoInterval>\n\n" +
                                "</MinksModChallenge>\n";

                using (StreamWriter writer = new StreamWriter(_xmlfile))
                {
                    writer.Write(content);
                }
            }
            catch (Exception Ex)
            {
                Log.Error("Exception in writing file " + _xmlfile + ".");
                Log.Exception(Ex);
            }

        }

        public static void AddChallenge(Challenge c)
        {
            if (c != null)
            {
                Challenges.Add(c);
            }
        }

        public static void DelChallenge(Challenge c)
        {
            if (c != null)
            {
                Challenges.Remove(c);
            }
        }

        // is only called if at least one player is involved
        public static void EntityKilled(Entity loser, Entity winner)
        {
            print_debug("winner: " + loser.ToString());
            print_debug("loser: " + winner.ToString());

            if (loser == null || winner == null)
            {
                return;
            }

            foreach (Challenge c in Challenges)
            {
                if (c.Stage == Challenge.stages.running)
                {
                    ClientInfo winner_ci = ConsoleHelper.ParseParamEntityIdToClientInfo(loser.belongsPlayerId.ToString());
                    ClientInfo loser_ci = ConsoleHelper.ParseParamEntityIdToClientInfo(winner.belongsPlayerId.ToString());

                    if (winner_ci == null || loser_ci == null)
                    {
                        return;
                    }

                    /* Option1: if one challenger dies (for any reason), the other one wins */
                    if (loser_ci.playerId == c.Receiver)
                    {
                        c.Handler.Win(c.Handler.req_ci);
                    }
                    else if (loser_ci.playerId == c.Requester)
                    {
                        c.Handler.Win(c.Handler.rec_ci);
                    }

                    /* Option 2: challenger must kill other challenger to win
                    if ((winner_ci.playerId == c.Receiver || winner_ci.playerId == c.Requester) && (loser_ci.playerId == c.Receiver || loser_ci.playerId == c.Requester ))
                    {
                        c.Handler.Win(winner_ci);
                    }
                    */
                }
            }
        }

        public static void PlayerDisconnected(ClientInfo _cInfo)
        {
            if (_cInfo == null)
            {
                return;
            }

            foreach (Challenge c in Challenges)
            {
                if (c.Receiver == _cInfo.playerId || c.Receiver == _cInfo.playerId)
                {
                    c.DisconnectFromRunningChallenge(_cInfo);
                    return;
                }
            }
        }

        public enum SoundEvents
        {
            invite,
            accepted,
            revoked,
            start,
            won,
            lost,
            draw,
            info,
            gunshot
        }

        // sound strings defined in 7daysfolder/Data/Config/sounds.xml
        public static Dictionary<SoundEvents, string> Sounds = new Dictionary<SoundEvents, string>()
            {
                { SoundEvents.invite    , "quest_note_offer" },
                { SoundEvents.accepted  , "quest_subtask_complete" },
                { SoundEvents.revoked   , "quest_note_decline" }, // password_fail, recipe_unlocked, SlowSwoosh, wellness_increase
                { SoundEvents.start     , "quest_started" },
                { SoundEvents.won       , "quest_master_complete" },
                { SoundEvents.lost      , "quest_failed" },
                { SoundEvents.draw      , "quest_failed" },
                { SoundEvents.info      , "swoosh" },
                { SoundEvents.gunshot   , "44magnum_fire" }
            };

//#if DEBUG
        public static void print_debug(string text)
        {
            ClientInfo mink = ConsoleHelper.ParseParamIdOrName(ModChallenge.mySteamID);
            mink.SendPackage(new NetPackageChat(EChatType.Whisper, -1, text, "debug", false, null));
        }
//#endif

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

        public enum winoptions
        {
            none,
            requester,
            receiver
        }

        private int id;
        private stages stage;
        private winoptions winner;
        private string requester, receiver;
        private DateTime time = new DateTime();
        private ChallengeHandler handler;

        public Challenge(string _requester, string _receiver, int _id)
        {
            stage = stages.requested;
            requester = _requester;
            receiver = _receiver;
            time = DateTime.Now;
            id = _id;
            winner = winoptions.none;
            handler = new ChallengeHandler(this);
        }

        public int ID
        {
            get { return id; }
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

        public winoptions Winner
        {
            get { return winner; }
            set { winner = value;  }
        }

        public DateTime Time
        {
            get { return time; }
        }

        public ChallengeHandler Handler
        {
            get { return handler; }
        }

        public void Accept()
        {
            stage = stages.accepted;
            time = DateTime.Now;

            System.Threading.Timer CallBackTimer = new System.Threading.Timer(handler.Tick);
            CallBackTimer.Change(5000, 0);
        }

        public void Start()
        {
            stage = stages.running;
            time = DateTime.Now;
        }

        public void End()
        {
            stage = stages.over;
            // kills timer
            handler.clean();
        }

        public bool IsTimedOut()
        {
            TimeSpan age = new TimeSpan(DateTime.Now.Subtract(time).Ticks);
            
            if (stage == stages.requested && age.TotalMinutes > TimeSpan.FromMinutes(ModChallenge.request_duration).TotalMinutes)
            {
                return true;
            }
            else if (stage == stages.running && age > TimeSpan.FromMinutes(ModChallenge.challenge_duration))
            {
                return true;
            }

            return false;
        }

        public void DisconnectFromRunningChallenge(ClientInfo _cInfo)
        {
            if (_cInfo == null)
            {
                return;
            }

            if (_cInfo.playerId == receiver)
            {
                handler.Win(handler.req_ci);
            }
            else
            {
                handler.Win(handler.rec_ci);
            }
        }
    }


    public class ChallengeHandler
    {
        private Challenge challenge;
        private System.Threading.Timer timer;
        public ClientInfo req_ci, rec_ci;
        private Player req_p, rec_p;
        private int Countdown = 3;

        public ChallengeHandler(Challenge _c)
        {
            if (_c == null)
            {
                return;
            }

            challenge = _c;

            rec_ci = ConsoleHelper.ParseParamIdOrName(challenge.Receiver);
            rec_p = PersistentContainer.Instance.Players[rec_ci.playerId, false];

            req_ci = ConsoleHelper.ParseParamIdOrName(challenge.Requester);
            req_p = PersistentContainer.Instance.Players[req_ci.playerId, false];
        }

        public void Tick(System.Object stateinfo)
        {
            timer = (System.Threading.Timer)stateinfo;

            if (challenge == null)
            {
                clean();
            }

            switch (challenge.Stage)
            {
                case Challenge.stages.accepted:

                    if (Countdown > 0)
                    {
                        ShowStartCountdown(Countdown--);

#if DEBUG
                        timer.Change(5000, 0);
#else
                        timer.Change(60000, 0);
#endif
                    }
                    else
                    {
                        challenge.Start();
                        ShowStart();
                        ShowDistances();

                        SendSoundPackage(rec_ci, ModChallenge.SoundEvents.start);
#if DEBUG
                        timer.Change(5000, 0);
#else
                        timer.Change(60000, 0);
                        SendSoundPackage(req_ci, ModChallenge.SoundEvents.start);
#endif
                    }
                    break;

                case Challenge.stages.running:

                    if (challenge.IsTimedOut())
                    {
                        ShowDraw();
                        SendSoundPackage(rec_ci, ModChallenge.SoundEvents.start);
#if !DEBUG
                        SendSoundPackage(req_ci, ModChallenge.SoundEvents.start);
#endif
                        challenge.End();
                    }
                    else
                    {
                        ShowDistances();
                        SendSoundPackage(rec_ci, ModChallenge.SoundEvents.info);
#if DEBUG
                        timer.Change(5000, 0);

#else
                        timer.Change((int)new TimeSpan(0, 0, ModChallenge.info_interval).TotalMilliseconds, 0);
                        SendSoundPackage(rec_ci, ModChallenge.SoundEvents.info);
#endif
                    }
                    break;

                default:

                    clean();
                    break;
            }
        }


        public void SendSoundPackage(ClientInfo _ci, ModChallenge.SoundEvents _se)
        {
            if (_ci == null)
            {
                return;
            }

            _ci.SendPackage(new NetPackageSoundAtPosition(rec_p.LastPosition.ToVector3(), ModChallenge.Sounds[_se] , UnityEngine.AudioRolloffMode.Linear, 10, rec_ci.entityId));
        }


        public void ShowDistances()
        {
            //other way of getting distance: Entitiy.GetDistance
#if DEBUG
            float dist = UnityEngine.Vector3.Distance(rec_p.LastPosition.ToVector3(), new UnityEngine.Vector3(0, 0));
#else
            float dist = UnityEngine.Vector3.Distance(rec_p.LastPosition.ToVector3(), req_p.LastPosition.ToVector3());
#endif
            string dir = GetDirection(req_p.LastPosition, rec_p.LastPosition);
            if (dir == "")
            {
                req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + rec_p.Name + " is " + (int)dist + "m away. In another hight.[-]", "", false, null));
            }
            else
            {
                req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + rec_p.Name + " is " + (int)dist + "m to the " + dir + ".[-]", "", false, null));
            }

            dir = GetDirection(rec_p.LastPosition, req_p.LastPosition);
            if (dir == "")
            {
                rec_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + req_p.Name + " is " + (int)dist + "m away. In another hight.[-]", "", false, null));
            }
            else
            {
                rec_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + req_p.Name + " is " + (int)dist + "m to the " + GetDirection(rec_p.LastPosition, req_p.LastPosition) + ".[-]", "", false, null));
            }
        }


        public void ShowStartCountdown(int minutes)
        {
            string text_req, text_rec = "";

            if (minutes == 3)
            {
                text_req = "[" + ModChallenge.message_color + "]Challenge vs " + rec_p.Name + " will start in " + minutes + " min. Your relative position will be revealed to your challenger.[-]";
                text_rec = "[" + ModChallenge.message_color + "]Challenge vs " + req_p.Name + " will start in " + minutes + " min. Your relative position will be revealed to your challenger.[-]";
            }
            else
            {
                text_req = "[" + ModChallenge.message_color + "]Challenge vs " + rec_p.Name + " will start in " + minutes + " min.[-]";
                text_rec = "[" + ModChallenge.message_color + "]Challenge vs " + req_p.Name + " will start in " + minutes + " min.[-]";
            }

            req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, text_req, "", false, null));
            rec_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, text_rec, "", false, null));
        }

        public void ShowStart()
        {
            req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "] Challenge vs " + rec_p.Name + " started![-]", "", false, null));
            rec_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "] Challenge vs " + req_p.Name + " started![-]", "", false, null));
        }


        public void Win(ClientInfo _ci)
        {
            ClientInfo winner = _ci;
            if (winner == null)
            {
                return;
            }

            // send message to winner/loser
            if (winner.playerId == challenge.Receiver)
            {
                challenge.Winner = Challenge.winoptions.receiver;

                req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "] You lost the challenge against " + rec_p.Name + "![-]", "", false, null));
                rec_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "] You won the challenge against " + req_p.Name + "![-]", "", false, null));

                SendSoundPackage(rec_ci, ModChallenge.SoundEvents.won);
#if !DEBUG
                SendSoundPackage(req_ci, ModChallenge.SoundEvents.lost);   
#endif
            }
            else
            {
                challenge.Winner = Challenge.winoptions.requester;
                req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "] You won the challenge against " + rec_p.Name + "![-]", "", false, null));
                rec_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "] You lost the challenge against " + req_p.Name + "![-]", "", false, null));

                SendSoundPackage(rec_ci, ModChallenge.SoundEvents.lost);
#if !DEBUG
                SendSoundPackage(req_ci, ModChallenge.SoundEvents.won);
#endif
            }

            // send a message to all players (except winner/loser)
            foreach (KeyValuePair<string, Player> kvp in PersistentContainer.Instance.Players.Dict)
            {
                Player p = kvp.Value;
                ClientInfo p_ci = ConsoleHelper.ParseParamIdOrName(kvp.Key);

                if (p_ci == null || p_ci.playerId == challenge.Receiver || p_ci.playerId == challenge.Requester)
                    continue;

                p_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "]" + winner.playerName + " won a Challange against " + ((challenge.Winner == Challenge.winoptions.receiver) ? rec_ci.playerName : req_ci.playerName) + "![-]", "", false, null));
                SendSoundPackage(p_ci, ModChallenge.SoundEvents.gunshot);
            }
        }

        public void ShowDraw()
        {
            req_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "] Challenge vs " + rec_p.Name + " timed out. Its a draw![-]", "", false, null));
            rec_ci.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "[" + ModChallenge.message_color + "] Challenge vs " + req_p.Name + " timed out. Its a draw![-]", "", false, null));
        }


        public string GetDirection(Vector3i thisone, Vector3i otherone)
        {
            string response = "";
#if DEBUG
            otherone.x = 0;
            otherone.y = 0;
            otherone.z = 0;
#endif

            if (thisone.z < otherone.z)
            {
                response = "north";
            }
            else if (thisone.z > otherone.z)
            {
                response = "south";
            }

            if (thisone.x < otherone.x)
            {
                
                response += ((response.Length > 1) ? "-" : "") + "east";
            }
            else if (thisone.x > otherone.x)
            {
                response += ((response.Length > 1) ? "-" : "") + "west";
            }

            // "" will be returned if otherones position == thisones position (hight _not_ considered).
            return response;
        }


        public void clean()
        {
            timer.Dispose();
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

                if ( _params.Count > 0 && _params[0] == "settings")
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
                    SdtdConsole.Instance.Output(c.ID.ToString() + " " + c.Time.ToString() + " " + c.Requester + " " + c.Receiver + " " + c.Stage.ToString() + " " + c.Winner.ToString() );
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
            ModEvents.PlayerDisconnected.RegisterHandler(PlayerDisconnected);
            ModChallenge.init();
        }
        public void EntityKilled(Entity a, Entity b)
        {
            try
            {
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

        public void PlayerDisconnected(ClientInfo _cInfo, bool _bShutdown)
        {
            try
            {
                Player p = PersistentContainer.Instance.Players[_cInfo.playerId, false];
                if (p != null)
                {
                    ModChallenge.PlayerDisconnected(_cInfo);
                }
            }
            catch (Exception e)
            {
                Log.Out("Error in ModChallange.PlayerDisconnected: " + e);
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
            if (string.IsNullOrEmpty(_msg) || !_msg.EqualsCaseInsensitive("/mink") || !_msg.EqualsCaseInsensitive("/c"))
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
