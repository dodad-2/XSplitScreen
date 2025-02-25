using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class XButton : Button
    {
        public Action<XButton, BaseInputModule> onPointerDown;
        public Action<XButton, BaseInputModule> onPointerUp;
        public Action<XButton, BaseInputModule> onPointerExit;
        public Action<XButton, BaseInputModule> onSubmit;

        private bool log = false;
        private bool clicked = false;
        private int frameCount = 0;

        #region Unity Methods
        public override void Awake()
        {
            if (gameObject.GetComponent<MultiInputHelper>() == null)
                gameObject.AddComponent<MultiInputHelper>().xButton = this;
        }
        void LateUpdate()
        {
            if (!interactable)
            {
                frameCount = 0;
                clicked = false;
            }

            if (!clicked)
                return;

            frameCount++;

            if (frameCount > 1)
            {
                frameCount = 0;
                clicked = false;
            }
        }
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable)
                return;

            base.OnPointerClick(eventData);

			/*if (log) // 4.0.0 rewrite 9-12-24
                Log.LogOutput($"'{name}' OnPointerClick");*/
		}
		public override void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable)
                return;

            base.OnPointerDown(eventData);

            onPointerDown?.Invoke(this, eventData.currentInputModule);

			/*if (log) // 4.0.0 rewrite 9-12-24
                Log.LogOutput($"'{name}' OnPointerDown");*/
		}
		public override void OnPointerUp(PointerEventData eventData)
        {
            if (!interactable)
                return;

            base.OnPointerUp(eventData);

            onPointerUp?.Invoke(this, eventData.currentInputModule);

			/*if (log) // 4.0.0 rewrite 9-12-24
                Log.LogOutput($"'{name}' OnPointerUp");*/
		}
		public override void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactable)
                return;

            base.OnPointerEnter(eventData);

            eventData.currentInputModule.eventSystem.SetSelectedGameObject(gameObject, (BaseEventData)eventData);

			/*if (log) // 4.0.0 rewrite 9-12-24
                Log.LogOutput($"'{name}' OnPointerEnter");*/
		}
		public override void OnPointerExit(PointerEventData eventData)
        {
            if (!interactable)
                return;

            base.OnPointerExit(eventData);

            onPointerExit?.Invoke(this, eventData.currentInputModule);

            eventData.currentInputModule.eventSystem.SetSelectedGameObject(null, (BaseEventData)eventData);

			/*if (log) // 4.0.0 rewrite 9-12-24
                Log.LogOutput($"'{name}' OnPointerExit");*/
		}
		public override void OnSubmit(BaseEventData eventData)
        {
            if (!interactable)
                return;

            if (clicked)
                return;

            clicked = true;

            base.OnSubmit(eventData);

            onSubmit?.Invoke(this, eventData.currentInputModule);

			/*if (log) // 4.0.0 rewrite 9-12-24
                Log.LogOutput($"'{name}' OnSubmit");*/
		}
		#endregion

		#region Event Handlers

		#endregion
	}
}
