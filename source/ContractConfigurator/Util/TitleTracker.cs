using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KSP.UI;
using KSP.UI.Screens;
using Contracts;
using UnityEngine.UI;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for tracking and changing parameter titles in the stock contract app
    /// </summary>
    public class TitleTracker
    {
        /// <summary>
        /// Class to cache reflected field from the contracts app.
        /// </summary>
        [KSPAddon(KSPAddon.Startup.EveryScene, false)]
        private class TitleTrackerHelper : MonoBehaviour
        {
            internal static TitleTrackerHelper Instance;
            private static readonly GameScenes[] validScenes = new GameScenes[] {
                GameScenes.EDITOR,
                GameScenes.FLIGHT,
                GameScenes.SPACECENTER,
                GameScenes.TRACKSTATION
            };

            private static readonly FieldInfo contractsField = typeof(ContractsApp).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => mi.FieldType == typeof(Dictionary<Guid, UICascadingList.CascadingListItem>))
                .First();
            private Dictionary<Guid, UICascadingList.CascadingListItem> _uiListMap = null;

            public static Dictionary<Guid, UICascadingList.CascadingListItem> UIListMap
            {
                get
                {
                    if (Instance._uiListMap == null)
                    {
                        Instance._uiListMap = (Dictionary<Guid, UICascadingList.CascadingListItem>)contractsField.GetValue(ContractsApp.Instance);
                    }
                    return Instance._uiListMap;
                }
            }

            void Awake()
            {
                if (!validScenes.Contains(HighLogic.LoadedScene))
                {
                    Destroy(this);
                }
                Instance = this;
            }
        }

        private readonly ContractParameter parameter;
        private readonly List<string> titles = new List<string>();
        private TMPro.TextMeshProUGUI text;
        private LayoutElement layoutElement;

        public TitleTracker(ContractParameter parameter)
        {
            this.parameter = parameter;
        }

        public void RegisterToEvents()
        {
            GameEvents.Contract.onParameterChange.Add(OnParameterChange);
            GameEvents.onVesselRename.Add(OnVesselRename);
        }

        public void UnregisterFromEvents()
        {
            GameEvents.onVesselRename.Remove(OnVesselRename);
            TitleTrackerHelper.Instance.StartCoroutine(UnregisterEventsAtEndOfFrame());
        }

        /// <summary>
        /// Used for unbinding the onParameterChange event at the end of frame since otherwise this could happen during the event firing process and thus cause exceptions.
        /// </summary>
        /// <returns></returns>
        private IEnumerator UnregisterEventsAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            GameEvents.Contract.onParameterChange.Remove(OnParameterChange);
        }

        private void OnParameterChange(Contract c, ContractParameter p)
        {
            if (c == parameter.Root)
            {
                text = null;
            }
        }

        protected void OnVesselRename(GameEvents.HostedFromToAction<Vessel, string> hft)
        {
            // Many parameters have the vessel name in the title, so force a refresh
            if (text != null)
            {
                if (text.text.Contains(hft.from))
                {
                    _ = parameter.Title;
                }
            }
            // No cached value, assume a refresh required
            else
            {
                _ = parameter.Title;
            }
        }

        /// <summary>
        /// Add a title to the TitleTracker to be tracked.  Call this in GetTitle() before returning a new string.
        /// </summary>
        /// <param name="title">The title to add</param>
        public void Add(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return;
            }
            titles.AddUnique(title);
        }

        /// <summary>
        /// Call this any time the title text has changed - this will make an attempt to update
        /// the contract window title.  We do this because otherwise the window will only ever read
        /// the title once.
        /// </summary>
        /// <param name="newTitle">New title to display</param>
        public void UpdateContractWindow(string newTitle)
        {
            // Get the cascading list for our contract
            if (text == null && ContractsApp.Instance != null)
            {
                UICascadingList.CascadingListItem list = TitleTrackerHelper.UIListMap.ContainsKey(parameter.Root.ContractGuid) ? TitleTrackerHelper.UIListMap[parameter.Root.ContractGuid] : null;

                if (list != null)
                {
                    foreach (KSP.UI.UIListItem item in list.items)
                    {
                        TMPro.TextMeshProUGUI textComponent = item.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).FirstOrDefault();
                        if (textComponent != null)
                        {
                            // Check for any string in titleTracker
                            foreach (string title in titles)
                            {
                                if (textComponent.text.EndsWith(">" + title + "</color>"))
                                {
                                    text = textComponent;
                                    layoutElement = item.GetComponentsInChildren<LayoutElement>(true).FirstOrDefault();
                                    break;
                                }
                            }

                            if (text != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            if (text)
            {
                // Clear the titleTracker, and replace the text
                if (!text.text.Contains(">" + newTitle + "<"))
                {
                    float preHeight = text.preferredHeight;
                    
                    titles.Clear();
                    text.text = text.text.Substring(0, text.text.IndexOf(">") + 1) + newTitle + "</color>";
                    titles.Add(newTitle);

                    float postHeight = text.preferredHeight;

                    if (preHeight != postHeight)
                    {
                        text.rectTransform.sizeDelta = new Vector2(text.rectTransform.sizeDelta.x, postHeight + 4f);
                        layoutElement.preferredHeight = postHeight + 6f;

                        // Force an update to the layout even when not active
                        if (!layoutElement.IsActive())
                        {
                            LayoutRebuilder.MarkLayoutForRebuild(layoutElement.transform as RectTransform);
                        }
                    }
                }
            }

            // Contracts Window + update
            ContractsWindow.SetParameterTitle(parameter, newTitle);
        }
    }
}
