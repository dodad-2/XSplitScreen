using DoDad.XLibrary.Toolbox;
using DoDad.XSplitScreen.Assignments;
using Rewired;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DoDad.XSplitScreen
{
    public class LocalSplitscreenUser : LocalUser
    {
        public Assignment assignment;
        public GameObject pauseScreenInstance;

        public void AssignController(bool removeFromOtherUsers = false)
        {
            inputPlayer.controllers.AddController(assignment.controller, removeFromOtherUsers);
        }
    }
}
