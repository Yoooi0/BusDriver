using SimpleJSON;
using System.Collections.Generic;
using BusDriver.Config;
using BusDriver.MotionTarget;
using BusDriver.ValuesSource;
using BusDriver.UI;
using UnityEngine;

namespace BusDriver
{
    public partial class Plugin : MVRScript, IConfigProvider
    {
        private UIBuilder _builder;
        private UIGroup _group, _valuesSourceGroup, _motionTargetGroup;

        private UIDynamicButton PluginTitle;
        private UIHorizontalGroup PresetButtonGroup;
        private JSONStorableBool DebugDrawEnableToggle;
        private JSONStorableString ValuesSourceReportText;

        private UIDynamicButton RangeTitle;
        private JSONStorableStringChooser L0DirectionChooser;
        private JSONStorableFloat L0RangeSlider, L2RangeSlider, L1RangeSlider;
        private JSONStorableFloat R0RangeSlider, R2RangeSlider, R1RangeSlider;

        private JSONStorableStringChooser ValuesSourceChooser;
        private UIDynamicButton ValuesSourceTitle;

        private UIDynamicButton OriginTitle;
        private JSONStorableBool AlwaysDrawOriginToggle;
        private JSONStorableBool DrawOriginBoxToggle;
        private JSONStorableBool DrawOriginAnglesToggle;

        private JSONStorableStringChooser MotionTargetChooser;
        private UIDynamicButton MotionTargetTitle;

        public void CreateUI()
        {
            pluginLabelJSON.val = PluginName;

            _builder = new UIBuilder(this);
            _group = new UIGroup(_builder);
            _group.BlacklistStorable("Device Report");

            PluginTitle = _group.CreateDisabledButton("Plugin", new Color(0.3f, 0.3f, 0.3f), Color.white);

            PresetButtonGroup = _group.CreateHorizontalGroup(510, 50, new Vector2(10, 0), 3, idx => _group.CreateButtonEx());
            var saveButton = PresetButtonGroup.items[0].GetComponent<UIDynamicButton>();
            saveButton.buttonText.fontSize = 25;
            saveButton.label = "Save Config";
            saveButton.buttonColor = new Color(0.309f, 1f, 0.039f) * 0.8f;
            saveButton.textColor = Color.white;
            saveButton.button.onClick.AddListener(SaveConfigCallback);

            var loadButton = PresetButtonGroup.items[1].GetComponent<UIDynamicButton>();
            loadButton.buttonText.fontSize = 25;
            loadButton.label = "Load Config";
            loadButton.buttonColor = new Color(1f, 0.168f, 0.039f) * 0.8f;
            loadButton.textColor = Color.white;
            loadButton.button.onClick.AddListener(LoadConfigCallback);

            var defaultButton = PresetButtonGroup.items[2].GetComponent<UIDynamicButton>();
            defaultButton.buttonText.fontSize = 25;
            defaultButton.label = "As Default";
            defaultButton.buttonColor = new Color(1f, 0.870f, 0.039f) * 0.8f;
            defaultButton.textColor = Color.white;
            defaultButton.button.onClick.AddListener(SaveDefaultConfigCallback);

            DebugDrawEnableToggle = _group.CreateToggle("Plugin:DebugDrawEnable", "Enable Debug", false);

            var rangeVisible = false;
            var rangeGroup = new UIGroup(_group);
            RangeTitle = _group.CreateButton("Range", () => rangeGroup.SetVisible(rangeVisible = !rangeVisible), new Color(0.3f, 0.3f, 0.3f), Color.white);

            L0DirectionChooser = rangeGroup.CreateScrollablePopup("Plugin:L0Direction", "L0 Direction", new List<string> { "+Up", "+Right", "+Forward", "-Up", "-Right", "-Forward" }, "+Up", null);
            L0RangeSlider = rangeGroup.CreateSlider("Plugin:L0Range", "L0 Range (+/- cm)", 0.08f, 0.01f, 0.25f, true, true, valueFormat: "P0");
            L1RangeSlider = rangeGroup.CreateSlider("Plugin:L1Range", "L1 Range (+/- cm)", 0.05f, 0.01f, 0.25f, true, true, valueFormat: "P0");
            L2RangeSlider = rangeGroup.CreateSlider("Plugin:L2Range", "L2 Range (+/- cm)", 0.05f, 0.01f, 0.25f, true, true, valueFormat: "P0");

            R0RangeSlider = rangeGroup.CreateSlider("Plugin:R0Range", "R0 Range (+/- deg)", 30, 1, 90, true, true, valueFormat: "F0");
            R1RangeSlider = rangeGroup.CreateSlider("Plugin:R1Range", "R1 Range (+/- deg)", 30, 1, 90, true, true, valueFormat: "F0");
            R2RangeSlider = rangeGroup.CreateSlider("Plugin:R2Range", "R2 Range (+/- deg)", 30, 1, 90, true, true, valueFormat: "F0");
            rangeGroup.SetVisible(false);

            _valuesSourceGroup = new UIGroup(_group);
            var valuesSourceVisible = true;
            ValuesSourceTitle = _group.CreateButton("Values Source", () => _valuesSourceGroup.SetVisible(valuesSourceVisible = !valuesSourceVisible), new Color(0.3f, 0.3f, 0.3f), Color.white);
            ValuesSourceReportText = _valuesSourceGroup.CreateTextField("Values Report", "", 230);
            ValuesSourceReportText.text.font = Font.CreateDynamicFontFromOSFont("Consolas", 14);

            ValuesSourceChooser = _valuesSourceGroup.CreatePopup("Plugin:ValuesSource", "Select values source", new List<string> { "None", "Udp" }, "None", ValuesSourceChooserCallback);

            var originVisible = false;
            var originGroup = new UIGroup(_group);
            OriginTitle = _group.CreateButton("Origin", () => originGroup.SetVisible(originVisible = !originVisible), new Color(0.3f, 0.3f, 0.3f), Color.white, true);
            AlwaysDrawOriginToggle = originGroup.CreateToggle("Origin:AlwaysDrawOrigin", "Always draw origin", false, true);
            DrawOriginBoxToggle = originGroup.CreateToggle("Origin:DrawOriginBox", "Draw origin box", false, true);
            DrawOriginAnglesToggle = originGroup.CreateToggle("Origin:DrawOriginAngles", "Draw origin angles", false, true);
            originGroup.SetVisible(false);

            _motionTargetGroup = new UIGroup(_group);
            var motionTargetVisible = true;
            MotionTargetTitle = _group.CreateButton("Motion Target", () => _motionTargetGroup.SetVisible(motionTargetVisible = !motionTargetVisible), new Color(0.3f, 0.3f, 0.3f), Color.white, true);
            MotionTargetChooser = _motionTargetGroup.CreatePopup("Plugin:MotionTarget", "Select motion target", new List<string> { "None", "Physics Link", "Force" }, "None", MotionTargetChooserCallback, true);

            ValuesSourceChooserCallback(null);
            MotionTargetChooserCallback(null);
        }

