using System;
using System.Collections.Generic;
using System.Reflection;

namespace DarkMultiPlayer
{
    public class KerbalReassigner
    {
        private static KerbalReassigner singleton;
        private bool registered = false;
        private static string[] femaleNames;
        private static string[] femaleNamesPrefix;
        private static string[] femaleNamesPostfix;
        private Dictionary<Guid, List<string>> vesselToKerbal = new Dictionary<Guid, List<string>>();
        private Dictionary<string, Guid> kerbalToVessel = new Dictionary<string, Guid>();

        public static KerbalReassigner fetch
        {
            get
            {
                return singleton;
            }
        }

        public void RegisterGameHooks()
        {
            if (!registered)
            {
                registered = true;
                GameEvents.onVesselCreate.Add(this.OnVesselCreate);
                GameEvents.onVesselWasModified.Add(this.OnVesselWasModified);
                GameEvents.onVesselDestroy.Add(this.OnVesselDestroyed);
                GameEvents.onFlightReady.Add(this.OnFlightReady);
            }
        }

        private void UnregisterGameHooks()
        {
            if (registered)
            {
                registered = false;
                GameEvents.onVesselCreate.Remove(this.OnVesselCreate);
                GameEvents.onVesselWasModified.Remove(this.OnVesselWasModified);
                GameEvents.onVesselDestroy.Remove(this.OnVesselDestroyed);
                GameEvents.onFlightReady.Remove(this.OnFlightReady);
            }
        }

        private void OnVesselCreate(Vessel vessel)
        {
            //Kerbals are put in the vessel *after* OnVesselCreate. Thanks squad!.
            if (vesselToKerbal.ContainsKey(vessel.id))
            {
                OnVesselDestroyed(vessel);
            }
            if (vessel.GetCrewCount() > 0)
            {
                vesselToKerbal.Add(vessel.id, new List<string>());
                foreach (ProtoCrewMember pcm in vessel.GetVesselCrew())
                {
                    vesselToKerbal[vessel.id].Add(pcm.name);
                    if (kerbalToVessel.ContainsKey(pcm.name) && kerbalToVessel[pcm.name] != vessel.id)
                    {
                        DarkLog.Debug("Warning, kerbal double take on " + vessel.id + " ( " + vessel.name + " )");
                    }
                    kerbalToVessel[pcm.name] = vessel.id;
                    DarkLog.Debug("OVC " + pcm.name + " belongs to " + vessel.id);
                }
            }
        }

        private void OnVesselWasModified(Vessel vessel)
        {
            OnVesselDestroyed(vessel);
            OnVesselCreate(vessel);
        }

        private void OnVesselDestroyed(Vessel vessel)
        {
            if (vesselToKerbal.ContainsKey(vessel.id))
            {
                foreach (string kerbalName in vesselToKerbal[vessel.id])
                {
                    kerbalToVessel.Remove(kerbalName);
                }
                vesselToKerbal.Remove(vessel.id);
            }
        }

        //Squad workaround - kerbals are assigned after vessel creation for new vessels.
        private void OnFlightReady()
        {
            if (!vesselToKerbal.ContainsKey(FlightGlobals.fetch.activeVessel.id))
            {
                OnVesselCreate(FlightGlobals.fetch.activeVessel);
            }
        }

