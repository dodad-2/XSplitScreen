using System;
using System.Collections.Generic;
using System.Text;
using DoDad.XLibrary.Toolbox;

namespace DoDad.XSplitScreen.Assignments
{
    class AssignmentGraph
    {
        public static readonly int2 graphDimensions = new int2(3, 3);
        internal NodeGraph<Assignment> nodeGraph { get; private set; }

        internal int currentDisplay;

        #region Event Handlers
        public void OnAssignmentsChanged()
        {
            LoadAssignmentsForDisplay();
        }
        #endregion

        #region Graph Interaction
        public void SwapAssignments(Assignment assignment1, Assignment assignment2)
        {
            if (nodeGraph == null || assignment1 == null || assignment2 == null)
                return;

            var display1 = assignment1.display;
            var display2 = assignment2.display;
            var position1 = assignment1.position;
            var position2 = assignment2.position;
            var controller1 = assignment1.controller;
            var controller2 = assignment2.controller;

            //if (assignment1.display == currentDisplay)
            //    nodeGraph.graph[assignment1.position.x][assignment1.position.y].SetData(null);

            //if (assignment2.display == currentDisplay)
            //    nodeGraph.graph[assignment2.position.x][assignment2.position.y].SetData(null);

            assignment1.position = position2;
            assignment1.display = display2;
            assignment2.position = position1;
            assignment2.display = display1;
            //assignment1.controller = controller2; // disable controller swapping
            //assignment2.controller = controller1; // disable controller swapping

            // Controller switching in the case that an unassigned profile has a controller
            /* // perhaps controllers shouldn't switch?
            bool a1IsAssigned = assignment1.position.IsPositive();
            bool a2IsAssigned = assignment2.position.IsPositive();

            if (a1IsAssigned && !a2IsAssigned &&
                assignment1.controller == null && assignment2.controller != null)
            {
                assignment1.controller = assignment2.controller;
                assignment2.controller = null;
            }

            if (a2IsAssigned && !a1IsAssigned &&
                assignment2.controller == null && assignment1.controller != null)
            {
                assignment2.controller = assignment1.controller;
                assignment1.controller = null;
            }

            if (!a1IsAssigned)
                assignment1.controller = null;

            if (!a2IsAssigned)
                assignment2.controller = null;
            */
            LoadAssignmentsForDisplay(currentDisplay);
        }
        public List<Assignment> GetAssignments()
        {
            List<Assignment> assignments = new List<Assignment>();

            for (int x = 0; x < nodeGraph.dimensions.x; x++)
            {
                for (int y = 0; y < nodeGraph.dimensions.y; y++)
                {
                    if (nodeGraph.graph[x][y].data != null)
                    {
                        assignments.Add(nodeGraph.graph[x][y].data);
                    }
                }
            }

            return assignments;
        }
        public void AssignUser(Assignment assignment, int2 position)
        {
            if (nodeGraph == null || !nodeGraph.IsPositionValid(position) || assignment.profile == null || assignment.profile.Length == 0)
                return;

            var node = nodeGraph.graph[position.x][position.y];

            ShiftType shiftType = node.shiftType == ShiftType.None ? ShiftType.Expand : node.shiftType;

            LoadAssignmentProperties(true);

            nodeGraph.ShiftGraphData(position, shiftType);

            node.SetData(assignment);

            LoadAssignmentProperties();
        }
        public void UnassignUser(Assignment assignment)
        {
            if (!nodeGraph.IsPositionValid(assignment.position) || assignment.profile == null || assignment.profile.Length == 0)
                return;

            var node = nodeGraph.graph[assignment.position.x][assignment.position.y];
            bool doShift = true;

            ShiftType shiftType = node.shiftType == ShiftType.None ? ShiftType.Contract : node.shiftType;

            switch (node.shiftType)
            {
                case ShiftType.Left:
                    shiftType = ShiftType.Right;
                    break;
                case ShiftType.Right:
                    shiftType = ShiftType.Left;
                    break;
                case ShiftType.Up:
                    shiftType = ShiftType.Down;
                    break;
                case ShiftType.Down:
                    shiftType = ShiftType.Up;
                    break;
            }

            if (node.nodeType == NodeType.None && shiftType == ShiftType.Contract)
            {
                bool[] validNeighbor = new bool[2]
                {
                    node.neighborLeft?.neighborLeft != null && node.neighborLeft.neighborLeft.HasData(),
                    node.neighborRight?.neighborRight != null && node.neighborRight.neighborRight.HasData()
                };

                if (XLibrary.Toolbox.Math.Truth(validNeighbor) == 1)
                {
                    if (validNeighbor[0])
                        node.PullData(ShiftType.Left, false);
                    else
                        node.PullData(ShiftType.Right, false);

                    doShift = false;
                }
            }

            if (doShift)
            {
                node.SetData(null);
                nodeGraph.ShiftGraphData(assignment.position, shiftType);
            }

            assignment.ResetAssignment();

            LoadAssignmentProperties();
        }
        public void LoadAssignmentsForDisplay(int display = 0)
        {
            if (AssignmentManager.assignments == null)
                return;

            if (nodeGraph == null)
            {
                nodeGraph = new NodeGraph<Assignment>(graphDimensions);

                KeyValuePair<int2, ShiftType>[] mainScreens = new KeyValuePair<int2, ShiftType>[4];

                mainScreens[0] = new KeyValuePair<int2, ShiftType>(new int2(0, 1), ShiftType.Right);
                mainScreens[1] = new KeyValuePair<int2, ShiftType>(new int2(1, 0), ShiftType.Up);
                mainScreens[2] = new KeyValuePair<int2, ShiftType>(new int2(1, 2), ShiftType.Down);
                mainScreens[3] = new KeyValuePair<int2, ShiftType>(new int2(2, 1), ShiftType.Left);

                for (int e = 0; e < mainScreens.Length; e++)
                    nodeGraph.SetNodeConfig(mainScreens[e].Key, NodeType.Secondary, mainScreens[e].Value);

                nodeGraph.SetNodeConfig(new int2(1, 1), NodeType.Primary, ShiftType.Expand);
            }

            nodeGraph.ClearGraphData();

            currentDisplay = display;

            foreach (var item in AssignmentManager.assignments)
            {
                if (item.position.IsPositive() && item.display.Equals(display))
                {
                    if (!nodeGraph.TrySetNodeData(item, item.position))
                        Log.LogOutput($"LoadAssignmentForDisplay: Unable to assign '{item.profile}' to '{item.position.ToString()}'");
                    else
                        Log.LogOutput($"LoadAssignmentForDisplay: Found assignment for '{item.profile}'");
                }
            }
        }
        #endregion

