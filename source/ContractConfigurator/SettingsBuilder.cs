using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using KSP;
using Contracts;
using FinePrint;
using ContractConfigurator.Util;
using KSP.Localization;

namespace ContractConfigurator
{
    public class ContractConfiguratorParameters : GameParameters.CustomParameterNode
    {
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "Contract Configurator"; } }
        public override string DisplaySection { get { return Localizer.GetStringByTag("#cc.settings.Section"); } }

        public override int SectionOrder { get { return 0; } }
        public override string Title { get { return "#autoLOC_149458"; } } // Settings

        public bool DisplayOfferedOrbits = ContractDefs.DisplayOfferedOrbits;
        public bool DisplayActiveOrbits = true;
        public bool DisplayOfferedWaypoints = ContractDefs.DisplayOfferedWaypoints;
        public bool DisplayActiveWaypoints = true;

        public enum MissionControlButton
        {
            All,
            Available,
            Active,
            Archive
        }

        public MissionControlButton lastMCButton = MissionControlButton.All;

        public override void OnSave(ConfigNode node)
        {
            node.AddValue("lastMCButton", lastMCButton);
        }

        public override void OnLoad(ConfigNode node)
        {
            lastMCButton = ConfigNodeUtil.ParseValue<MissionControlButton>(node, "lastMCButton", MissionControlButton.All);
        }
    }
}
