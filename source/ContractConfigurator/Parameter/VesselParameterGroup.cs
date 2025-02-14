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
    /// ContractParameter that is successful when all child parameters are successful for a the same vessel over the given duration.
    /// </summary>
    public class VesselParameterGroup : ContractConfiguratorParameter, ParameterDelegateContainer
    {
        protected string define { get; set; }
        protected string defineList { get; set; }
        protected List<string> vesselList { get; set; }
        protected bool dissassociateVesselsOnContractFailure;
        protected bool dissassociateVesselsOnContractCompletion;
        protected bool hideVesselName;
        protected bool resetChildrenWhenVesselDestroyed;

        public IEnumerable<string> VesselList { get { return vesselList; } }
        protected double duration { get; set; }
        protected ParameterDelegate<Vessel> durationParameter;
        protected ParameterDelegate<Vessel> vesselListParam;
        protected double completionTime { get; set; }
        protected bool waiting { get; set; }

        private Vessel oldTrackedVessel = null;
        private Vessel trackedVessel = null;
        public Vessel TrackedVessel { get { return trackedVessel; } }
        private Guid trackedVesselGuid = Guid.Empty;

        private double lastUpdate = 0.0;
        private List<VesselWaypoint> vesselWaypoints = new List<VesselWaypoint>();

        public bool ChildChanged { get; set; }

        public VesselParameterGroup()
            : base("")
        {
        }

        public VesselParameterGroup(string title, string define, string defineList, IEnumerable<string> vesselList, double duration,
            bool dissassociateVesselsOnContractFailure, bool dissassociateVesselsOnContractCompletion, bool hideVesselName, bool resetChildrenWhenVesselDestroyed)
            : base(title)
        {
            this.define = define;
            this.defineList = defineList;
            this.duration = duration;
            this.vesselList = vesselList == null ? new List<string>() : vesselList.ToList();
            this.dissassociateVesselsOnContractFailure = dissassociateVesselsOnContractFailure;
            this.dissassociateVesselsOnContractCompletion = dissassociateVesselsOnContractCompletion;
            this.hideVesselName = hideVesselName;
            this.resetChildrenWhenVesselDestroyed = resetChildrenWhenVesselDestroyed;
            waiting = false;

            CreateVesselListParameter();
            CreateTimerParameter();
        }

        protected override string GetParameterTitle()
        {
            // Set the first part of the output
            string output;
            if (!string.IsNullOrEmpty(title))
            {
                output = title;
            }
            else
            {
                // Set the vessel name
                if ((waiting || state == ParameterState.Complete) && trackedVessel != null)
                {
                    output = Localizer.Format("#cc.param.VesselParameterGroup.default", trackedVessel.vesselName);
                }
                else if (!string.IsNullOrEmpty(define))
                {
                    output = Localizer.Format("#cc.param.VesselParameterGroup.newVessel", define);
                }
                else if (vesselList.Any())
                {
                    output = Localizer.Format("#cc.param.VesselParameterGroup.default",
                        LocalizationUtil.LocalizeList<string>(LocalizationUtil.Conjunction.OR, vesselList.AsEnumerable(), vesselName =>
                        {
                            if (ContractVesselTracker.Instance != null)
                            {
                                return ContractVesselTracker.GetDisplayName(vesselName);
                            }
                            else
                            {
                                LoggingUtil.LogWarning(this, "Unable to get vessel display name for '{0}' - ContractVesselTracker is null.  This is likely caused by another ScenarioModule crashing, preventing others from loading.", vesselName);
                                return vesselName;
                            }
                        }
                    ));
                }
                else
                {
                    output = Localizer.GetStringByTag("#cc.param.VesselParameterGroup.anyVessel");
                }

                // If we're complete and a custom title hasn't been provided, try to get a better title
                if (state == ParameterState.Complete && ParameterCount == 1 && trackedVessel != null)
                {
                    output = Localizer.Format("#cc.param.VesselParameterGroup.complete", trackedVessel.vesselName, GetParameter(0).Title);
                }
            }

            return output;
        }

        protected override string GetNotes()
        {
            if (duration > 0.0 && Root.ContractState == Contract.State.Active)
            {
                if (trackedVessel == null)
                {
                    return Localizer.GetStringByTag("#cc.param.VesselParameterGroup.notes.noVessel");
                }
                else if (!waiting)
                {
                    return Localizer.GetStringByTag("#cc.param.VesselParameterGroup.notes.activeVessel");
                }
                else
                {
                    return Localizer.Format("#cc.param.VesselParameterGroup.notes.waitingVessel", trackedVessel.vesselName);
                }
            }

            return base.GetNotes();
        }

        public override void OnContractLoad(ConfiguredContract configuredContract)
        {
            CreateTimerParameter();
        }

        protected void CreateTimerParameter()
        {
            if (duration > 0.0)
            {
                durationParameter = new ParameterDelegate<Vessel>(
                    Localizer.Format("#cc.param.VesselParameterGroup.duration", DurationUtil.StringValue(duration)),
                    v => false);
                durationParameter.Optional = true;
                durationParameter.fakeOptional = true;

                AddParameter(durationParameter);
            }
        }

        protected void CreateVesselListParameter()
        {
            if (vesselList.Any())
            {
                if (vesselList.Count() == 1)
                {
                    vesselListParam = new ParameterDelegate<Vessel>(hideVesselName ? "" : Localizer.Format("#cc.param.VesselParameterGroup.default", ContractVesselTracker.GetDisplayName(vesselList.First())), v =>
                    {
                        bool check = VesselCanBeConsidered(v);
                        if (!hideVesselName)
                        {
                            vesselListParam.SetTitle(Localizer.Format((FlightGlobals.ActiveVessel == v && trackedVessel != null ? "#cc.param.VesselParameterGroup.default" : "#cc.param.VesselParameterGroup.trackedVessel"),
                                ContractVesselTracker.GetDisplayName(vesselList.First())));
                        }
                        return check;
                    });
                    vesselListParam.Optional = true;
                    vesselListParam.fakeOptional = true;

                    AddParameter(vesselListParam);
                }
                else
                {
                    vesselListParam = new ParameterDelegate<Vessel>(hideVesselName ? "" : Localizer.GetStringByTag("#cc.param.VesselParameterGroup.anyVesselListEmpty"), v =>
                    {
                        bool check = VesselCanBeConsidered(v);
                        if (!hideVesselName)
                        {
                            if (check)
                            {
                                vesselListParam.SetTitle(Localizer.Format("#cc.param.VesselParameterGroup.anyVesselList", ParameterDelegate<Vessel>.GetDelegateText(vesselListParam)));
                            }
                            else
                            {
                                Localizer.GetStringByTag("#cc.param.VesselParameterGroup.anyVesselListEmpty");
                            }
                        }
                        return check;
                    });
                    vesselListParam.Optional = true;
                    vesselListParam.fakeOptional = true;

                    foreach (string vessel in vesselList)
                    {
                        ContractParameter childParam = new ParameterDelegate<Vessel>(ContractVesselTracker.GetDisplayName(vessel), v => false);
                        vesselListParam.AddParameter(childParam);
                    }

                    AddParameter(vesselListParam);
                }
            }
        }

        /// <summary>
        /// Checks the child parameters and updates state.
        /// </summary>
        /// <param name="vessel">The vessel to check the state for</param>
        public void UpdateState(Vessel vessel)
        {
            if (!enabled)
            {
                return;
            }

            LoggingUtil.LogVerbose(this, "-> UpdateState({0})", (vessel != null ? vessel.id.ToString() : "null"));

            // If this vessel doesn't match our list of valid vessels, ignore the update
            if (!VesselCanBeConsidered(vessel))
            {
                LoggingUtil.LogVerbose(this, "<- UpdateState (vessel cannot be considered)");

                // Set the tracked vessel in delegate parameters, if there is one (updates text)
                if (trackedVessel != null)
                {
                    ParameterDelegate<Vessel>.CheckChildConditions(this, trackedVessel);
                }

                return;
            }

            // Reset the tracked vessel if it no longer exists
            if (trackedVesselGuid != Guid.Empty && !FlightGlobals.Vessels.Any(v => v.id == trackedVesselGuid))
            {
                LoggingUtil.LogVerbose(this, "Tracked vessel no longer exists, resetting tracking.");
                waiting = false;
                trackedVessel = null;
                trackedVesselGuid = Guid.Empty;
            }

            // Ignore updates to non-tracked vessels if that vessel is already winning
            if (vessel != trackedVessel && (waiting || state == ParameterState.Complete))
            {
                // Make sure that the state of our tracked vessel has not suddenly changed
                SetChildState(trackedVessel);
                if (AllChildParametersComplete())
                {
                    LoggingUtil.LogVerbose(this, "<- UpdateState (tracked vessel has already completed parameter)");
                    return;
                }
            }

            // Temporarily change the state
            SetChildState(vessel);

            // Check if this is a completion
            if (AllChildParametersComplete())
            {
                trackedVessel = vessel;
                trackedVesselGuid = trackedVessel.id;
            }
            // Look at all other possible craft to see if we can find a winner
            else
            {
                trackedVessel = null;

                // Get a list of vessels to check
                Dictionary<Vessel, int> vessels = new Dictionary<Vessel, int>();
                foreach (VesselParameter p in AllDescendents<VesselParameter>())
                {
                    foreach (Vessel v in p.GetCompletingVessels())
                    {
                        if (v != vessel && VesselCanBeConsidered(v))
                        {
                            vessels[v] = 0;
                        }
                    }
                }

                // Check the vessels
                foreach (Vessel v in vessels.Keys)
                {
                    // Temporarily change the state
                    SetChildState(v);

                    // Do a check
                    if (AllChildParametersComplete())
                    {
                        trackedVessel = v;
                        trackedVesselGuid = trackedVessel.id;
                        break;
                    }
                }

                // Still no winner
                if (trackedVessel == null)
                {
                    // Use active
                    if (FlightGlobals.ActiveVessel != null && VesselCanBeConsidered(FlightGlobals.ActiveVessel))
                    {
                        SetChildState(FlightGlobals.ActiveVessel);
                        trackedVessel = FlightGlobals.ActiveVessel;
                        trackedVesselGuid = trackedVessel.id;
                    }
                }
            }

            // Force a VesselMeetsCondition call to update ParameterDelegate objects
            if (oldTrackedVessel != trackedVessel && trackedVessel != null)
            {
                foreach (ContractParameter p in this.GetAllDescendents())
                {
                    if (p is VesselParameter)
                    {
                        ((VesselParameter)p).CheckVesselMeetsCondition(trackedVessel);
                    }
                }
                oldTrackedVessel = trackedVessel;
            }

            if (trackedVessel != null)
            {
                // Set the tracked vessel in delegate parameters
                ParameterDelegate<Vessel>.CheckChildConditions(this, trackedVessel);
            }

            // Fire the parameter change event to account for all the changed child parameters.
            // We don't fire it for the child parameters, as any with a failed state will cause
            // the contract to fail, which we don't want.
            LoggingUtil.LogVerbose(this, "Firing OnParameterChange");
            ContractConfigurator.OnParameterChange.Fire(this.Root, this);

            // Manually run the OnParameterStateChange
            OnParameterStateChange(this);

            LoggingUtil.LogVerbose(this, "<- UpdateState (state possibly changed)");
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            if (!string.IsNullOrEmpty(define))
            {
                node.AddValue("define", define);
            }
            if (!string.IsNullOrEmpty(defineList))
            {
                node.AddValue("defineList", defineList);
            }
            foreach (string vesselName in vesselList)
            {
                node.AddValue("vessel", vesselName);
            }
            node.AddValue("duration", duration);
            if (waiting || state == ParameterState.Complete)
            {
                if (waiting)
                {
                    node.AddValue("completionTime", completionTime);
                }
            }
            if (trackedVessel != null)
            {
                node.AddValue("trackedVessel", trackedVesselGuid);
            }
            node.AddValue("dissassociateVesselsOnContractFailure", dissassociateVesselsOnContractFailure);
            node.AddValue("dissassociateVesselsOnContractCompletion", dissassociateVesselsOnContractCompletion);
            node.AddValue("resetChildrenWhenVesselDestroyed", resetChildrenWhenVesselDestroyed);
            if (hideVesselName)
            {
                node.AddValue("hideVesselName", hideVesselName);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                define = ConfigNodeUtil.ParseValue<string>(node, "define", null);
                defineList = ConfigNodeUtil.ParseValue<string>(node, "defineList", null);
                duration = Convert.ToDouble(node.GetValue("duration"));
                dissassociateVesselsOnContractFailure = ConfigNodeUtil.ParseValue<bool?>(node, "dissassociateVesselsOnContractFailure", (bool?)true).Value;
                dissassociateVesselsOnContractCompletion = ConfigNodeUtil.ParseValue<bool?>(node, "dissassociateVesselsOnContractCompletion", (bool?)false).Value;
                hideVesselName = ConfigNodeUtil.ParseValue<bool?>(node, "hideVesselName", (bool?)false).Value;
                resetChildrenWhenVesselDestroyed = ConfigNodeUtil.ParseValue<bool?>(node, "resetChildrenWhenVesselDestroyed", (bool?)false).Value;
                vesselList = ConfigNodeUtil.ParseValue<List<string>>(node, "vessel", new List<string>());
                if (node.HasValue("completionTime"))
                {
                    waiting = true;
                    completionTime = Convert.ToDouble(node.GetValue("completionTime"));
                }
                else
                {
                    waiting = false;
                }

                if (node.HasValue("trackedVessel"))
                {
                    trackedVesselGuid = new Guid(node.GetValue("trackedVessel"));
                    trackedVessel = FlightGlobals.Vessels.FirstOrDefault(v => v != null && v.id == trackedVesselGuid);
                    if (trackedVessel == null)
                    {
                        trackedVesselGuid = Guid.Empty;
                    }
                }

                // Register these early, otherwise we'll miss the event
                if (resetChildrenWhenVesselDestroyed && Root.ContractState == Contract.State.Active)
                {
                    ContractVesselTracker.OnKeyedVesselDestroyed.Add(OnKeyedVesselDestroyed);
                }

                // Create the parameter delegate for the vessel list
                CreateVesselListParameter();
            }
            finally
            {
                ParameterDelegate<Vessel>.OnDelegateContainerLoad(node);
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselChange.Add(OnVesselChange);
            ContractVesselTracker.OnVesselAssociation.Add(OnVesselAssociation);
            ContractVesselTracker.OnVesselDisassociation.Add(OnVesselDisassociation);

            GameEvents.Contract.onCompleted.Add(OnContractCompleted);
            GameEvents.Contract.onFailed.Add(OnContractFailed);
            GameEvents.Contract.onCancelled.Add(OnContractFailed);

            // Add a waypoint for each possible vessel in the list
            foreach (string vesselKey in vesselList)
            {
                VesselWaypoint vesselWaypoint = new VesselWaypoint(Root, vesselKey);
                vesselWaypoints.Add(vesselWaypoint);
                vesselWaypoint.Register();
            }
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselChange.Remove(OnVesselChange);
            ContractVesselTracker.OnVesselAssociation.Remove(OnVesselAssociation);
            ContractVesselTracker.OnVesselDisassociation.Remove(OnVesselDisassociation);

            // Leave to catch late events
            if (state != ParameterState.Complete)
            {
                GameEvents.Contract.onCompleted.Remove(OnContractCompleted);
                GameEvents.Contract.onFailed.Remove(OnContractFailed);
                GameEvents.Contract.onCancelled.Remove(OnContractFailed);
            }

            ContractVesselTracker.OnKeyedVesselDestroyed.Remove(OnKeyedVesselDestroyed);

            foreach (VesselWaypoint vesselWaypoint in vesselWaypoints)
            {
                vesselWaypoint.Unregister();
            }
        }

        protected void OnContractCompleted(Contract c)
        {
            if (c == Root)
            {
                if (dissassociateVesselsOnContractCompletion && !string.IsNullOrEmpty(define) && trackedVessel != null)
                {
                    LoggingUtil.LogVerbose(this, "Removing defined vessel {0}", define);
                }

                if (!string.IsNullOrEmpty(defineList) && trackedVessel != null)
                {
                    // Create a vessel association
                    string vesselId = "Vessel" + trackedVessel.id.ToString();
                    ContractVesselTracker.Instance.AssociateVessel(vesselId, trackedVessel);

                    // Add to the vessel store
                    List<VesselIdentifier> vesselStore = PersistentDataStore.Instance.Retrieve<List<VesselIdentifier>>(defineList) ?? new List<VesselIdentifier>();
                    vesselStore.Add(new VesselIdentifier(vesselId));
                    PersistentDataStore.Instance.Store<List<VesselIdentifier>>(defineList, vesselStore);
                }
            }
        }

        protected void OnContractFailed(Contract c)
        {
            if (c == Root && dissassociateVesselsOnContractFailure && !string.IsNullOrEmpty(define) && trackedVessel != null)
            {
                LoggingUtil.LogVerbose(this, "Removing defined vessel {0}", define);
                ContractVesselTracker.Instance.AssociateVessel(define, null);
            }
        }

        protected void OnVesselAssociation(GameEvents.HostTargetAction<Vessel, string> hta)
        {
            // If it's the tracked vessel
            if (define == hta.target)
            {
                if (trackedVessel != hta.host)
                {
                    // It's the new tracked vessel
                    trackedVessel = hta.host;
                    trackedVesselGuid = hta.host.id;

                    // Try it out
                    UpdateState(hta.host);
                }
            }
            // If it's a vessel we're looking for
            else if (vesselList.Contains(hta.target))
            {
                // Try it out
                UpdateState(hta.host);

                // Potentially force a title update
                GetTitle();
            }
        }

        protected void OnVesselDisassociation(GameEvents.HostTargetAction<Vessel, string> hta)
        {
            // If it's a vessel we're looking for, and it's tracked
            if (vesselList.Contains(hta.target) && define == hta.target)
            {
                waiting = false;
                trackedVessel = null;
                trackedVesselGuid = Guid.Empty;

                // Try out the active vessel
                UpdateState(FlightGlobals.ActiveVessel);

                // Active vessel didn't work out - force the children to be incomplete
                if (trackedVessel == null)
                {
                    SetChildState(null);

                    // Fire the parameter change event to account for all the changed child parameters.
                    // We don't fire it for the child parameters, as any with a failed state will cause
                    // the contract to fail, which we don't want.
                    ContractConfigurator.OnParameterChange.Fire(this.Root, this);

                    // Manually run the OnParameterStateChange
                    OnParameterStateChange(this);
                }
            }
        }

        protected void OnKeyedVesselDestroyed(GameEvents.HostTargetAction<Vessel, string> hta)
        {
            string vesselKey = hta.target;
            if (define == vesselKey)
            {
                foreach (ContractParameter param in this.GetAllDescendents())
                {
                    param.Enable();
                    if (param is ContractConfiguratorParameter ccp)
                        ccp.SetState(ParameterState.Incomplete);
                    param.Enable();   // Just in case
                    param.Reset();
                }

                Enable();
                SetState(ParameterState.Incomplete);
                Enable();   // Ugh, don't ask why
                Reset();
            }
        }

        protected void OnVesselChange(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "OnVesselChange({0}), Active = ", (vessel != null && vessel.id != null ? vessel.id.ToString() : "null"),
                (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.id != null ? FlightGlobals.ActiveVessel.id.ToString() : "null"));
            UpdateState(vessel);
        }

        protected override void OnParameterStateChange(ContractParameter contractParameter)
        {
            if (System.Object.ReferenceEquals(contractParameter.Parent, this) ||
                System.Object.ReferenceEquals(contractParameter, this))
            {
                LoggingUtil.LogVerbose(this, "OnParameterStateChange");
                if (AllChildParametersComplete())
                {
                    LoggingUtil.LogVerbose(this, "    AllChildParametersComplete (waiting = {0})", waiting);
                    if (!waiting && trackedVessel != null)
                    {
                        LoggingUtil.LogVerbose(this, "    set waiting");
                        waiting = true;
                        completionTime = Planetarium.GetUniversalTime() + duration;

                        // Set the tracked vessel association
                        if (!string.IsNullOrEmpty(define))
                        {
                            LoggingUtil.LogVerbose(this, "setting {0} as {1}", define, trackedVessel.vesselName);
                            ContractVesselTracker.Instance.AssociateVessel(define, trackedVessel);
                        }
                    }
                }
                else
                {
                    LoggingUtil.LogVerbose(this, "    not all params complete");
                    waiting = false;
                    if (state == ParameterState.Complete)
                    {
                        SetState(ParameterState.Incomplete);
                    }

                    // Set the tracked vessel association
                    if (!string.IsNullOrEmpty(define))
                    {
                        LoggingUtil.LogVerbose(this, "setting {0} as null", define);
                        ContractVesselTracker.Instance.AssociateVessel(define, null);
                    }

                    // Find any failed non-VesselParameter parameters
                    for (int i = 0; i < ParameterCount; i++)
                    {
                        ContractParameter param = GetParameter(i);
                        if (!param.GetType().IsSubclassOf(typeof(VesselParameter)) && param.State == ParameterState.Failed)
                        {
                            SetState(ParameterState.Failed);
                            break;
                        }
                    }
                }
            }
        }

        protected override void OnUpdate()
        {
            if (waiting && Planetarium.GetUniversalTime() > completionTime)
            {
                SetState(ParameterState.Complete);
                if (state == ParameterState.Complete)
                {
                    waiting = false;
                }
            }
            // Every time the clock ticks over, make an attempt to update the contract window
            // notes.  We do this because otherwise the window will only ever read the notes once,
            // so this is the only way to get our fancy timer to work.
            else if (waiting && trackedVessel != null)
            {
                if (Planetarium.GetUniversalTime() - lastUpdate > 1.0f)
                {
                    lastUpdate = Planetarium.GetUniversalTime();

                    // Force a call to GetTitle to update the contracts app
                    GetTitle();

                    if (durationParameter != null)
                    {
                        durationParameter.SetTitle(Localizer.Format("#cc.param.VesselParameterGroup.time", DurationUtil.StringValue(completionTime - Planetarium.GetUniversalTime())));
                    }
                }
            }
            else
            {
                if (durationParameter != null)
                {
                    durationParameter.SetTitle(Localizer.Format("#cc.param.VesselParameterGroup.duration", DurationUtil.StringValue(duration)));
                }
            }
        }

        protected IEnumerable<T> AllDescendents<T>() where T : ContractParameter
        {
            return AllDescendents<T>(this);
        }

        protected static IEnumerable<T> AllDescendents<T>(ContractParameter p) where T : ContractParameter
        {
            for (int i = 0; i < p.ParameterCount; i++)
            {
                ContractParameter child = p.GetParameter(i);
                if (child is T)
                {
                    yield return child as T;
                }
                foreach (ContractParameter grandChild in AllDescendents<T>(child))
                {
                    yield return grandChild as T;
                }
            }
        }

        /// <summary>
        /// Set the state in all children to that of the given vessel.
        /// </summary>
        /// <param name="vessel">Vessel to use for the state change</param>
        protected void SetChildState(Vessel vessel)
        {
            foreach (VesselParameter p in AllDescendents<VesselParameter>())
            {
                p.SetState(vessel);
            }
        }

        /// <summary>
        /// Checks whether the given veseel can be considered for completion of this group.  Checks
        /// the vessel inclusion list.
        /// </summary>
        /// <param name="vessel">The vessel to check.</param>
        /// <returns>Whether we can continue with this vessel.</returns>
        private bool VesselCanBeConsidered(Vessel vessel)
        {
            if (vesselList.Any())
            {
                return vesselList.Any(key => ContractVesselTracker.Instance.GetAssociatedVessel(key) == vessel);
            }
            // If the vessel is already in the define list, don't allow it to be considered again
            else if (!string.IsNullOrEmpty(defineList))
            {
                List<VesselIdentifier> vesselStore = PersistentDataStore.Instance.Retrieve<List<VesselIdentifier>>(defineList);
                if (vesselStore == null)
                {
                    return true;
                }

                IEnumerable<string> vesselKeys = ContractVesselTracker.Instance.GetAssociatedKeys(vessel);
                return !vesselStore.Any(vi => vesselKeys.Contains(vi.identifier));
            }

            return true;
        }
    }
}