        #region Helpers
        private void LoadAssignmentProperties(bool reset = false)
        {
            int2 position = int2.zero;

            for (int x = 0; x < nodeGraph.dimensions.x; x++)
            {
                for (int y = 0; y < nodeGraph.dimensions.y; y++)
                {
                    if (nodeGraph.graph[x][y].HasData())
                    {
                        position.x = x;
                        position.y = y;

                        nodeGraph.graph[x][y].data.position = reset ? int2.negative : position;
                        nodeGraph.graph[x][y].data.display = reset ? -1 : currentDisplay;
                    }
                }
            }
        }
        public void PrintGraph()
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder subBuilder = new StringBuilder();
            string subDivider = " | ";

            builder.AppendLine(" ");
            builder.AppendLine($"------ Display {currentDisplay} ------");

            for (int y = 2; y > -1; y--)
            {
                subBuilder.Clear();

                for (int x = 0; x < 3; x++)
                {
                    subBuilder.Append($"[{nodeGraph.graph[x][y].position.x}, {nodeGraph.graph[x][y].position.y}] {(nodeGraph.graph[x][y].data == null ? "x" : nodeGraph.graph[x][y].data.localId)}{(nodeGraph.graph[x][y].data?.controller?.type == Rewired.ControllerType.Keyboard ? "k" : "")} {(x == 2 ? "" : subDivider)}");
                }

                builder.AppendLine(subBuilder.ToString());
            }

            builder.AppendLine($"------ Display {currentDisplay} ------");

            Log.LogOutput(builder);
        }
        #endregion
    }
}
