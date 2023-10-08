using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for IsNotVesselFactory ContractParameter. 
    /// </summary>
    public class IsNotVesselFactory : ParameterFactory
    {
        protected List<VesselIdentifier> vessels;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            bool foundOne = false;
            foreach (ConfigNode.Value v in configNode.values)
            {
                if (v.name != "vessel")
                    continue;

                foundOne = true;
                vessels.Add(new VesselIdentifier(v.value));
            }

            valid &= foundOne;

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            List<string> vStrings = new List<string>();
            foreach (var v in vessels)
                vStrings.Add(v.identifier);
            return new IsNotVessel(vStrings, title);
        }
    }
}
