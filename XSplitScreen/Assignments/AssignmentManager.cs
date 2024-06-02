using BepInEx.Configuration;
using DoDad.XLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using RoR2;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using Rewired;

namespace DoDad.XSplitScreen.Assignments
{
    public class AssignmentManager : IConfigurableContent
    {
        #region Variables
        public const int maxPlayers = 4;

        internal const int saveInterval = 1;

        public static event Action onAssignmentsChanged;

        //public static List<Assignment> assignments { get; private set; }
        //public static List<Assignment> assignments => internalList;
        public static ReadOnlyCollection<Assignment> assignments => internalList.AsReadOnly();// { get; private set; }

        /// <summary>
        /// Users assigned across all displays
        /// </summary>
        public static int assignedUsers => internalList.Where(x => x.position.IsPositive()).Count();
        /// <summary>
        /// Are there fewer than maxPlayers active?
        /// </summary>
        public static bool canAddUsers => assignedUsers < maxPlayers;
        public static bool firstLaunch = false;

        private static List<Assignment> internalList;

        private const string fileName = "assignments.json";

        private static ConfigFile configFile;

        private static AssignmentSave save = new AssignmentSave();
        #endregion

        #region Initialization
        private void Initialize()
        {
            //ReloadAssignments();
            SaveSystem.onAvailableUserProfilesChanged += OnAvailableUserProfilesChanged;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the number of assignments attached to a display.
        /// </summary>
        /// <param name="display"></param>
        /// <returns></returns>
        public static int AssignedToDisplay(int display)
        {
            return internalList.Where(x => x.position.IsPositive() && x.display.Equals(display)).Count();
        }
        /// <summary>
        /// Returns null if none exists.
        /// </summary>
        /// <returns></returns>
        public static Assignment GetFirstLocalAssignment()
        {
            if (internalList == null || internalList.Count() == 0)
                return null;

            return internalList.Where(x => x.profile.Equals(LocalUserManager.GetFirstLocalUser().userProfile.fileName)).First();
        }
        /// <summary>
        /// Returns the first unassigned user or null if not found.
        /// </summary>
        /// <returns></returns>
        public static Assignment GetFreeAssignment()
        {
            var assignments = internalList.Where(x => !x.position.IsPositive());

            if (assignments.Count() == 0)
                return null;

            return assignments.First();
        }
        /// <summary>
        /// Return null if not found.
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        public static Assignment GetAssignmentByController(Controller controller)
        {
            var items = internalList.Where(x => x.controller != null && x.controller.Equals(controller));

            if (items == null)
                return null;

            return items.Count() > 0 ? items.First() : null;
        }
        public static Assignment GetAssignmentByFilename(string fileName)
        {
            var items = internalList.Where(x => x.profile.Equals(fileName));

            if (items == null)
                return null;

            return items.Count() > 0 ? items.First() : null;
        }
        #endregion

        #region IConfigurableContent
        public void ConfigureContent(ConfigFile configFile)
        {
            AssignmentManager.configFile = configFile;

            Initialize();
        }

        public bool IsContentEnabled()
        {
            return true;
        }
        #endregion

        #region Load / Save
        /// <summary>
        /// If not immediate the save occurs during LateUpdate
        /// </summary>
        internal static void Save(bool immediate = false)
        {
            if (immediate)
                PushToConfig();
            else
                save.shouldSave = true;
        }
        internal static void PushToConfig()
        {
            if (internalList == null || internalList.Count == 0)
                return;

            string filePath = $"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\{fileName}";

            File.WriteAllText(filePath, JsonConvert.SerializeObject(internalList, Formatting.Indented));

            Log.LogOutput($"Assignments saved to file", Log.LogLevel.Message);
        }
        private static void ReloadAssignments()
        {
            try
            {
                if (internalList == null)
                    internalList = new List<Assignment>();

                string filePath = $"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\{fileName}";

                if (!File.Exists(filePath) || Plugin.clearAssignmentsOnStart)
                {
                    firstLaunch = true;
                    return;
                }

                internalList.Clear();

                internalList = JsonConvert.DeserializeObject<List<Assignment>>(File.ReadAllText(filePath));

                foreach (var assignment in internalList)
                    FixAssignments(assignment);

                onAssignmentsChanged?.Invoke();
            }
            catch (Exception e)
            {
                Log.LogOutput($"Unable to load assignments: {e}", Log.LogLevel.Error);
            }
        }
        private static void EnsureAssignmentsExist()
        {
            if (internalList == null)
                ReloadAssignments();

            int colorIndex = 0;

            foreach (KeyValuePair<string, UserProfile> keyPair in PlatformSystems.saveSystem.loadedUserProfiles)
            {
                if (internalList.Where(x => x.profile.Equals(keyPair.Key)).Count() > 0)
                    continue;

                Log.LogOutput($"Creating assignment for '{keyPair.Key}'");
                internalList.Add(CreateAssignment(keyPair.Key));

                internalList[internalList.Count - 1].color = ColorCatalog.GetMultiplayerColor(colorIndex);

                colorIndex++;

                if (colorIndex == ColorCatalog.multiplayerColors.Length)
                    colorIndex = 0;
            }

            foreach (Assignment assignment in internalList)
            {
                if (PlatformSystems.saveSystem.loadedUserProfiles.Where(x => x.Key.Equals(assignment.profile)).Count() == 0)
                    assignment.profile = null;
            }

            internalList.RemoveAll(x => x.profile == null);

            PushToConfig();
            ReloadAssignments();
        }
        private static Assignment CreateAssignment(string profile)
        {
            if (profile == null)
                return null;

            Color color = ColorCatalog.GetMultiplayerColor(UnityEngine.Random.Range(0, ColorCatalog.multiplayerColors.Length));

            var assignment = new Assignment()
            {
                profile = profile,
                position = XLibrary.Toolbox.int2.negative,
                color = color,
                display = -1,
                localId = -1,
                hudScale = 100,
            };

            return assignment;
        }
        #endregion

        #region Compatibility
        private static void FixAssignments(Assignment assignment)
        {
            if (assignment.hudScale < 1 || assignment.hudScale > 300)
                assignment.hudScale = 100;
        }
        #endregion

        #region Event Handlers
        private static void OnAvailableUserProfilesChanged()
        {
            EnsureAssignmentsExist();
        }
        #endregion
    }
}
