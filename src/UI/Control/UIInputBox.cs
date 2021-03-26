using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BusDriver.UI
{
    public class UIInputBox
    {
        public readonly UIDynamic container;
        public readonly GameObject gameObject;

        public UIInputBox(UIDynamic container, float width, float height)
        {
            this.container = container;

            gameObject = new GameObject();
            gameObject.transform.SetParent(container.gameObject.transform, false);

            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, 0);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            var inputField = gameObject.AddComponent<InputField>();
        }
    }
}