        public void StoreConfig(JSONNode config)
        {
            _group.StoreConfig(config);
        }

        public void RestoreConfig(JSONNode config)
        {
            _group.RestoreConfig(config);
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            if (!awakecalled)
                Awake();

            needsStore = false;
            var config = ConfigManager.GetJSON(this);
            config["id"] = storeId;
            needsStore = true;

            return config;
        }


        public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            insideRestore = true;
            if (!awakecalled)
                Awake();

            ConfigManager.RestoreFromJSON(jc, this);
            insideRestore = false;
        }

        protected void SaveConfigCallback() => ConfigManager.OpenSaveDialog(SaveDialogCallback);
        protected void LoadConfigCallback() => ConfigManager.OpenLoadDialog(LoadDialogCallback);
        protected void SaveDefaultConfigCallback() => ConfigManager.SaveConfig($@"{PluginDir}\default.json", this);
        protected void SaveDialogCallback(string path) => ConfigManager.SaveConfig(path, this);
        protected void LoadDialogCallback(string path) => ConfigManager.LoadConfig(path, this);

        protected void MotionTargetChooserCallback(string s)
        {
            if (_motionTarget != null)
            {
                _motionTarget.TargetChanged -= OnTargetChanged;
                _motionTarget.DestroyUI(_motionTargetGroup);
                _motionTarget.Dispose();
                _motionTarget = null;
            }


            if (s == "Physics Link")
                _motionTarget = new PhysicsLinkMotionTarget();
            else if (s == "Force")
                _motionTarget = new ForceMotionTarget();
            else
            {
                MotionTargetChooser.valNoCallback = "None";
                return;
            }

            MotionTargetChooser.valNoCallback = s;
            _motionTarget.TargetChanged += OnTargetChanged;
            _motionTarget.CreateUI(_motionTargetGroup);
        }

        protected void ValuesSourceChooserCallback(string s)
        {
            _valuesSource?.DestroyUI(_valuesSourceGroup);
            _valuesSource?.Dispose();
            _valuesSource = null;

            if (s == "Udp")
                _valuesSource = new UdpValuesSource();
            else
            {
                ValuesSourceChooser.valNoCallback = "None";
                return;
            }

            ValuesSourceChooser.valNoCallback = s;
            _valuesSource.CreateUI(_valuesSourceGroup);
        }
    }
}