﻿using System;
using System.Collections.Generic;
using AllocsFixes.PersistentData;

namespace MinksMods.ModChallenge
{

    public class API : IModApi
    {

        public void InitMod()
        {
            ModEvents.EntityKilled.RegisterHandler(EntityKilled);
            ModEvents.GameMessage.RegisterHandler(GameMessage);
            ModEvents.ChatMessage.RegisterHandler(ChatMessage);
            ModEvents.PlayerDisconnected.RegisterHandler(PlayerDisconnected);
            ModChallenge.init();
        }

        private bool GameMessage(ClientInfo _ci, EnumGameMessages _type, string _string1, string _playerName, bool _bool1, string _killerName, bool _bool2)
        {
            //Field Debug
#if DEBUG
            Log.Out("type: " + _type.ToString());       // EnumGameMessages
            Log.Out("string1: " + _string1);            // Unknown; "" on Join and Killed
            Log.Out("string2: " + _playerName);         // PlayerName
            Log.Out("string3: " + _killerName);         //  killer name in a pvp kill @EnumGameMessages.EntityWasKilled
            Log.Out("bool1: " + _bool1.ToString());    // Unknown; false on Join and Killed
            Log.Out("bool2: " + _bool2.ToString());    // Unknown; false on Join and Killed
#endif
            
            if (_type == EnumGameMessages.EntityWasKilled && !string.IsNullOrEmpty(_playerName))
            {
                ModChallenge.OnPlayerKilled(_playerName, _killerName);
            }

            if (_type == EnumGameMessages.LeftGame && !string.IsNullOrEmpty(_playerName))
            {
                ModChallenge.OnPlayerLeft(_playerName);
            }

            return true;
        }

        // sadly will not be called if player kills player
        public void EntityKilled(Entity a, Entity b)
        {
            try
            {
                if (isPlayer(a) || isPlayer(b))
                {
                    //ModChallenge.OnEntityKilled(a, b);
                }
            }
            catch (Exception Ex)
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
            if (e != null && e.GetType() == typeof(EntityPlayer))
            {
                return true;
            }

            return false;
        }

        public bool ChatMessage(ClientInfo _cInfo, EChatType _type, int _senderId, string _msg, string _mainName, bool _localizeMain, List<int> _recipientEntityIds)
        {
            if (string.IsNullOrEmpty(_msg))
            {
                return true;
            }

            if (_msg.EqualsCaseInsensitive("/mink") || _msg.EqualsCaseInsensitive("/c"))
            {
                if (_cInfo != null)
                {
                    Log.Out("Sent chat hook reply to {0}", _cInfo.playerId);
                    _cInfo.SendPackage(new NetPackageChat(EChatType.Whisper, -1, "Thats the author of ModChallenge!", "", false, null));
                }
                else
                {
                    Log.Error("ChatHookExample: Argument _cInfo null on message: {0}", _msg);
                }
            }
            else if (_msg.Contains("Minkio") )
            {
                Log.Out("_mainName: " + _mainName.ToString());
                Log.Out("_localizeMain: " + _localizeMain.ToString());
                Log.Out("_msg: " + _msg );
            }

            return false;
        }
    }

}