        public void DodgeKerbals(ConfigNode inputNode, Guid protovesselID)
        {
            DarkLog.Debug("DK1");
            List<string> takenKerbals = new List<string>();
            DarkLog.Debug("DK2");
            foreach (ConfigNode partNode in inputNode.GetNodes("PART"))
            {
                DarkLog.Debug("DK3");
                int crewIndex = 0;
                foreach (string currentKerbalName in partNode.GetValues("crew"))
                {
                    DarkLog.Debug("DK4");
                    if (kerbalToVessel.ContainsKey(currentKerbalName) ? kerbalToVessel[currentKerbalName] != protovesselID : false)
                    {
                        DarkLog.Debug("DK5");
                        ProtoCrewMember newKerbal = null;
                        DarkLog.Debug("DK6");
                        ProtoCrewMember.Gender newKerbalGender = GetKerbalGender(currentKerbalName);
                        DarkLog.Debug("DK7");
                        string newExperienceTrait = null;
                        DarkLog.Debug("DK8");
                        if (HighLogic.CurrentGame.CrewRoster.Exists(currentKerbalName))
                        {
                            DarkLog.Debug("DK9");
                            ProtoCrewMember oldKerbal = HighLogic.CurrentGame.CrewRoster[currentKerbalName];
                            DarkLog.Debug("DK10");
                            newKerbalGender = oldKerbal.gender;
                            DarkLog.Debug("DK11");
                            newExperienceTrait = oldKerbal.experienceTrait.TypeName;
                            DarkLog.Debug("DK12");
                        }
                        DarkLog.Debug("DK13");
                        foreach (ProtoCrewMember possibleKerbal in HighLogic.CurrentGame.CrewRoster.Crew)
                        {
                            DarkLog.Debug("DK14 - " + possibleKerbal.name);
                            bool kerbalOk = true;
                            DarkLog.Debug("DK15");
                            if (kerbalOk && kerbalToVessel.ContainsKey(possibleKerbal.name) && (takenKerbals.Contains(possibleKerbal.name) || kerbalToVessel[possibleKerbal.name] != protovesselID))
                            {
                                DarkLog.Debug("DK16");
                                kerbalOk = false;
                            }
                            DarkLog.Debug("DK17");
                            if (kerbalOk && possibleKerbal.gender != newKerbalGender)
                            {
                                DarkLog.Debug("DK18");
                                kerbalOk = false;
                            }
                            DarkLog.Debug("DK19");
                            if (kerbalOk && newExperienceTrait != null && newExperienceTrait != possibleKerbal.experienceTrait.TypeName)
                            {
                                DarkLog.Debug("DK20 - " + newExperienceTrait + " : " + possibleKerbal.experienceTrait.TypeName);
                                kerbalOk = false;
                            }
                            if (kerbalOk)
                            {
                                DarkLog.Debug("DK21");
                                newKerbal = possibleKerbal;
                                break;
                            }
                        }
                        int kerbalTries = 0;
                        while (newKerbal == null)
                        {
                            DarkLog.Debug("DK22");
                            bool kerbalOk = true;
                            DarkLog.Debug("DK23");
                            ProtoCrewMember.KerbalType kerbalType = ProtoCrewMember.KerbalType.Crew;
                            if (newExperienceTrait == "Tourist")
                            {
                                kerbalType = ProtoCrewMember.KerbalType.Tourist;
                            }
                            ProtoCrewMember possibleKerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(kerbalType);
                            DarkLog.Debug("DK24");
                            if (kerbalTries < 200 && possibleKerbal.gender != newKerbalGender)
                            {
                                DarkLog.Debug("DK25");
                                kerbalOk = false;
                            }
                            DarkLog.Debug("DK26");
                            if (kerbalTries < 100 && newExperienceTrait != null && newExperienceTrait != possibleKerbal.experienceTrait.TypeName)
                            {
                                DarkLog.Debug("DK27 - " + newExperienceTrait + " : " + possibleKerbal.experienceTrait.TypeName);
                                kerbalOk = false;
                            }
                            DarkLog.Debug("DK28");
                            if (kerbalOk)
                            {
                                DarkLog.Debug("DK29");
                                newKerbal = possibleKerbal;
                            }
                            kerbalTries++;
                        }
                        DarkLog.Debug("Kerbal generated with " + kerbalTries + " tries");
                        partNode.SetValue("crew", newKerbal.name, crewIndex);
                        newKerbal.seatIdx = crewIndex;
                        newKerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                        takenKerbals.Add(newKerbal.name);
                    }
                    else
                    {
                        takenKerbals.Add(currentKerbalName);
                        CreateKerbalIfMissing(currentKerbalName, protovesselID);
                        HighLogic.CurrentGame.CrewRoster[currentKerbalName].rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                        HighLogic.CurrentGame.CrewRoster[currentKerbalName].seatIdx = crewIndex;
                    }
                    crewIndex++;
                }
            }
            vesselToKerbal[protovesselID] = takenKerbals;
            foreach (string name in takenKerbals)
            {
                kerbalToVessel[name] = protovesselID;
            }
        }

