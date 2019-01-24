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

    Feature Ideas:
    * Transportation fee (may depending on distance)
    * Allied shared destinations
    * make private/global
    * 
*/

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using AllocsFixes.PersistentData;
using System.Runtime.Serialization.Formatters.Binary;

namespace MinksMods.MinksTeleportSystem
{
    public static class TeleportSystem
    {
        //default values. will be overwritten by xml file
        public static double TeleportDelay = 15; // minutes
        public static int MaxLocations = 5; // per player (0 = unlimited)
        public static string filepath;

        public static void init()
        {
            filepath = GameUtils.GetSaveGameDir() + Path.DirectorySeparatorChar;
            TeleportDestinations.Load();
            LoadSettingsFromXml();
        }

        public static void LoadSettingsFromXml()
        {
            XmlDocument doc = new XmlDocument();
            string configfile = Path.Combine(filepath, "TeleportSystemConfig.xml");
            if (File.Exists(configfile))
            {
                doc.Load(configfile);

                XmlNode setting_maxloc, setting_teldel;
                XmlElement root = doc.DocumentElement;

                setting_maxloc = root.SelectSingleNode("MaxLocations");
                setting_teldel = root.SelectSingleNode("TeleportDelay");

                if (Int32.TryParse(setting_maxloc.InnerText, out MaxLocations) && Double.TryParse(setting_teldel.InnerText, out TeleportDelay))
                {
                    Log.Out("TeleportSystem: TeleportSystemConfig.xml loaded.");
                    return;
                }
            }
            else
            {
                WriteSettingsFile(configfile);
            }

            // xml load failed, restoring defaults
            TeleportDelay = 15;
            MaxLocations = 5;
            Log.Out("TeleportSystem: Error loading TeleportSystemConfig.xml");
        }

        public static void WriteSettingsFile(string _xmlfile)
        {
            try
            {
                string content = "<?xml version='1.0'?>\n" +
                                "<TeleportSystemConfig>\n\n" +
                                "\t<!-- in Minutes-->\n" +
                                "\t<TeleportDelay> 15 </TeleportDelay>\n\n" +
                                "\t<!-- Max teleport destinations per player -->\n" +
                                "\t<MaxLocations> 5 </MaxLocations>\n\n" +
                                "</TeleportSystemConfig>\n";

                using (StreamWriter writer = new StreamWriter(_xmlfile))
                {
                    writer.Write(content);
                }
            }
            catch (Exception Ex)
            {
                Log.Error("Exception in savinig file " + _xmlfile + ".");
                Log.Exception(Ex);
            }

        }

    }

    [Serializable]
    public class TeleportDestination
    {
        public int x, y, z;
        public string name;
        public string owner;
        public bool global;

        public TeleportDestination(string _name, Vector3i _destination, string _owner, bool _global)
        {
            this.name = _name;
            this.owner = _owner;
            this.global = _global;

            if (_destination == null || _name == null || _owner == null)
                return;

            this.x = _destination.x;
            this.y = _destination.y; 
            this.z = _destination.z;
        }

        public TeleportDestination(string _name, int _x, int _y, int _z, string _owner, bool _global)
        {
            this.name = _name;
            this.owner = _owner;
            this.global = _global;

            if (_name == null || _owner == null)
                return;

            this.x = _x;
            this.y = _y;
            this.z = _z;
        }
    }

    public static class TeleportDestinations
    {
        public static List<TeleportDestination> Destinations = new List<TeleportDestination>();
        public static string filename = "TeleportSystem.bin";

        public static void AddLocation(TeleportDestination td)
        {
            Destinations.Add(td);
            Save();
        }

        public static bool DelLocation(TeleportDestination td)
        {
            if (td == null)
                return false;

            if (Destinations.Remove(td))
            {
                Save();
                return true;                
            }

            return false;
        }

        public static bool DelLocation(string name)
        {
            foreach (TeleportDestination td in Destinations)
            {
                if (td.name == name)
                {
                    return DelLocation(td);
                }
            }
            return false;
        }

        public static List<TeleportDestination> GetLocations()
        {
            return Destinations;
        }

        public static int Count()
        {
            return Destinations.Count;
        }

