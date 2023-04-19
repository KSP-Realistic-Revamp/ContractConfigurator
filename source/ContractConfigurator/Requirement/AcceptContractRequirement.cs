using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.Util;
using KSP.Localization;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having accepted other contracts.
    /// </summary>
    public class AcceptContractRequirement : ContractCheckRequirement
    {
        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Don't support min/max counts
            valid &= ConfigNodeUtil.ParseValue<uint>(configNode, "minCount", x => minCount = x, this, 1, x => Validation.EQ<uint>(x, 1));
            valid &= ConfigNodeUtil.ParseValue<uint>(configNode, "maxCount", x => maxCount = x, this, UInt32.MaxValue, x => Validation.EQ<uint>(x, UInt32.MaxValue));

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Get the count of accepted contracts
            int accepted = 0;

            // Accepted CC contracts with matching tag
            if (tag != null)
            {

                // special handling if this contract has the tag - we need to not count it in that case.
                IEnumerable<ConfiguredContract> acceptedContract = ContractSystem.Instance.Contracts.OfType<ConfiguredContract>().
                   Where(c => c != null && c.contractType != null && c.ContractState == Contract.State.Active && c.contractType.tag.Equals(tag) && c != contract);
                accepted = acceptedContract.Count();
            }
            // Accepted contracts - Contract Configurator style
            else if (ccType != null)
            {
                IEnumerable<ConfiguredContract> acceptedContract = ContractSystem.Instance.Contracts.OfType<ConfiguredContract>().
                    Where(c => c != null && c.contractType != null && c.contractType.name.Equals(ccType) && c.ContractState == Contract.State.Active);
                accepted = acceptedContract.Count();
            }
            // Accepted contracts - stock style
            else
            {
                // Call the GetCompletedContracts with our type, and get the count
                IEnumerable<Contract> acceptedContract = ContractSystem.Instance.Contracts.Where(c => c != null && c.GetType() == contractClass &&
                    c.ContractState == Contract.State.Active);
                accepted = acceptedContract.Count();
            }

            // Return based on the min/max counts configured
            return (accepted >= minCount) && (accepted <= maxCount);
        }

        protected override string RequirementText()
        {
            string title = StringBuilderCache.Format("<color=#{0}>{1}</color>", MissionControlUI.RequirementHighlightColor, ContractTitle());
            return Localizer.Format(invertRequirement ? "#cc.req.AcceptContract.x" : "#cc.req.AcceptContract", title);
        }

        internal bool ConflictsWithContract(ConfiguredContract cc)
        {
            if (!InvertRequirement)
                return false;

            if (tag != null)
            {
                return cc.contractType?.tag == tag;
            }
            else if (ccType != null)
            {
                return cc.contractType?.name == ccType;
            }

            // Do not support checking for stock contract types
            return false;
        }
    }
}
