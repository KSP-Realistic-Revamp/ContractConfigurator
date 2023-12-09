using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory for VisitWaypoint.
    /// </summary>
    public class VisitWaypointFactory : ParameterFactory
    {
        protected int index;
        protected double distance;
        protected double horizontalDistance;
        protected bool hideOnCompletion;
        protected bool showMessages;
        protected float updateFrequency;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "index", x => index = x, this, 0, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "distance", x => distance = x, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "horizontalDistance", x => horizontalDistance = x, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "hideOnCompletion", x => hideOnCompletion = x, this, true);
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "showMessages", x => showMessages = x, this, false);
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "updateFrequency", x => updateFrequency = x, this, VisitWaypoint.DEFAULT_UPDATE_FREQUENCY, x => Validation.GT(x, 0.0f));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            if (contract == null)
            {
                LoggingUtil.LogError(this, "Contract is null.");
                return null;
            }
            VisitWaypoint vw = new VisitWaypoint(index, distance, horizontalDistance, hideOnCompletion, showMessages, title, updateFrequency);
            return vw.FetchWaypoint(contract) != null ? vw : null;
        }
    }
}
