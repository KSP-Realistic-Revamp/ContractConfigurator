﻿using System;
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
    /// Parameter for checking whether a vessel has the given resource.
    /// </summary>
    public class HasResource : VesselParameter
    {
        public class Filter
        {
            public PartResourceDefinition resource { get; set; }
            public double minQuantity { get; set; }
            public double maxQuantity { get; set; }

            public Filter() { }
        }

        protected List<Filter> filters = new List<Filter>();
        protected bool capacity = false;

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public HasResource()
            : base(null)
        {
        }

        public HasResource(List<Filter> filters, bool capacity, string title = null)
            : base(title)
        {
            this.filters = filters;
            this.capacity = capacity;

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                if (state == ParameterState.Complete || ParameterCount == 1)
                {
                    if (ParameterCount == 1)
                    {
                        hideChildren = true;
                    }

                    output = ParameterDelegate<Vessel>.GetDelegateText(this);
                }
                else
                {
                    output = Localizer.GetStringByTag("#cc.param.HasResource");
                }
            }
            else
            {
                output = title;
            }
            return output;
        }

        protected override string GetParameterTitlePreview(out bool hideChildren)
        {
            if (!string.IsNullOrEmpty(title))
            {
                hideChildren = true;
                return title;
            }

            if (ParameterCount == 1)
            {
                hideChildren = true;
                return ParameterDelegate<Vessel>.GetDelegateText(this);
            }
            else
            {
                hideChildren = false;
                return Localizer.GetStringByTag("#cc.param.HasResource");
            }
        }

        protected void CreateDelegates()
        {
            foreach (Filter filter in filters)
            {
                string resourceStr;
                if (filter.maxQuantity == 0)
                {
                    resourceStr = Localizer.GetStringByTag("#cc.param.count.none");
                }
                else if (filter.maxQuantity == double.MaxValue && (filter.minQuantity > 0.0 && filter.minQuantity <= 0.01))
                {
                    resourceStr  = Localizer.GetStringByTag("#cc.param.HasResource.notzero");
                }
                else if (filter.maxQuantity == double.MaxValue)
                {
                    resourceStr = Localizer.Format("#cc.param.HasResource.measure", Localizer.Format("#cc.param.count.atLeast.num", filter.minQuantity));
                }
                else if (filter.minQuantity == 0)
                {
                    resourceStr = Localizer.Format("#cc.param.HasResource.measure", Localizer.Format("#cc.param.count.atMost.num", filter.maxQuantity));
                }
                else
                {
                    resourceStr = Localizer.Format("#cc.param.HasResource.measure", Localizer.Format("#cc.param.count.between.num", filter.minQuantity, filter.maxQuantity));
                }

                string output = Localizer.Format((capacity ? "#cc.param.HasResource.capacity" : "#cc.param.HasResource.resource"), filter.resource.name, resourceStr);
                AddParameter(new ParameterDelegate<Vessel>(output, v => VesselHasResource(v, filter.resource, capacity, filter.minQuantity, filter.maxQuantity),
                    ParameterDelegateMatchType.VALIDATE));
            }
        }

        protected static bool VesselHasResource(Vessel vessel, PartResourceDefinition resource, bool capacity, double minQuantity, double maxQuantity)
        {
            double quantity = capacity ? vessel.ResourceCapacity(resource) : vessel.ResourceQuantity(resource);
            return quantity >= minQuantity && quantity <= maxQuantity;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);

            node.AddValue("capacity", capacity);

            foreach (Filter filter in filters)
            {
                ConfigNode childNode = new ConfigNode("RESOURCE");
                node.AddNode(childNode);

                childNode.AddValue("resource", filter.resource.name);
                childNode.AddValue("minQuantity", filter.minQuantity);
                if (filter.maxQuantity != double.MaxValue)
                {
                    childNode.AddValue("maxQuantity", filter.maxQuantity);
                }
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                base.OnParameterLoad(node);

                capacity = ConfigNodeUtil.ParseValue<bool?>(node, "capacity", (bool?)false).Value;

                foreach (ConfigNode childNode in node.GetNodes("RESOURCE"))
                {
                    Filter filter = new Filter();

                    filter.resource = ConfigNodeUtil.ParseValue<PartResourceDefinition>(childNode, "resource");
                    filter.minQuantity = ConfigNodeUtil.ParseValue<double>(childNode, "minQuantity");
                    filter.maxQuantity = ConfigNodeUtil.ParseValue<double>(childNode, "maxQuantity", double.MaxValue);

                    filters.Add(filter);
                }

                // Legacy
                if (node.HasValue("resource"))
                {
                    Filter filter = new Filter();

                    filter.resource = ConfigNodeUtil.ParseValue<PartResourceDefinition>(node, "resource");
                    filter.minQuantity = ConfigNodeUtil.ParseValue<double>(node, "minQuantity");
                    filter.maxQuantity = ConfigNodeUtil.ParseValue<double>(node, "maxQuantity", double.MaxValue);

                    filters.Add(filter);
                }

                CreateDelegates();
            }
            finally
            {
                ParameterDelegate<Part>.OnDelegateContainerLoad(node);
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
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

            return ParameterDelegate<Vessel>.CheckChildConditions(this, vessel);
        }
    }
}
