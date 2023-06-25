using DoDad.XLibrary.Interfaces;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoDad.XSplitScreen
{
    public static class Input
    {
        /// <summary>
        /// The last event system to receive input. Cleared in LateUpdate.
        /// </summary>
        public static MPEventSystem activeEventSystem { get; private set; }
        /// <summary>
        /// The last event system to receive input.
        /// </summary>
        public static MPEventSystem lastEventSystem { get; private set; }
        private static UpdateInput update;

        internal static void UpdateEventSystem(MPEventSystem eventSystem)
        {
            if (update == null)
                update = new UpdateInput();

            if (eventSystem == null)
                return;

            activeEventSystem?.SetSelectedGameObject(null);

            lastEventSystem = eventSystem;
            activeEventSystem = eventSystem;
        }

        private class UpdateInput : IMono
        {
            internal UpdateInput()
            {
                XLibrary.Plugin.RegisterIMono(this);

                SetLastEventSystemDefault();
                activeEventSystem = MPEventSystem.current as MPEventSystem;
            }
            public void FixedUpdate() { }
            public void Update() { }
            public void LateUpdate()
            {
                activeEventSystem = null;

                if (lastEventSystem == null)
                    SetLastEventSystemDefault();
            }

            private void SetLastEventSystemDefault()
            {
                lastEventSystem = MPEventSystem.current as MPEventSystem;
            }
        }
    }
}
