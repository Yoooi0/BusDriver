﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BusDriver.UI
{
    public interface IUIBuilder
    {
        JSONStorableStringChooser CreatePopup(string paramName, string label, List<string> values, string startingValue, JSONStorableStringChooser.SetStringCallback callback, bool scrollable, bool rightSide = false);
        JSONStorableStringChooser CreatePopup(string paramName, string label, List<string> values, string startingValue, JSONStorableStringChooser.SetStringCallback callback, bool rightSide = false);
        JSONStorableStringChooser CreateScrollablePopup(string paramName, string label, List<string> values, string startingValue, JSONStorableStringChooser.SetStringCallback callback, bool rightSide = false);
        UIDynamicButton CreateButton(string label, UnityAction callback, bool rightSide = false);
        UIDynamicButton CreateButton(string label, UnityAction callback, Color buttonColor, Color textColor, bool rightSide = false);
        UIDynamicButton CreateDisabledButton(string label, Color buttonColor, Color textColor, bool rightSide = false);
        JSONStorableFloat CreateSlider(string paramName, string label, float startingValue, float minValue, float maxValue, JSONStorableFloat.SetFloatCallback callback, bool constrain, bool interactable, bool rightSide = false, string valueFormat = "F2");
        JSONStorableFloat CreateSlider(string paramName, string label, float startingValue, float minValue, float maxValue, bool constrain, bool interactable, bool rightSide = false, string valueFormat = "F2");
        JSONStorableString CreateTextField(string paramName, string startingValue, float height, JSONStorableString.SetStringCallback callback, bool rightSide = false);
        JSONStorableString CreateTextField(string paramName, string startingValue, float height, bool rightSide = false);
        UITextInput CreateTextInput(string paramName, string label, string startingValue, float height, bool rightSide = false);
        JSONStorableBool CreateToggle(string paramName, string label, bool startingValue, JSONStorableBool.SetBoolCallback callback, bool rightSide = false);
        JSONStorableBool CreateToggle(string paramName, string label, bool startingValue, bool rightSide = false);
        UIDynamic CreateSpacer(float height, bool rightSide = false);
        UIHorizontalGroup CreateHorizontalGroup(float width, float height, Vector2 spacing, int count, Func<int, Transform> itemCreator, bool rightSide = false);

        Transform CreateButtonEx();
        Transform CreateSliderEx();
        Transform CreateToggleEx();
        Transform CreatePopupEx();

        void Destroy(object o);
    }
}
