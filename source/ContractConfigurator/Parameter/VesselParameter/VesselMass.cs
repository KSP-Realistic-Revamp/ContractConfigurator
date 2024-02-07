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
    /// Parameter for checking the mass of a vessel
    /// </summary>
    public class VesselMass : VesselParameter
    {
        protected float minMass { get; set; }
        protected float maxMass { get; set; }
        protected float updateFrequency { get; set; }

        private float lastUpdate = 0.0f;
        internal const float DEFAULT_UPDATE_FREQUENCY = 0.25f;

        public VesselMass()
            : this(DEFAULT_UPDATE_FREQUENCY, 0.0f)
        {
        }

        public VesselMass(float updateFrequency, float minMass = 0.0f, float maxMass = float.MaxValue, string title = null)
            : base(title)
        {
            this.updateFrequency = updateFrequency;
            this.minMass = minMass;
            this.maxMass = maxMass;
            if (title == null)
            {
                this.title = "Mass: ";

                string massStr;
                if (maxMass == float.MaxValue)
                {
                    massStr = Localizer.Format("#cc.param.VesselMass.atLeast", minMass);
                }
                else if (minMass == 0.0)
                {
                    massStr = Localizer.Format("#cc.param.VesselMass.atMost", maxMass);
                }
                else
                {
                    massStr = Localizer.Format("#cc.param.VesselMass.between", minMass, maxMass);
                }
                this.title = Localizer.Format("#cc.param.VesselMass", massStr);
            }
            else
            {
                this.title = title;
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            node.AddValue("updateFrequency", updateFrequency);
            node.AddValue("minMass", minMass);
            if (maxMass != double.MaxValue)
            {
                node.AddValue("maxMass", maxMass);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            updateFrequency = ConfigNodeUtil.ParseValue<float>(node, "updateFrequency", DEFAULT_UPDATE_FREQUENCY);
            minMass = (float)Convert.ToDouble(node.GetValue("minMass"));
            maxMass = node.HasValue("maxMass") ? (float)Convert.ToDouble(node.GetValue("maxMass")) : float.MaxValue;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (UnityEngine.Time.fixedTime - lastUpdate > updateFrequency)
            {
                lastUpdate = UnityEngine.Time.fixedTime;
                CheckVessel(FlightGlobals.ActiveVessel);
            }
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: {0}", vessel.id);
            float mass = vessel.GetTotalMass();
            return mass >= minMass && mass <= maxMass;
        }
    }
}
