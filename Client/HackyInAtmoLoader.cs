using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkMultiPlayer
{
    public class HackyInAtmoLoader
    {
        private static HackyInAtmoLoader singleton;
        private List<HackyFlyingVesselLoad> loadingFlyingVessels = new List<HackyFlyingVesselLoad>();

        public static HackyInAtmoLoader fetch
        {
            get
            {
                return singleton;
            }
        }

        public void UpdateVessels()
        {
            foreach (HackyFlyingVesselLoad hfvl in loadingFlyingVessels.ToArray())
            {
                if (!FlightGlobals.fetch.vessels.Contains(hfvl.flyingVessel))
                {
                    DarkLog.Debug("Hacky load failed: Vessel destroyed");
                    loadingFlyingVessels.Remove(hfvl);
                }

                string vesselID = hfvl.flyingVessel.id.ToString();

                if (LockSystem.fetch.LockExists("update-" + vesselID) || LockSystem.fetch.LockIsOurs("update-" + vesselID))
                {
                    DarkLog.Debug("Hacky load failed: Vessel stopped being controlled");
                    loadingFlyingVessels.Remove(hfvl);
                    VesselWorker.fetch.KillVessel(hfvl.flyingVessel);
                }

                double atmoPressure = hfvl.flyingVessel.mainBody.staticPressureASL * Math.Pow(Math.E, ((-hfvl.flyingVessel.altitude) / (hfvl.flyingVessel.mainBody.atmosphereScaleHeight * 1000)));
                if (atmoPressure < 0.01)
                {
                    DarkLog.Debug("Hacky load successful: Vessel is now safe from atmo");
                    loadingFlyingVessels.Remove(hfvl);
                    hfvl.flyingVessel.Landed = false;
                    hfvl.flyingVessel.Splashed = false;
                    hfvl.flyingVessel.landedAt = string.Empty;
                    hfvl.flyingVessel.situation = Vessel.Situations.FLYING;
                }
            }
        }

        public void SetVesselUpdate(Vessel hackyVessel, VesselUpdate vesselUpdate)
        {
            foreach (HackyFlyingVesselLoad hfvl in loadingFlyingVessels)
            {
                if (hfvl.flyingVessel == hackyVessel)
                {
                    hfvl.lastVesselUpdate = vesselUpdate;
                }
            }
        }

        public void AddHackyInAtmoLoad(Vessel hackyVessel)
        {
            HackyFlyingVesselLoad hfvl = new HackyFlyingVesselLoad();
            hfvl.flyingVessel = hackyVessel;
            hfvl.loadTime = UnityEngine.Time.realtimeSinceStartup;
            loadingFlyingVessels.Add(hfvl);
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                singleton = new HackyInAtmoLoader();
            }
        }
    }

    class HackyFlyingVesselLoad
    {
        public double loadTime;
        public Vessel flyingVessel;
        public VesselUpdate lastVesselUpdate;
    }
}

