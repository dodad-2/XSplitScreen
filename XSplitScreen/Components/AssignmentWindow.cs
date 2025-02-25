using DoDad.XLibrary.Toolbox;
using DoDad.XSplitScreen.Assignments;
using Rewired;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DoDad.XLibrary.Components;
using RoR2.UI;
using System.Collections.Generic;

namespace DoDad.XSplitScreen.Components
{
    class AssignmentWindow : MonoBehaviour
    {
        #region Variables
        internal static AssignmentGraph graph { get; private set; }
        internal static DisplayAssignmentsPanel displayPanel { get; private set; }
        internal static OptionsPanel optionsPanel { get; private set; }
        internal static ControllerPanel controllerPanel { get; private set; }
        internal static ErrorPanel errorPanel { get; private set; }
        internal static int currentDisplay => graph.currentDisplay;

        private static RectTransform errorDescriptionLayout;
        private static Image errorWarningImage;
        private static ErrorResolver resolver;

        private static int updateErrorOffsetAfterFrames = 1;
        private static int frameCount = 0;
        #endregion

        #region Unity Methods
        void OnEnable()
        {
            GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Display.main.renderingHeight * 0.9f);//0.8f);
            GetComponent<RectTransform>().localScale = Vector3.one;
        }
        void Awake()
        {
            GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.1f);
            GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.95f);
            /*
                        VerticalLayoutGroup verticalLayoutGroup = gameObject.GetComponent<VerticalLayoutGroup>();

                        verticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                        verticalLayoutGroup.childControlHeight = true;
                        verticalLayoutGroup.childControlWidth = true;
                        verticalLayoutGroup.childScaleHeight = true;
                        verticalLayoutGroup.childScaleWidth = true;
                        verticalLayoutGroup.childForceExpandHeight = false;
                        verticalLayoutGroup.childForceExpandWidth = false;
            */
            graph = new AssignmentGraph();

            controllerPanel = gameObject.AddComponent<ControllerPanel>();

            displayPanel = gameObject.AddComponent<DisplayAssignmentsPanel>();
            displayPanel.Initialize();

            optionsPanel = new GameObject("Options Panel", typeof(RectTransform), typeof(LayoutElement), typeof(OptionsPanel)).GetComponent<OptionsPanel>();
            optionsPanel.transform.SetParent(transform);
            optionsPanel.transform.localPosition = new Vector3(0, -320, 0);
            optionsPanel.GetComponent<OptionsPanel>().Initialize();

            resolver = gameObject.AddComponent<ErrorResolver>();
            resolver.focus = optionsPanel.splitscreenToggle.transform;

            var warning = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.SimpleImage).gameObject);
            warning.AddComponent<ErrorPanel>().useResolverOffset = false;
            warning.transform.SetParent(AssignmentScreen.overlayContainer);
            warning.transform.localPosition = Vector3.one * -100f;
            warning.name = "(Image) Warning Triangle";

            errorWarningImage = warning.GetComponent<Image>();
            errorWarningImage.sprite = Instantiate(XLibrary.Resources.GetSprite("warning"));
            errorWarningImage.SetNativeSize();

            var colorShifter = warning.AddComponent<ColorShifter>();
            colorShifter.colorCycles.Add(new Color[2]
                {
                    new Color(1f, 0.5f, 0),
                    Color.yellow,
                });
            colorShifter.target = errorWarningImage;
            colorShifter.cycleIndex = 0;
            colorShifter.cycleSpeed = 0.25f;

            warning.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, UserPanel.removeButtonSize);
            warning.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, UserPanel.removeButtonSize);
            warning.SetActive(true);

            errorPanel = new GameObject("Error Panel", typeof(RectTransform), typeof(LayoutElement), typeof(ErrorPanel)).GetComponent<ErrorPanel>();
            errorPanel.GetComponent<LayoutElement>().ignoreLayout = true;
            errorPanel.transform.SetParent(AssignmentScreen.overlayContainer);
            errorPanel.transform.position = Vector3.one * -100f;

            var descriptionPanel = GameObject.Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.Description));

            var descriptionRect = descriptionPanel.GetComponent<RectTransform>();
            descriptionRect.anchorMax = new Vector2(1f, 0.5f);
            descriptionRect.anchorMin = descriptionRect.anchorMax;
            descriptionRect.pivot = new Vector2(0, 0.5f);

            descriptionPanel.transform.SetParent(errorPanel.transform);
            descriptionRect.transform.localPosition = Vector3.zero;
            descriptionPanel.gameObject.SetActive(true);
            descriptionPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
            //descriptionPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300);

            errorPanel.text = descriptionPanel.GetComponentInChildren<LanguageTextMeshController>();
            //errorPanel.speed = 0.05f;

            errorDescriptionLayout = errorPanel.GetComponentInChildren<VerticalLayoutGroup>().GetComponent<RectTransform>();
            var disable = errorPanel.GetComponentInChildren<DisableIfTextIsEmpty>();
            GameObject[] objects = new GameObject[disable.gameObjects.Length + 1];
            disable.gameObjects.CopyTo(objects, 0);
            objects[disable.gameObjects.Length] = warning;
            disable.gameObjects = objects;

            InitializeGraph();

            Display.onDisplaysUpdated += OnDisplaysUpdated;
            AssignmentManager.onAssignmentsChanged += OnAssignmentsChanged;
            ErrorResolver.onNewError += OnNewError;
        }
        void LateUpdate()
        {
            if (frameCount == 0 && ErrorResolver.unresolved.Count > 0)
            {
                var warningPanel = errorWarningImage.GetComponent<ErrorPanel>();

                var focus = Input.lastEventSystem == null ? MPEventSystem.instancesList[0].cursorIndicatorController?.currentChildIndicator?.transform : Input.lastEventSystem.cursorIndicatorController?.currentChildIndicator?.transform;

                if (focus == null)
                    focus = optionsPanel.splitscreenToggle.transform;

                if (!AssignmentManager.firstLaunch)
                    resolver.focus = focus;

                var warningSizeDelta = errorWarningImage.GetComponent<RectTransform>().sizeDelta;

                if (AssignmentManager.firstLaunch)
                    warningSizeDelta.x *= 1.5f;

                if (!resolver.isResolved)
                    warningPanel.offset.x = 15f + (warningSizeDelta.x / 1.8f);
                else
                    warningPanel.offset.x = 0;

                var offset = Vector3.zero;

                offset.x = warningPanel.offset.x + (warningSizeDelta.x / 2f);
                offset.y = Mathf.Abs((errorDescriptionLayout.sizeDelta.y)) * 1.1f;

                errorPanel.offset = offset;

                foreach (var controller in ReInput.controllers.Controllers)
                {
                    if (controller.GetAnyButtonDown())
                    {
                        foreach (var assignment in AssignmentManager.assignments)
                            assignment.error = ErrorType.NONE;

                        ErrorResolver.ResolveAllErrors();
                        AssignmentManager.firstLaunch = false;

                        warningPanel.offset.x = 0;
                        break;
                    }
                }
            }

            if (frameCount > 0)
                frameCount--;
        }
        void OnDestroy()
        {
            Display.onDisplaysUpdated -= OnDisplaysUpdated;
            AssignmentManager.onAssignmentsChanged -= OnAssignmentsChanged;
            ErrorResolver.onNewError -= OnNewError;
        }
        #endregion

        #region Initialization
        private void InitializeGraph()
        {
            graph.LoadAssignmentsForDisplay();

            if (AssignmentManager.assignedUsers == 0)
            {
                if (AssignmentManager.firstLaunch)
                {
                    resolver.focus = displayPanel.center.transform;
                    resolver.token = "XSS_ERROR_FIRSTTIME";
                    resolver.isResolved = false;
                }
                /*
                var assignment = AssignmentManager.GetFirstLocalAssignment();

                assignment.controller = GetFirstAvailableController();
                graph.AssignUser(assignment, int2.one);

                SaveGraph();
                */
            }
            else
            {
                foreach (Assignment assignment in AssignmentManager.assignments.Where(x => x.position.IsPositive() && x.display > -1))
                {
                    if (assignment.controller == null)
                        assignment.controller = GetFirstAvailableController();

					//Log.LogOutput($"Assignment {assignment.position}, display {assignment.display} controller = {assignment.controller?.name}"); // 4.0.0 rewrite 9-12-24
				}
			}

            OnAssignmentsChanged();

            graph.PrintGraph();
        }
        #endregion

        #region Event Handlers
        private static void OnNewError(ErrorResolver newError)
        {
            frameCount = 1;
            //updateErrorOffsetAfterFrames = true;
        }
        private static void OnAssignmentsChanged()
        {
            OnAssignmentsChanged(0);
        }
        private static void OnAssignmentsChanged(int display = 0)
        {
            graph.LoadAssignmentsForDisplay(display);
            displayPanel.LoadFromGraph();
            controllerPanel.UpdateIcons();
            optionsPanel.OnAssignmentsUpdated();
        }
        private static void OnDisplaysUpdated()
        {
            OnAssignmentsChanged();
        }
        #endregion

        #region Public Methods
        public static void SetSplitscreenEnabled(bool enabled)
        {
            List<Assignment> invalid = new List<Assignment>();
            ErrorType pluginError;

            if (enabled)
            {
                if (UserManager.AssignSplitscreenUsers(AssignmentManager.assignments.Where(x => x.position.IsPositive()).ToList(), out invalid, out pluginError))
                {
					//Log.LogOutput($"Splitscreen enabled", Log.LogLevel.Message); // 4.0.0 rewrite 9-12-24
				}
				else
                {
                    var invalidAlternateDisplay = invalid.Where(x => x.display != graph.currentDisplay && x.error != ErrorType.NONE);

                    if (invalidAlternateDisplay.Count() > 0)
                        RequestDisplay(invalidAlternateDisplay.First().display);

                    foreach (Assignment assignment in invalid)
                    {
						//Log.LogOutput($"Cannot enable splitscreen: {assignment.error}", Log.LogLevel.Message); // 4.0.0 rewrite 9-12-24
					}

					if (pluginError != ErrorType.NONE)
                    {
                        resolver.token = $"XSS_ERROR_{pluginError}";
                        resolver.isResolved = false;
                    }
                }
            }
            else
            {
                UserManager.AssignSplitscreenUsers(null, out invalid, out pluginError);
            }
        }
        public static void SwapAssignments(Assignment first, Assignment second) // fix swapping between displays
        {
            if (first == null || second == null)
                return;

            graph.SwapAssignments(first, second);
            SaveGraph();
            RequestDisplay(graph.currentDisplay);
            graph.PrintGraph();
        }
        public static void ResetAllAssignments()
        {
            foreach (Assignment assignment in AssignmentManager.assignments)
                assignment.ResetAssignment();

            AssignmentManager.Save(true);

            OnAssignmentsChanged(0);
        }
        public static void AssignUser(Assignment assignment, int2 position)
        {
            if (assignment != null && graph.nodeGraph.IsPositionValid(position))
            {
                graph.PrintGraph();

                if (assignment.controller == null)
                    assignment.controller = GetFirstAvailableController();

                graph.AssignUser(assignment, position);

                SaveGraph();
                RequestDisplay(graph.currentDisplay);
                graph.PrintGraph();
            }
        }
        public static void UnassignUser(Assignment assignment)
        {
            if (assignment.position.IsPositive())
            {
                graph.UnassignUser(assignment);
                SaveGraph();
                RequestDisplay(graph.currentDisplay); // test removal
                graph.PrintGraph();
            }
        }
        public static void RequestDisplay(int display)
        {
            OnAssignmentsChanged(display);
        }
        public static void SaveGraph()
        {
            AssignmentManager.Save(true);
        }
        /// <summary>
        /// Returns null if none are available
        /// </summary>
        /// <returns></returns>
        private static Controller GetFirstAvailableController()
        {
            var sequence = ReInput.players.GetPlayer(0).controllers.Controllers.Where(x => x.type != ControllerType.Mouse && AssignmentManager.assignments.Where(y => y.controller == x).Count() == 0);

            return sequence.Count() == 0 ? null : sequence.First();
        }
        #endregion
    }
}