        public void CreateKerbalIfMissing(string kerbalName, Guid vesselID)
        {
            if (!HighLogic.CurrentGame.CrewRoster.Exists(kerbalName))
            {
                ProtoCrewMember pcm = CrewGenerator.RandomCrewMemberPrototype(ProtoCrewMember.KerbalType.Crew);
                pcm.ChangeName(kerbalName);
                pcm.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                HighLogic.CurrentGame.CrewRoster.AddCrewMember(pcm);
                DarkLog.Debug("Created kerbal " + pcm.name + " for vessel " + vesselID + ", Kerbal was missing");
            }
        }

        //Better not use a bool for this and enforce the gender binary on xir!
        public static ProtoCrewMember.Gender GetKerbalGender(string kerbalName)
        {
            DarkLog.Debug("GKG FIND 1");
            if (femaleNames == null || femaleNamesPrefix == null || femaleNamesPostfix == null)
            {
                foreach (FieldInfo fi in typeof(CrewGenerator).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
                {
                    DarkLog.Debug("Name: " + fi.Name);
                    DarkLog.Debug("Name: " + (int)(fi.Name[0]));
                    if (fi.FieldType == typeof(string[]))
                    {
                        string[] fieldValue = (string[])fi.GetValue(null);
                        foreach (string entry in fieldValue)
                        {
                            if (entry == "Alice")
                            {
                                DarkLog.Debug("Found female single names!");
                                femaleNames = fieldValue;
                                break;
                            }
                            if (entry == "Aga")
                            {
                                DarkLog.Debug("Found female prefixes!");
                                femaleNamesPrefix = fieldValue;
                                break;
                            }
                            if (entry == "alla")
                            {
                                DarkLog.Debug("Found female postfixes!");
                                femaleNamesPostfix = fieldValue;
                                break;
                            }
                        }
                    }
                }
            }
            if (femaleNames == null || femaleNamesPrefix == null || femaleNamesPostfix == null)
            {
                DarkLog.Debug("Kerbal Gender Assigner is BROKEN!");
            }
            DarkLog.Debug("GKG FIND 2");
            string trimmedName = kerbalName;
            if (kerbalName.Contains(" Kerman"))
            {
                trimmedName = kerbalName.Substring(0, kerbalName.IndexOf(" Kerman"));
                DarkLog.Debug("(KerbalReassigner) Trimming name to '" + trimmedName + "'");
            }
            DarkLog.Debug("GKG4");
            //Not part of the generator
            if (trimmedName == "Valentina")
                return ProtoCrewMember.Gender.Female;
            DarkLog.Debug("GKG5");
            foreach (string name in femaleNames)
            {
                DarkLog.Debug("GKG6");
                if (name == trimmedName)
                    return ProtoCrewMember.Gender.Female;
            }

            foreach (string prefixName in femaleNamesPrefix)
            {
                DarkLog.Debug("GKG7 - " + prefixName);
                if (trimmedName.StartsWith(prefixName))
                {
                    DarkLog.Debug("GKG8");
                    foreach (string postfixName in femaleNamesPostfix)
                    {
                        DarkLog.Debug("GKG9 - " + postfixName);
                        if (trimmedName == prefixName + postfixName)
                        {
                            DarkLog.Debug("GKG10");
                            return ProtoCrewMember.Gender.Female;
                        }
                    }
                }
            }
            return ProtoCrewMember.Gender.Male;
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    if (singleton.registered)
                    {
                        singleton.UnregisterGameHooks();
                    }
                }
                singleton = new KerbalReassigner();
            }
        }
    }
}

