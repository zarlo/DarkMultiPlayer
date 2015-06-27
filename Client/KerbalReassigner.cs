using System;
using System.Collections.Generic;

namespace DarkMultiPlayer
{
    public class KerbalReassigner
    {
        private static KerbalReassigner singleton;
        private bool registered = false;
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
            }
        }

        private void OnVesselCreate(Vessel vessel)
        {
            if (vesselToKerbal.ContainsKey(vessel.id))
            {
                //Shouldn't happen, but being defensive shouldn't hurt.
                DarkLog.Debug("OnVesselCreate has a duplicate entry for " + vessel.id + ", cleaning!");
                OnVesselDestroyed(vessel);
            }
            vesselToKerbal.Add(vessel.id, new List<string>());
            foreach (ProtoCrewMember pcm in vessel.GetVesselCrew())
            {
                vesselToKerbal[vessel.id].Add(pcm.name);
                if (kerbalToVessel.ContainsKey(pcm.name) && kerbalToVessel[pcm.name] != vessel.id)
                {
                    DarkLog.Debug("Warning, kerbal failed to reassign on " + vessel.id + " ( " + vessel.name + " )");
                }
                kerbalToVessel[pcm.name] = vessel.id;
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

        public void DodgeKerbals(ConfigNode inputNode, Guid protovesselID)
        {
            List<string> takenKerbals = new List<string>();
            foreach (ConfigNode partNode in inputNode.GetNodes("PART"))
            {
                int crewIndex = 0;
                foreach (string currentKerbalName in partNode.GetValues("crew"))
                {
                    if (kerbalToVessel.ContainsKey(currentKerbalName) ? kerbalToVessel[currentKerbalName] == protovesselID : false)
                    {
                        ProtoCrewMember newKerbal = null;
                        ProtoCrewMember.Gender newKerbalGender = ProtoCrewMember.Gender.Male;
                        string newExperienceTrait = null;
                        if (HighLogic.CurrentGame.CrewRoster.Exists(currentKerbalName))
                        {
                            ProtoCrewMember oldKerbal = HighLogic.CurrentGame.CrewRoster[currentKerbalName];
                            newKerbalGender = oldKerbal.gender;
                            newExperienceTrait = oldKerbal.experienceTrait.TypeName;
                        }
                        while (newKerbal == null)
                        {
                            ProtoCrewMember possibleKerbal = HighLogic.CurrentGame.CrewRoster.GetNextAvailableKerbal(ProtoCrewMember.KerbalType.Crew);
                            if (kerbalToVessel.ContainsKey(possibleKerbal.name) && (takenKerbals.Contains(possibleKerbal.name) || kerbalToVessel[possibleKerbal.name] != protovesselID))
                            {
                                possibleKerbal.rosterStatus = ProtoCrewMember.RosterStatus.Missing;
                                continue;
                            }
                            if (possibleKerbal.gender != newKerbalGender)
                            {
                                possibleKerbal.rosterStatus = ProtoCrewMember.RosterStatus.Missing;
                                continue;
                            }
                            if (newExperienceTrait != null && newExperienceTrait != possibleKerbal.experienceTrait.TypeName)
                            {
                                possibleKerbal.rosterStatus = ProtoCrewMember.RosterStatus.Missing;
                                continue;
                            }
                            newKerbal = possibleKerbal;
                        }
                        DarkLog.Debug("Crew reassigned in " + protovesselID + ", replaced " + currentKerbalName + " with " + newKerbal.name);
                        partNode.SetValue("crew", newKerbal.name, crewIndex);
                        takenKerbals.Add(newKerbal.name);
                    }
                    else
                    {
                        takenKerbals.Add(currentKerbalName);
                    }
                    crewIndex++;
                }
            }
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

