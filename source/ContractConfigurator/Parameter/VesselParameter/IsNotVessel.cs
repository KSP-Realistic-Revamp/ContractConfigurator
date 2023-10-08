using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking that a vessel is not a given vessel.
    /// </summary>
    public class IsNotVessel : VesselParameter
    {
        protected List<string> vessels { get; set; }

        public IsNotVessel()
            : this(null, null)
        {
        }

        public IsNotVessel(List<string> vessels, string title)
            : base(title)
        {
            failWhenUnmet = true;
            fakeFailures = true;

            this.vessels = vessels ?? new List<string>();
        }

        protected override string GetParameterTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                string outStr;
                if (vessels.Count == 1)
                {
                    outStr = ContractVesselTracker.GetDisplayName(vessels[0]);
                }
                else
                {
                    List<string> vNames = new List<string>();
                    foreach (var v in vessels)
                        vNames.Add(ContractVesselTracker.GetDisplayName(v));
                    outStr = Localizer.Format("<<and(1," + vNames.Count + ")>>", vNames);
                }
                output = Localizer.Format("#cc.param.IsNotVessel", outStr);
            }
            else
            {
                output = title;
            }

            return output;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            foreach (var v in vessels)
                node.AddValue("vesselKey", v);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            foreach (ConfigNode.Value v in node.values)
                if (v.name == "vesselKey")
                    vessels.Add(v.value);
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            ContractVesselTracker.OnVesselAssociation.Add(OnVesselAssociation);
            ContractVesselTracker.OnVesselDisassociation.Add(OnVesselDisassociation);
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            ContractVesselTracker.OnVesselAssociation.Remove(OnVesselAssociation);
            ContractVesselTracker.OnVesselDisassociation.Remove(OnVesselDisassociation);
        }

        protected void OnVesselAssociation(GameEvents.HostTargetAction<Vessel, string> pair)
        {
            LoggingUtil.LogVerbose(this, "OnVesselAssociation");
            CheckVessel(pair.host);
        }

        protected void OnVesselDisassociation(GameEvents.HostTargetAction<Vessel, string> pair)
        {
            LoggingUtil.LogVerbose(this, "OnVesselDisassociation");
            CheckVessel(pair.host);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: {0}", vessel.id);
            bool success = true;
            foreach (var v in vessels)
            {
                if (ContractVesselTracker.Instance.GetAssociatedVessel(v) == vessel)
                {
                    success = false;
                    break;
                }
            }
            return success;
        }
    }
}