        public static TeleportDestination GetLocation(string name)
        {
            if (name == null)
                return null;

            foreach (TeleportDestination td in Destinations)
            {
                if (td.name == name)
                    return td;
            }
            return null;
        }

        private static void Save()
        {
            try
            {
                string file = Path.Combine(TeleportSystem.filepath, filename);
                Stream stream = File.Open(file, FileMode.Create);
                BinaryFormatter bFormatter = new BinaryFormatter();
                bFormatter.Serialize(stream, Destinations);
                stream.Close();
            }
            catch (Exception Ex)
            {
                Log.Exception(Ex);
            }
        }

        public static void Load()
        {
            if (!File.Exists(Path.Combine(TeleportSystem.filepath, filename)))
            {
                return;
            }

            try
            {
                Stream stream = File.Open( Path.Combine(TeleportSystem.filepath, filename), FileMode.Open);
                BinaryFormatter bFormatter = new BinaryFormatter();
                Destinations = (List<TeleportDestination>)bFormatter.Deserialize(stream);
                stream.Close();
            }

            catch (Exception Ex)
            {
                Log.Error("Exception in loading file " + filename + ".");
                Log.Exception(Ex);
            }

        }
    }


    public static class LastTeleportTimes
    {
        public static Dictionary<string, DateTime> Teleportations = new Dictionary<string, DateTime>();

        public static bool IsValidTeleportRequest(string _SteamID)
        {
            string SteamID = _SteamID;

            if (LastTeleportTimes.Teleportations.ContainsKey(SteamID))
            {
                int result = (Teleportations[SteamID].AddMinutes(TeleportSystem.TeleportDelay)).CompareTo(DateTime.Now);
                if (result >= 0 )
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                // first teleport
                return true;
            }
        }

        public static void ShowTimeTillNextTeleleport(string _steamID)
        {
            string SteamID = _steamID;

            if (!IsValidTeleportRequest(SteamID))
            {
                TimeSpan duration = LastTeleportTimes.Teleportations[SteamID].AddMinutes(TeleportSystem.TeleportDelay) - DateTime.Now;

                if (duration.TotalSeconds <= 60)
                {
                    SdtdConsole.Instance.Output("You need to wait another " + duration.TotalSeconds.RoundToSignificantDigits(2) + " second(s) to be able to teleport again.");
                }
                else
                {
                    SdtdConsole.Instance.Output("You need to wait another " + duration.TotalMinutes.RoundToSignificantDigits(2) + " minute(s) to be able to teleport again.");
                }
            }
        }
    }

    public class AddTeleportDestination : ConsoleCmdAbstract
    {
        public override string GetDescription()
        {
            return "Define a teleport destination.";
        }

        public override string GetHelp()
        {
            return "Usage:\n" +
                   "AddTeleportDestination <Destination Name> [global]";

        }

        public override string[] GetCommands()
        {
            return new[] { "AddTeleportDestination", "atd", "at" };
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; }
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params[0] == null)
            {
                SdtdConsole.Instance.Output(GetHelp());
                return;
            }

