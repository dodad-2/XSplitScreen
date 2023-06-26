using DoDad.XLibrary.Toolbox;
using Newtonsoft.Json;
using Rewired;
using RoR2;
using System;
using UnityEngine;

namespace DoDad.XSplitScreen.Assignments
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class Assignment
    {
        #region Get / Set
        /// <summary>
        /// The NodeGraph position
        /// </summary>
        public int2 position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;

                onUpdatePosition?.Invoke(this);
            }
        }
        /// <summary>
        /// The profile's file name
        /// </summary>
        public string profile
        {
            get
            {
                return _profile;
            }
            set
            {
                _profile = value;

                onUpdateProfile?.Invoke(this);
            }
        }
        public Controller controller
        {
            get
            {
                return _controller;
            }
            set
            {
                _controller = value;

                onUpdateController?.Invoke(this);
            }
        }
        public Color color
        {
            get
            {
                return new Color(_r, _g, _b, _a);
            }
            set
            {
                _r = value.r;
                _g = value.g;
                _b = value.b;
                _a = value.a;

                onUpdateColor?.Invoke(this);
            }
        }
        public int localId
        {
            get
            {
                return _localId;
            }
            set
            {
                _localId = value;

                onUpdateLocalId?.Invoke(this);
            }
        }
        public int display
        {
            get
            {
                return _display;
            }
            set
            {
                _display = value;

                onUpdateDisplay?.Invoke(this);
            }
        }
        public int hudScale
        {
            get
            {
                return _hudScale;
            }
            set
            {
                _hudScale = value;

                onUpdateHudScale?.Invoke(this);
            }
        }
        #endregion

        #region Variables
        public Action<Assignment> onUpdatePosition;
        public Action<Assignment> onUpdateDisplay;
        public Action<Assignment> onUpdateController;
        public Action<Assignment> onUpdateColor;
        public Action<Assignment> onUpdateProfile;
        public Action<Assignment> onUpdateLocalId;
        public Action<Assignment> onUpdateHudScale;

        public ErrorType error = ErrorType.NONE;

        public Rect cameraRect;

        private Controller _controller;
        #endregion

        #region JSON Properties
        [JsonProperty]
        private string _profile;
        [JsonProperty]
        private int2 _position;
        [JsonProperty]
        private float _r;
        [JsonProperty]
        private float _g;
        [JsonProperty]
        private float _b;
        [JsonProperty]
        private float _a;
        [JsonProperty]
        private int _display;
        /// <summary>
        /// The local index used for LocalUserManager
        /// </summary>
        [JsonProperty]
        private int _localId;
        [JsonProperty]
        private int _hudScale;
        #endregion

        #region Data
        public void LoadValuesFrom(Assignment assignment)
        {
            this.profile = assignment.profile;
            this.position = assignment.position;
            this.display = assignment.display;
            this.color = assignment.color;
            this.controller = assignment.controller;
            this.localId = assignment.localId;
            this.hudScale = assignment.hudScale;

        }
        public void ResetAssignment(bool loseController = true)
        {
            this.position = int2.negative;
            this.display = -1;
            this.controller = loseController ? null : this.controller;
            this.localId = -1;
        }
        #endregion

        #region Public Methods
        public Rect GetScreen()
        {
            Rect screenRect = new Rect(0, 0, 1, 1);

            if (!position.IsPositive() || display < 0)
                return screenRect;

            screenRect.x = position.x < 2 ? 0 : 0.5f;
            screenRect.y = position.y < 2 ? 0 : 0.5f;

            screenRect.height = position.y == 1 ? 1 : 0.5f;
            screenRect.width = position.x == 1 ? 1 : 0.5f;

            return screenRect;
        }
        #endregion
    }
    public enum ErrorType
    {
        NONE,
        OTHER,
        PROFILE,
        CONTROLLER,
        POSITION,
        DISPLAY,
        MINIMUM,
        PROFILE_2,
    }
}