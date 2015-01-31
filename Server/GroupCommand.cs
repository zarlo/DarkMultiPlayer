using System;
using System.Collections.Generic;
using DarkMultiPlayerCommon;

namespace DarkMultiPlayerServer
{
    public class GroupCommand
    {
        public static void HandleCommand(string commandArgs)
        {
            string func = "";
            string argument1 = "";
            string argument2 = "";

            func = commandArgs;
            if (commandArgs.Contains(" "))
            {
                func = commandArgs.Substring(0, commandArgs.IndexOf(" "));
                if (commandArgs.Substring(func.Length).Contains(" "))
                {
                    string parseString = commandArgs.Substring(func.Length + 1);
                    //First argument
                    if (parseString.StartsWith("\""))
                    {
                        parseString = parseString.Substring(1);
                        argument1 = parseString.Substring(0, parseString.IndexOf("\""));
                        parseString = parseString.Substring(argument1.Length + 1);
                        if (parseString.StartsWith(" "))
                        {
                            parseString = parseString.Substring(1);
                        }
                    }
                    else
                    {
                        if (parseString.Contains(" "))
                        {
                            argument1 = parseString.Substring(0, parseString.IndexOf(" "));
                            parseString = parseString.Substring(argument1.Length + 1);
                        }
                        else
                        {
                            argument1 = parseString.Substring(0, parseString.Length);
                            parseString = "";
                        }

                    }
                    //Second argument
                    if (parseString.Length > 0)
                    {
                        if (parseString.StartsWith("\""))
                        {
                            argument2 = parseString.Substring(1, parseString.Length - 1);
                        }
                        else
                        {
                            argument2 = parseString.Substring(0, parseString.Length);
                        }
                    }
                }
            }

            switch (func)
            {
                default:
                    DarkLog.Debug("Undefined function. Usage: /group [create|join] groupname leader, [leave] playername, [remove] groupname, or /group show");
                    break;
                case "create":
                    GroupSystem.fetch.CreateGroup(null, argument1, argument2, GroupPrivacy.PUBLIC);
                    break;
                case "remove":
                    GroupSystem.fetch.RemoveGroup(null, argument1);
                    break;
                case "join":
                    GroupSystem.fetch.JoinGroup(null, argument1, argument2);
                    break;
                case "leave":
                    GroupSystem.fetch.LeaveGroup(null, argument1);
                    break;
                case "show":
                    foreach (KeyValuePair<string,GroupObject> kvp in GroupSystem.fetch.GetCopy())
                    {
                        DarkLog.Debug(kvp.Key + " (" + kvp.Value.privacy + ")");
                        bool printedLeader = false;
                        foreach (string member in kvp.Value.members)
                        {
                            if (!printedLeader)
                            {
                                DarkLog.Debug("  @" + member);
                                printedLeader = true;
                            }
                            else
                            {
                                DarkLog.Debug("  +" + member);
                            }
                        }
                    }
                    break;
            }
        }
    }
}

