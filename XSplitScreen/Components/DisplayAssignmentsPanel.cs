using DoDad.XLibrary.Toolbox;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DoDad.XSplitScreen.Assignments;
using Rewired;
using DoDad.XLibrary.Components;

namespace DoDad.XSplitScreen.Components
{
    class DisplayAssignmentsPanel : MonoBehaviour
    {
        #region Variables
        //public static readonly int2 displaySize = new int2(855, 855);
        //public static readonly int2 panelSize = new int2((int)(displaySize.x * 0.45f), (int)(displaySize.y * 0.45f));//new int2((int)(displaySize.x * 0.578), (int)(displaySize.y * 0.578));//new int2(350, 350);
        public static float2 containerDelta { get; private set; }
        private Transform container;

        public Image display;
        public Image center;
        public Image[] dividers;

        public UserPanel[][] panels;

        private Action resetPanels;
        private Action onCheckAddButtons;
        #endregion

        #region Unity Methods
        void Awake()
        {
        }
        #endregion

        #region Initialization
        public void Initialize()
        {
            CreateDisplay();
            CreatePanelGrid(AssignmentGraph.graphDimensions);
        }
        private void CreateDisplay()
        {
            container = new GameObject("Display Assignments Panel Container", typeof(RectTransform), typeof(LayoutElement)).transform;
            container.GetComponent<LayoutElement>().preferredHeight = 800;//containerDelta.y;//displaySize.y;
            container.GetComponent<LayoutElement>().flexibleHeight = 3;
            container.SetParent(transform);

            display = Instantiate(XLibrary.Resources.GetPrefabUI("SimpleImage")).GetComponent<Image>();
            display.name = "(Image) Background";
            display.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            display.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            display.transform.SetParent(container);
            display.sprite = Instantiate(XLibrary.Resources.GetSprite("display"));
            display.transform.localPosition = Vector3.zero;
            display.transform.localScale = Vector3.one;
            display.SetNativeSize();
            display.raycastTarget = false;
            display.gameObject.SetActive(true);
            AssignmentScreen.lemonizer.Lemonize(display);

            containerDelta = float2.Create(display.GetComponent<RectTransform>().sizeDelta);

            //display.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, displaySize.x);//800);
            //display.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, displaySize.y);//800);

            center = Instantiate(XLibrary.Resources.GetPrefabUI("SimpleImage")).GetComponent<Image>();
            center.name = "(Image) Center";
            center.transform.SetParent(container);
            center.sprite = Instantiate(XLibrary.Resources.GetSprite("display_center"));
            center.transform.localPosition = Vector3.zero;
            center.transform.localScale = Vector3.one;
            center.SetNativeSize();
            center.raycastTarget = false;
            center.gameObject.SetActive(true);

            // Layout Grid

            //display.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            //center.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            // 

            dividers = new Image[4];

            for (int e = 0; e < 4; e++)
            {
                dividers[e] = Instantiate(XLibrary.Resources.GetPrefabUI("SimpleImage")).GetComponent<Image>();
                dividers[e].name = $"(Image) Divider {e}";
                //dividers[e].gameObject.AddComponent<LayoutElement>().ignoreLayout = true; // Layout Grid
                dividers[e].transform.SetParent(container);
                dividers[e].transform.localPosition = Vector3.zero;
                dividers[e].transform.localScale = new Vector3(0.5f, 0.98f, 1);
                dividers[e].transform.localRotation = Quaternion.AngleAxis(e * -90f, Vector3.forward);
                dividers[e].sprite = Instantiate(XLibrary.Resources.GetSprite("divider"));
                dividers[e].raycastTarget = false;
                dividers[e].SetNativeSize();
                dividers[e].gameObject.SetActive(true);
                dividers[e].color = new Color(1, 1, 1, 0.2f);
                dividers[e].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerDelta.x);
                dividers[e].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerDelta.y);
                AssignmentScreen.lemonizer.Lemonize(dividers[e]);
            }
        }
        #endregion

        #region Public Methods
        public void CreatePanelGrid(int2 gridDimensions)
        {
            if (panels == null || panels.Length == 0)
            {
                panels = new UserPanel[gridDimensions.x][];

                for (int e = 0; e < gridDimensions.x; e++)
                {
                    panels[e] = new UserPanel[gridDimensions.y];
                }
            }

            GameObject newPanel;

            int2 index = int2.zero;

            float2 availableSize = containerDelta.Product(0.75f);//.Product(0.9f);

            float2 columnWidth = availableSize.Divide(gridDimensions);//displaySize.ToFloat2().Divide(gridDimensions);

            float2 origin = float2.zero.Subtract(availableSize.Divide(2));

            for (int x = 0; x < gridDimensions.x; x++)
            {
                index.x = x;

                for (int y = 0; y < gridDimensions.y; y++)
                {
                    index.y = y;

                    newPanel = new GameObject($"Panel ({index})", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(UserPanel), typeof(ErrorResolver));
                    newPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                    newPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                    newPanel.GetComponent<LayoutElement>().ignoreLayout = true;
                    newPanel.transform.SetParent(container);

                    var panelSizeDelta = containerDelta.Product(0.48f);

                    //newPanel.GetComponent<LayoutElement>().minWidth = panelSize.x;
                    //newPanel.GetComponent<LayoutElement>().minHeight = panelSize.y;
                    newPanel.GetComponent<Image>().sprite = Instantiate(DoDad.XLibrary.Resources.GetSprite("display_screen"));
                    newPanel.GetComponent<Image>().SetNativeSize();
                    newPanel.GetComponent<Image>().raycastTarget = false;
                    AssignmentScreen.lemonizer.Lemonize(newPanel.GetComponent<Image>());

                    newPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSizeDelta.x);//display.GetComponent<RectTransform>().sizeDelta.x);
                    newPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelSizeDelta.y); //display.GetComponent<RectTransform>().sizeDelta.y);

                    newPanel.transform.localPosition = origin.Add(columnWidth.Product(index.Add(int2.one))).Subtract(columnWidth.Divide(2)).ToVector2();//.Subtract(columnWidth.Divide(new float2(2, 2))).ToVector2();//.Product(new float2(1, 1)).ToVector2();
                    newPanel.GetComponent<UserPanel>().Initialize(panelSizeDelta.x);
                    /* // OLD ADDBUTTON CODE
                    var addPosition = Vector3.zero;

                    float dist = 10;

                    float xAdd = x < 1 ? -dist : dist;
                    float yAdd = y < 1 ? -dist : dist;

                    if (y != 1)
                        addPosition += new Vector3(0, yAdd, 0);

                    if (x != 1)
                        addPosition += new Vector3(xAdd, 0, 0);

                    newPanel.GetComponent<UserPanel>().addButton.transform.localPosition = addPosition;
                    */

                    panels[x][y] = newPanel.GetComponent<UserPanel>();
                    panels[x][y].position = new int2(x, y);

                    onCheckAddButtons += panels[x][y].OnCheckAddButtons;

                    // onEnableAddButtons += panels[x][y].SetAddButtonEnabled; // OLD ADDBUTTON CODE

                    resetPanels += panels[x][y].ResetPanel;
                }
            }
        }
        public UserPanel GetPanelByAssignment(Assignment assignment)
        {
            for (int x = 0; x < panels.Length; x++)
            {
                for (int y = 0; y < panels.Length; y++)
                {
                    if (panels[x][y].current == null)
                        continue;

                    if (panels[x][y].current.Equals(assignment))
                        return panels[x][y];
                }
            }

            return null;
        }
        public UserPanel GetNearestPanel(Vector3 position)
        {
            UserPanel panel = null;

            float shortestDistance = float.MaxValue;
            float maxDistance = panels[1][1].GetComponent<RectTransform>().sizeDelta.x / 2f;

            for (int x = 0; x < panels.Length; x++)
            {
                for (int y = 0; y < panels.Length; y++)
                {
                    if (panels[x][y].current == null)
                        continue;

                    float currentDistance = (panels[x][y].iconTarget.position - position).sqrMagnitude / 1000f;

                    if (currentDistance < shortestDistance)
                    {
                        panel = panels[x][y];
                        shortestDistance = currentDistance;
                    }
                }
            }

            if (shortestDistance <= 10 && shortestDistance <= maxDistance)
            {
                return panel;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Helpers
        private void ClearPanels()
        {
            resetPanels.Invoke();
        }
        public void LoadFromGraph()
        {
            var graph = AssignmentWindow.graph.nodeGraph;

            ClearPanels();

            //onEnableAddButtons.Invoke(false); // old addbutton

            foreach (var divider in dividers)
                divider.enabled = false;

            center.enabled = true;

            var canAddUsers = AssignmentManager.canAddUsers;

            if (AssignmentWindow.graph.GetAssignments().Count == 0)
            {
                center.enabled = false;

                //if (canAddUsers) // old addbutton
                //    panels[1][1].SetAddButtonEnabled(true);
            }
            else
            {
                foreach (var assignment in AssignmentWindow.graph.GetAssignments())
                {
                    panels[assignment.position.x][assignment.position.y].AssignProfile(assignment, false);

                    var node = graph.graph[assignment.position.x][assignment.position.y];

                    if (node.nodeType != NodeType.None) // Ignore outer corners
                    {
                        /* // old addbutton code
                        if (node.neighborUp != null && graph.GetNode(node.neighborUp.position) != null && node.neighborUp.nodeType != NodeType.Primary)
                            panels[node.neighborUp.position.x][node.neighborUp.position.y].SetAddButtonEnabled(canAddUsers);

                        if (node.neighborDown != null && graph.GetNode(node.neighborDown.position) != null && node.neighborDown.nodeType != NodeType.Primary)
                            panels[node.neighborDown.position.x][node.neighborDown.position.y].SetAddButtonEnabled(canAddUsers);

                        if (node.neighborLeft != null && graph.GetNode(node.neighborLeft.position) != null && node.neighborLeft.nodeType != NodeType.Primary)
                            panels[node.neighborLeft.position.x][node.neighborLeft.position.y].SetAddButtonEnabled(canAddUsers);

                        if (node.neighborRight != null && graph.GetNode(node.neighborRight.position) != null && node.neighborRight.nodeType != NodeType.Primary)
                            panels[node.neighborRight.position.x][node.neighborRight.position.y].SetAddButtonEnabled(canAddUsers);
                        */
                        if (node.nodeType != NodeType.Primary)
                        {
                            if (node.neighborUp == null || node.neighborDown == null)
                            {
                                dividers[1].enabled = true;
                                dividers[3].enabled = true;
                            }
                            else
                            {
                                dividers[0].enabled = true;
                                dividers[2].enabled = true;
                            }
                        }
                        else
                        {
                            center.enabled = false;
                        }
                    }
                    else
                    {
                        if (node.neighborUp == null)
                        {
                            dividers[0].enabled = true;
                        }
                        else
                        {
                            dividers[2].enabled = true;
                        }

                        if (node.neighborLeft == null)
                        {
                            dividers[3].enabled = true;
                        }
                        else
                        {
                            dividers[1].enabled = true;
                        }
                    }
                }
            }

            onCheckAddButtons?.Invoke();
        }
        #endregion
    }
}
