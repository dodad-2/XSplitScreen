using DoDad.XLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoDad.XSplitScreen.Assignments
{
    internal class AssignmentSave : IMono
    {
        internal bool shouldSave = false;

        public AssignmentSave()
        {
            XLibrary.Plugin.RegisterIMono(this);
        }
        public void FixedUpdate() { }

        public void LateUpdate()
        {
            if (shouldSave)
            {
                AssignmentManager.PushToConfig();
                shouldSave = false;
            }
        }

        public void Update() { }
    }
}
