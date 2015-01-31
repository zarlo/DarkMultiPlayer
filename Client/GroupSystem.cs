using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DarkMultiPlayerCommon;
using MessageStream2;

namespace DarkMultiPlayer
{
    public class GroupSystem
    {
        private static GroupSystem singleton;
        private Dictionary<string, GroupObject> groups = new Dictionary<string, GroupObject>();
        private object groupLock = new object();

        public static GroupSystem fetch
        {
            get
            {
                return singleton;
            }
        }

        public void HandleGroupMessage(byte[] messageData)
        {
            lock (groupLock)
            {
                using (MessageReader mr = new MessageReader(messageData))
                {
                    GroupMessageType messageType = (GroupMessageType)mr.Read<int>();
                    switch (messageType)
                    {
                        case GroupMessageType.SET:
                            {
                                string groupName = mr.Read<string>();
                                string[] groupMembers = mr.Read<string[]>();
                                GroupPrivacy groupPrivate = (GroupPrivacy)mr.Read<int>();
                                string groupSalt = null;
                                if (groupPrivate == GroupPrivacy.PRIVATE)
                                {
                                    bool passwordSet = mr.Read<bool>();
                                    {
                                        if (passwordSet)
                                        {
                                            groupSalt = mr.Read<string>();
                                        }
                                    }
                                }
                                if (!groups.ContainsKey(groupName))
                                {
                                    groups.Add(groupName, new GroupObject());
                                }
                                groups[groupName].members = new List<string>(groupMembers);
                                groups[groupName].privacy = groupPrivate;
                                groups[groupName].passwordSalt = groupSalt;
                                DarkLog.Debug("Group " + groupName + " updated");
                            }
                            break;
                        case GroupMessageType.REMOVE:
                            {
                                string groupName = mr.Read<string>();
                                if (groups.ContainsKey(groupName))
                                {
                                    groups.Remove(groupName);
                                    DarkLog.Debug("Group " + groupName + " removed");
                                }
                            }
                            break;
                        default:
                            DarkLog.Debug("Unknown group message type: " + messageType);
                            break;
                    }
                }
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                singleton = new GroupSystem();
            }
        }
    }
}

