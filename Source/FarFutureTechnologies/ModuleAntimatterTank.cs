﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using PowerMonitor;

namespace FarFutureTechnologies
{
    public class ModuleAntimatterTank: PartModule, IPowerConsumingPart
    {
        // Name of the fuel to boil off
        [KSPField(isPersistant = false)]
        public string FuelName;

        // Cost to contain in u/s
        [KSPField(isPersistant = false)]
        public float ContainmentCost = 0.0f;

        // KJ of energy released per unit of AM detonated
        // canoncially 1 ug of AM is 36000 kJ, 1 ug = 1 unit
        [KSPField(isPersistant = false)]
        public float DetonationKJPerUnit = 36000f;

        // Rate of AM detonation in u/s
        [KSPField(isPersistant = false)]
        public float DetonationRate = 5f;

        // Whether tank containment is enabled
        [KSPField(isPersistant = true)]
        public bool ContainmentEnabled = true;

        // Whether detonation is occurring
        [KSPField(isPersistant = true)]
        public bool DetonationOccuring = false;

        [KSPField(isPersistant = true)]
        public int PowerUsePriority = 0;

        // PRIVATE
        private double fuelAmount = 0.0;
        private double maxFuelAmount = 0.0;
        private double totalPowerCost = 0.0;

        // UI FIELDS/ BUTTONS
        // Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Stability")]
        public string DetonationStatus = "N/A";

        [KSPField(isPersistant = false, guiActive = true, guiName = "Containment")]
        public string ContainmentStatus = "N/A";

        [KSPEvent(guiActive = true, guiName = "Enable Containment", active = true)]
        public void Enable()
        {
            ContainmentEnabled= true;
        }
        [KSPEvent(guiActive = false, guiName = "Disable Containment", active = false)]
        public void Disable()
        {
            ContainmentEnabled= false;
        }

        // ACTIONS
        [KSPAction("Enable Containment")]
        public void EnableAction(KSPActionParam param) { Enable(); }

        [KSPAction("Disable Containment")]
        public void DisableAction(KSPActionParam param) { Disable(); }

        [KSPAction("Toggle Containment")]
        public void ToggleAction(KSPActionParam param)
        {
            ContainmentEnabled = !ContainmentEnabled;
        }

        // VAB UI
        public override string GetInfo()
        {
          string msg = String.Format("Containment Cost: {0:F2} Ec/s", ContainmentCost);
          return msg;
        }

        // INTERFACE METHODS
        // Sets the powered/unpowered state
        public void SetPoweredState(bool state)
        {
          if (ContainmentEnabled && ContainmentCost > 0f)
          {
            if (state)
            {
              DetonationOccuring = false;
              DetonationStatus = String.Format("Contained");
              ContainmentStatus = String.Format("Using {0:F2} Ec/s", coolingCost);
            } else
            {
              DetonationOccuring = true;
              DetonationStatus = String.Format("Losing {0:F2} u/s", DetonationRate);
              ContainmentStatus = "Uncontained!";
            }
        }

        // All AM
        public int GetPriority()
        {
          return PowerUsePriority;
        }

        // Gets the canonical power usage
        public double GetPowerUsage()
        {
            return totalPowerCost;
        }

        // Gets the current power usage
        public double CalculatePowerUsage()
        {
          if (ContainmentEnabled)
          {
            return totalPowerCost;
          }
          return 0d;
        }

        // Does processing at "low" warp
        public void ProcessLowWarp()
        {
          ConsumeCharge();
        }

        // Does processing at "high" warp
        public void ProcessHighWarp()
        {
          ConsumeCharge();
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
              fuelAmount = GetResourceAmount(FuelName);
              maxFuelAmount = GetMaxResourceAmount(FuelName);
              totalPowerCost = maxFuelAmount*ContainmentCost;

              // Catchup
              DoCatchup();
            }
        }