            try
            {
                string name = _params[0];
                bool global = false;

                if (_params.Count == 2)
                {
                    if (_params[1] == "global")
                    {
                        global = true;
                    }
                    else
                    {
                        SdtdConsole.Instance.Output("Invalid syntax. Only keyword \"global\" is allowed as second argument.");
                        SdtdConsole.Instance.Output(GetHelp());
                        return;
                    }
                }

                CommandSenderInfo senderinfo = _senderInfo;
                string playerID = senderinfo.RemoteClientInfo.playerId;
                Player p = PersistentContainer.Instance.Players[playerID, false];

                int count = 0;
                foreach (TeleportDestination td in TeleportDestinations.GetLocations())
                {
                    if (td.name == name)
                    {
                        SdtdConsole.Instance.Output("A teleport destination with that name already exists. May its a private destination of someone else. Destination names must be unique.");
                        return;
                    }
                    if (td.owner == p.Name)
                    {
                        count++;
                    }
                }

                if (TeleportSystem.MaxLocations != 0 && count > TeleportSystem.MaxLocations )
                {
                    SdtdConsole.Instance.Output("You reached your maximum allowed number of teleport destinations (max "+ TeleportSystem.MaxLocations +"). You can not add another one. May delete an old one? Try \"ListTeleportDestinations\" and \"DelTeleportDestination\" commands.");
                    return;
                }

                Vector3i pos = p.LastPosition;

                string g = (global) ? "global" : "private";
                SdtdConsole.Instance.Output("Adding "+ g + " teleport destination " + name + " at " + pos.ToString());

                TeleportDestinations.AddLocation(new TeleportDestination(name, pos, p.Name, global));
            }
            catch (Exception ex)
            {
                SdtdConsole.Instance.Output(ex.ToString());
            }   
        }
    }

    public class DelTeleportDestination : ConsoleCmdAbstract
    {
        public override string GetDescription()
        {
            return "Delete a teleport destination.";
        }

        public override string GetHelp()
        {
            return "Usage:\n" +
                   "DelTeleportDestination <Destination Name>";

        }

        public override string[] GetCommands()
        {
            return new[] { "DelTeleportDestination", string.Empty };
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; }
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params[0] == null || _params.Count != 1)
            {
                SdtdConsole.Instance.Output(GetHelp());
                return;
            }

            try
            {
                string name = _params[0];
                TeleportDestination dest = TeleportDestinations.GetLocation(name);

                CommandSenderInfo senderinfo = _senderInfo;
                string playerID = senderinfo.RemoteClientInfo.playerId;
                Player p = PersistentContainer.Instance.Players[playerID, false];

                if (p == null)
                    return;

                if (dest == null || dest.owner != p.Name)
                {
                    SdtdConsole.Instance.Output("Destination name not found or not yours.");
                    return;
                }

                if (TeleportDestinations.DelLocation(name))
                {
                    SdtdConsole.Instance.Output("Successfully deleted teleport destination " + name);
                }

            }
            catch (Exception ex)
            {
                SdtdConsole.Instance.Output(ex.ToString());
            }
        }
    }


    public class ListTeleportDestinations : ConsoleCmdAbstract
    {
        public virtual bool ListAll()
        {
            return false;
        }
        public override string GetDescription()
        {
            return "List available teleport destinations.";
        }

        public override string GetHelp()
        {
            return "List Teleport Destinations" +
                   "Usage:\n" +
                   "   ListTeleportDestinations \n";
        }

        public override string[] GetCommands()
        {
            return new[] { "ListTeleportDestinations", "ltd", "ld" };
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
                Player p = PersistentContainer.Instance.Players[playerID, false];

                if (TeleportDestinations.Count() > 0)
                {
                    SdtdConsole.Instance.Output("Global Teleport Destinations:");
                    SdtdConsole.Instance.Output("Name \t\t\tCoordinates \t\tOwner");
                    foreach (TeleportDestination td in TeleportDestinations.GetLocations())
                    {
                        if (td.global)
                        {
                            SdtdConsole.Instance.Output(td.name + "\t\t\t" + td.x + "," + td.y + "," + td.z + "\t\t" + td.owner);
                        }
                    }
                    SdtdConsole.Instance.Output("\nPrivate Teleport Destinations:");
                    SdtdConsole.Instance.Output("Name \t\t\tCoordinates \t\tOwner");
                    foreach (TeleportDestination td in TeleportDestinations.GetLocations())
                    {
                        if ( !td.global && ( td.owner == p.Name || ListAll() ) )
                        {
                            SdtdConsole.Instance.Output(td.name + "\t\t\t" + td.x + "," + td.y + "," + td.z + "\t\t" + td.owner);
                        }
                    }
                }
                else
                    SdtdConsole.Instance.Output("No destination defined. Use AddTeleportDestination <name> to mark your current position.");
            }
            catch (Exception Ex)
            {
                SdtdConsole.Instance.Output(Ex.ToString());
            }
        }
    }

    public class ListAllTeleportDestinations : ListTeleportDestinations
    {
        public override bool ListAll()
        {
            return true;
        }
        public override string GetDescription()
        {
            return "List all available teleport destinations. Admin command!";
        }

        public override string GetHelp()
        {
            return "Usage:\n" +
                   "   ListAllTeleportDestinations \n";
        }

        public override string[] GetCommands()
        {
            return new[] { "ListAllTeleportDestinations", "latd" };
        }
        public override int DefaultPermissionLevel
        {
            get { return 0; }
        }
    }

    public class ShowLastTeleports : ConsoleCmdAbstract
    {
        public override string GetDescription()
        {
            return "Shows the last teleportation table.";
        }

        public override string GetHelp()
        {
            return "Shows the last teleportation table" +
                   "Usage:\n" +
                   "   ShowLastTeleports \n";
        }

        public override string[] GetCommands()
        {
            return new[] { "ShowLastTeleports", "slt" };
        }

        public override int DefaultPermissionLevel
        {
            // Admins only
            get { return 0; }
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {

                CommandSenderInfo senderinfo = _senderInfo;
                string playerID = senderinfo.RemoteClientInfo.playerId;
                Player p = PersistentContainer.Instance.Players[playerID, false];

                foreach (var teleport in LastTeleportTimes.Teleportations)
                {
                    SdtdConsole.Instance.Output(teleport.Key + " : " + teleport.Value);
                }
            }
            catch (Exception Ex)
            {
                SdtdConsole.Instance.Output(Ex.ToString());
            }
        }
    }


    public class ShowTeleportDelay : ConsoleCmdAbstract
    {
        public override string GetDescription()
        {
            return "Shows your teleport delay";
        }

        public override string GetHelp()
        {
            return "Shows your teleport delay" +
                   "Usage:\n" +
                   "   ShowTeleportDelay \n";
        }

        public override string[] GetCommands()
        {
            return new[] { "ShowTeleportDelay", "d" };
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
                Player p = PersistentContainer.Instance.Players[playerID, false];

                if (LastTeleportTimes.Teleportations.ContainsKey(p.SteamID))
                {
                    if (!LastTeleportTimes.IsValidTeleportRequest(p.SteamID))
                    {
                        LastTeleportTimes.ShowTimeTillNextTeleleport(p.SteamID);
                        return;
                    }
                }

                SdtdConsole.Instance.Output("You are free to teleport.");
            }
            catch (Exception Ex)
            {
                SdtdConsole.Instance.Output(Ex.ToString());
            }
        }
    }


    public class TeleportToDestination : ConsoleCmdAbstract
    {
        public override string GetDescription()
        {
            return "Teleport to a saved destination. See ListTeleportDestionations.";
        }

        public override string GetHelp()
        {
            return "TeleportTo" +
                   "Usage:\n" +
                   "   TeleportTo <destination>. \n" +
                   "See command ListTeleportDestinations and At";
        }

        public override string[] GetCommands()
        {
            return new[] { "TeleportTo", "t" };
        }

        public override int DefaultPermissionLevel
        {
            get { return 1000; }
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                if (_params[0] == null )
                    return;

                string name = _params[0];
                TeleportDestination dest = TeleportDestinations.GetLocation(name);

                CommandSenderInfo senderinfo = _senderInfo;
                string playerID = senderinfo.RemoteClientInfo.playerId;
                Player p = PersistentContainer.Instance.Players[playerID, false];

                if (p == null)
                    return;

                if (dest == null || dest.owner != p.Name)
                {
                    SdtdConsole.Instance.Output("Destination name not found or private.");
                    return;
                }

                if ( LastTeleportTimes.Teleportations.ContainsKey(p.SteamID) )
                {
                    
                    if ( !LastTeleportTimes.IsValidTeleportRequest(p.SteamID) )
                    {
                        LastTeleportTimes.ShowTimeTillNextTeleleport(p.SteamID);
                        return;
                    }
                    else
                        LastTeleportTimes.Teleportations[p.SteamID] = DateTime.Now;
                }
                else
                    LastTeleportTimes.Teleportations.Add(p.SteamID, DateTime.Now);

                new ConsoleCmdTeleport().Execute(new List<string>() { dest.x.ToString(), dest.y.ToString(), dest.z.ToString() }, _senderInfo);

            }
            catch (Exception ex)
            {
                SdtdConsole.Instance.Output(ex.ToString());
            }
        }
    }
}
