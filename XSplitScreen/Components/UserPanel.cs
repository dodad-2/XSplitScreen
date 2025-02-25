using DoDad.XLibrary.Components;
using DoDad.XLibrary.Toolbox;
using DoDad.XSplitScreen.Assignments;
using Rewired;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class UserPanel : MonoBehaviour
    {
        #region Variables
        public const int addButtonSize = 48;
        public const int removeButtonSize = 56;
        private const bool hideProfileNames = false;
        public static readonly float2 dropdownScale = new float2(0.955f, 0.95f);
        private static Sprite addSprite;
        private static Sprite xMarkSprite;
        private static Sprite lockSprite;
        public Transform iconTarget => transform;
        public Assignment current { get; private set; }

        public int2 position { get; internal set; }

        internal HGButton removeButton { get; private set; }
        internal XButton[] addButtons { get; private set; }
        internal MPDropdown dropdown { get; private set; }
        internal SettingsSubPanel subPanel { get; private set; }
        private Image screen;
        private ErrorResolver resolver;
        #endregion

        #region Unity Methods
        void Update()
        {
            if (current != null && resolver != null && current.error != ErrorType.NONE && resolver.isResolved)
            {
                resolver.token = $"XSS_ERROR_{current.error}";
                resolver.isResolved = false;
            }
        }
        void OnEnable()
        {
            removeButton?.GetComponent<MPEventSystemLocator>().Awake();
            dropdown?.GetComponent<MPEventSystemLocator>().Awake();
        }
        void OnDestroy()
        {
            UserManager.onSplitscreenEnabled -= OnSplitscreenEnabled;
            UserManager.onSplitscreenDisabled -= OnSplitscreenDisabled;
        }
        #endregion

        #region Initialize
        public void Initialize(float panelSizeDelta)
        {
            panelSizeDelta /= 2;

            if (addSprite == null)
                addSprite = Instantiate(XLibrary.Resources.GetSprite("plus"));

            removeButton = Instantiate(XLibrary.Resources.GetPrefabUI("SimpleButton")).GetComponent<HGButton>();
            removeButton.name = "(SimpleButton) Remove";
            removeButton.transform.SetParent(transform);
            xMarkSprite = Instantiate(XLibrary.Resources.GetSprite("xmark"));
            lockSprite = Instantiate(XLibrary.Resources.GetSprite("lock"));
            removeButton.GetComponent<Image>().sprite = xMarkSprite;
            removeButton.GetComponent<Image>().SetNativeSize();

            RectTransform removeRect = removeButton.GetComponent<RectTransform>();

            removeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, removeButtonSize);
            removeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, removeButtonSize);
            removeButton.transform.localPosition = new Vector3(panelSizeDelta - (removeButtonSize / 2f), panelSizeDelta - (removeButtonSize / 2f), 0);

            removeButton.onClick.AddListener(RemovePanel);
            removeButton.gameObject.SetActive(false);

            addButtons = new XButton[5];

            for (int e = 0; e < 5; e++)
            {
                addButtons[e] = new GameObject("(XButton) Add", typeof(RectTransform), typeof(Image), typeof(XButton), typeof(LayoutElement)).GetComponent<XButton>();
                addButtons[e].transform.SetParent(transform);

                Vector3 position = Vector3.zero;

                float distance = panelSizeDelta * 1.5f;

                if (e != 4)
                {
                    switch (e)
                    {
                        case 0:
                            position.y += distance;
                            break;
                        case 1:
                            position.y -= distance;
                            break;
                        case 2:
                            position.x -= distance;
                            break;
                        case 3:
                            position.x += distance;
                            break;
                    }
                }

                addButtons[e].transform.localPosition = position;

                addButtons[e].GetComponent<LayoutElement>().ignoreLayout = true;
                addButtons[e].GetComponent<Image>().sprite = addSprite;
                addButtons[e].GetComponent<Image>().color = new Color(1, 1, 1, 0.6f);
                addButtons[e].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, addButtonSize);
                addButtons[e].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, addButtonSize);
                addButtons[e].GetComponent<XButton>().onSubmit += AddPanel;
                addButtons[e].GetComponent<XButton>().onPointerUp += AddPanel;
                addButtons[e].gameObject.SetActive(false);
            }

            removeButton.transform.SetAsLastSibling();

            dropdown = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.Dropdown)).GetComponent<MPDropdown>();
            dropdown.name = "(Dropdown) Profile";
            dropdown.transform.SetParent(transform);
            dropdown.GetComponent<Image>().SetNativeSize();
            dropdown.transform.localScale = new Vector3(dropdownScale.x, dropdownScale.y, 1);
            dropdown.onValueChanged.AddListener(SwapProfiles);

            var toggleTemplate = dropdown.GetComponentInChildren<Toggle>(true);

            toggleTemplate.gameObject.AddComponent<MultiInputHelper>().toggle = toggleTemplate;

            screen = GetComponent<Image>();
            screen.enabled = false;

            var subPanelObject = new GameObject("SubPanel");
            subPanelObject.transform.SetParent(transform);
            subPanelObject.transform.localPosition = Vector3.zero;
            subPanel = subPanelObject.AddComponent<SettingsSubPanel>();
            subPanel.Initialize(this, panelSizeDelta);

            resolver = GetComponent<ErrorResolver>();
            resolver.focus = transform;
            resolver.offset = new Vector3(0, 0/*panelSizeDelta*/, 0);

            UserManager.onSplitscreenEnabled += OnSplitscreenEnabled;
            UserManager.onSplitscreenDisabled += OnSplitscreenDisabled;

			/*if (Plugin.active) // 4.0.0 rewrite 9-12-24
                OnSplitscreenEnabled();
            else
                OnSplitscreenDisabled();*/
		}
		#endregion

		#region Event Handlers
		public void OnSubPanelFunction()
        {
            subPanel.SetOpenState(!subPanel.open);
            ShowRemoveButton(!subPanel.open);
            ShowProfileDropdown(!subPanel.open);
            ClearErrorState(ErrorType.NONE);
        }
        public void OnCheckAddButtons()
        {
            ShowAddButtons();

            if (!AssignmentManager.canAddUsers)
                return;

            var node = AssignmentWindow.graph.nodeGraph.GetNode(position);

            if (node.nodeType == NodeType.Primary)
            {
                if (current == null)
                {
                    if (AssignmentManager.AssignedToDisplay(AssignmentWindow.currentDisplay) == 0)
                    {
                        ShowAddButton(4, true);
                        return;
                    }
                }
                else
                {
                    ShowAddButtons(true, 4);
                }
            }

            if (current == null)
                return;

            if (node.neighborUp != null && node.neighborUp.data == null && node.neighborUp.nodeType == NodeType.None)
                ShowAddButton(0, true);
            if (node.neighborDown != null && node.neighborDown.data == null && node.neighborDown.nodeType == NodeType.None)
                ShowAddButton(1, true);
            if (node.neighborLeft != null && node.neighborLeft.data == null && node.neighborLeft.nodeType == NodeType.None)
                ShowAddButton(2, true);
            if (node.neighborRight != null && node.neighborRight.data == null && node.neighborRight.nodeType == NodeType.None)
                ShowAddButton(3, true);
        }
        public void OnSplitscreenEnabled()
        {
            SetRemoveButtonState(false);
            dropdown.interactable = false;

            foreach (var button in addButtons)
                button.interactable = false;
        }
        public void OnSplitscreenDisabled()
        {
            SetRemoveButtonState(true);
            dropdown.interactable = true;

            foreach (var button in addButtons)
                button.interactable = true;
        }
        #endregion

        #region Public Methods
        public void ShowUserPanel(bool status)
        {
            screen.enabled = status;
            ShowRemoveButton(status);
            ShowProfileDropdown(status);
            ShowSubPanel(status);
        }
        public void AssignController(Controller controller)
        {
            current.controller = controller;

            ClearErrorState(ErrorType.CONTROLLER);
        }
        public void AssignProfile(Assignment assignment, bool unassignCurrent = true)
        {
            if (current != null && unassignCurrent)
                current.position = int2.negative;

            current = assignment;

            ShowUserPanel(current != null);

            ClearErrorState(current == null ? ErrorType.NONE : ErrorType.PROFILE);
        }
        #endregion

        #region Helpers
        private void SetRemoveButtonState(bool state)
        {
            var removeButton = this.removeButton.GetComponent<Image>();

            removeButton.raycastTarget = state;

            if (state)
                removeButton.sprite = xMarkSprite;
            else
                removeButton.sprite = lockSprite;
        }
        private void UpdateProfileDropdown()
        {
            dropdown.ClearOptions();

            var options = new List<string>();
            int id = -1;

            for (int e = 0; e < AssignmentManager.assignments.Count; e++)
            {
                options.Add(hideProfileNames ? $"Profile {e + 1}" : PlatformSystems.saveSystem.GetProfile(AssignmentManager.assignments[e].profile).name);

                if (AssignmentManager.assignments[e].Equals(current))
                    id = e;
            }

            dropdown.AddOptions(options);

            if (id != -1)
                dropdown.SetValueWithoutNotify(id);
        }
        private void ShowAddButtons(bool status = false, int range = 5)
        {
            for (int e = 0; e < range; e++)
                ShowAddButton(e, status);
        }
        #endregion

        #region Event Handlers
        public void SwapProfiles(int profileIndex)
        {
            AssignmentWindow.SwapAssignments(current, AssignmentManager.assignments[profileIndex]);
        }
        public void AddPanel(XButton button, BaseInputModule inputModule)
        {
            var assignment = AssignmentManager.GetFreeAssignment();

            if (assignment != null)
            {
                var node = AssignmentWindow.graph.nodeGraph.GetNode(position);

                var neighborPosition = position;

                if (node != null && node.neighborUp != null && button != null && button.transform.GetSiblingIndex() == 0)
                    neighborPosition = node.neighborUp.position;
                if (node != null && node.neighborDown != null && button != null && button.transform.GetSiblingIndex() == 1)
                    neighborPosition = node.neighborDown.position;
                if (node != null && node.neighborLeft != null && button != null && button.transform.GetSiblingIndex() == 2)
                    neighborPosition = node.neighborLeft.position;
                if (node != null && node.neighborRight != null && button != null && button.transform.GetSiblingIndex() == 3)
                    neighborPosition = node.neighborRight.position;

				/*if (AssignmentWindow.graph.nodeGraph.GetNode(neighborPosition).data == null) // 4.0.0 rewrite 9-12-24
                    AssignmentWindow.AssignUser(assignment, neighborPosition);
                else
                    Log.LogOutput($"Unable to assign user: assignment already exists.");*/
			}
			else
            {
                if (PlatformSystems.saveSystem.loadedUserProfiles.Count < 2)
                {
                    assignment.error = ErrorType.PROFILE_2;
                    resolver.isResolved = false;
                }

				//Log.LogOutput($"No unassigned users exist."); // 4.0.0 rewrite 9-12-24
			}
		}
        public void RemovePanel()
        {
            AssignmentWindow.UnassignUser(current);
        }
        public void ResetPanel()
        {
            AssignProfile(null, false);
        }
        public void ShowAddButton(int id, bool status)
        {
            addButtons[id].gameObject.SetActive(status);
        }
        public void ShowRemoveButton(bool status)
        {
            removeButton.gameObject.SetActive(status);
        }
        public void ShowSubPanel(bool status)
        {
            subPanel.gameObject.SetActive(status);
            subPanel.SetOpenState(false);
        }
        public void ShowProfileDropdown(bool status)
        {
            if (status)
                UpdateProfileDropdown();

            Vector2 dropDelta = dropdown.GetComponent<RectTransform>().sizeDelta;
            Vector2 panelDelta = GetComponent<RectTransform>().sizeDelta;

            Vector3 position = new Vector3(dropDelta.x / 2, -((panelDelta.y / 2) - (dropDelta.y / 2)), 0);
            position.x *= dropdownScale.x;
            position.y *= 2 - dropdownScale.y;
            position.y += 5;

            dropdown.transform.localPosition = position;
            dropdown.gameObject.SetActive(status);
        }
        #endregion

        #region Helpers
        private void ClearErrorState(ErrorType error)
        {
            if (current != null && current.error == error)
            {
                current.error = ErrorType.NONE;
                error = ErrorType.NONE;
            }

            if (error == ErrorType.NONE)
                resolver.isResolved = true;
        }
        #endregion
    }
}