        public void DoCatchup()
        {
          if (part.vessel.missionTime > 0.0)
          {
              if (part.RequestResource("ElectricCharge", ContainmentCost * TimeWarp.fixedDeltaTime) < ContainmentCost * TimeWarp.fixedDeltaTime)
              {
                  //double elapsedTime = part.vessel.missionTime - LastUpdateTime;
                  //
                  //double toBoil = Math.Pow(1.0 - boiloffRateSeconds, elapsedTime);
                  //part.RequestResource(FuelName, (1.0 - toBoil) * fuelAmount,ResourceFlowMode.NO_FLOW);
              }
          }
        }

        public void Update()
        {
          if (HighLogic.LoadedSceneIsFlight)
          {

            // Show the containment status field if there is a cooling cost
              if (ContainmentCost > 0f)
            {
              foreach (BaseField fld in base.Fields)
                {
                    if (fld.guiName == "Containment")
                        fld.guiActive = true;
                }

              if (Events["Enable"].active == ContainmentEnabled || Events["Disable"].active != ContainmentEnabled)
                {
                    Events["Disable"].active = ContainmentEnabled;
                    Events["Enable"].active = !ContainmentEnabled;
               }
            }
          }
          if (HighLogic.LoadedSceneIsEditor)
          {
                foreach (BaseField fld in base.Fields)
                {
                    if (fld.guiName == "Containment")
                        fld.guiActiveEditor = true;
                }
                double max = GetMaxResourceAmount(FuelName);
                ContainmentStatus = String.Format("Cost {0:F2} Ec/s", ContainmentCost * (float)(max));
          }
        }



        protected void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                fuelAmount = GetResourceAmount(FuelName);
                // If we have no fuel, no need to do any calculations
                if (fuelAmount == 0.0)
                {
                  ContainmentStatus = "No Antimatter";
                  return;
                }

                // If the cooling cost is zero, we must boil off
                if (ContainmentCost == 0f)
                {
                    DetonationOccuring = true;
                    DetonationStatus = String.Format("Losing {0:F2} u/s", DetonationRate);
                }
                // else check for available power
                else
                {
                    if (!ContainmentEnabled)
                    {
                        DetonationOccuring = true;
                        DetonationStatus = String.Format("Losing {0:F2} u/s", DetonationRate);
                        ContainmentStatus = "Disabled";
                    }
                  }

                if (DetonationOccuring)
                {
                    DoDetonation(1d);
                }
                if (part.vessel.missionTime > 0.0)
                {
                    LastUpdateTime = part.vessel.missionTime;
                }
            }
        }
        protected void ConsumeCharge()
        {

          if (ContainmentEnabled && ContainmentCost > 0f)
          {
              double chargeRequest = totalPowerCost * TimeWarp.fixedDeltaTime;

              double req = part.RequestResource("ElectricCharge", chargeRequest);
              //Debug.Log(req.ToString() + " rec, wanted "+ chargeRequest.ToString());
              // Fully cooled
              double tolerance = 0.0001;
              if (req >= chargeRequest - tolerance)
              {
                  SetPoweredState(true);
              }
              else
              {
                  SetPoweredState(false);
              }
          }

        }
        protected void DoDetonation()
        {
            double detonatedAmount = part.RequestResource(FuelName, TimeWarp.fixedDeltaTime* DetonationRate);
            part.AddThermalFlux(detonatedAmount*DetonationKJPerUnit);
        }



        public bool isResourcePresent(string nm)
          {
              int id = PartResourceLibrary.Instance.GetDefinition(nm).id;
              PartResource res = this.part.Resources.Get(id);
              if (res == null)
                  return false;
              return true;
          }
          protected double GetResourceAmount(string nm)
          {


              PartResource res = this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id);
              return res.amount;
          }
          protected double GetMaxResourceAmount(string nm)
          {

              int id = PartResourceLibrary.Instance.GetDefinition(nm).id;

              PartResource res = this.part.Resources.Get(id);

              return res.maxAmount;
          }


    }
}
