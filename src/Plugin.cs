using System;
using System.Linq;
using BusDriver.Config;
using DebugUtils;
using BusDriver.Utils;
using System.Text;
using BusDriver.ValuesSource;
using BusDriver.MotionTarget;
using UnityEngine;

namespace BusDriver
{
    public partial class Plugin : MVRScript
    {
        public static readonly string PluginName = "Bus Driver";
        public static readonly string PluginAuthor = "Yoooi";
        public static readonly string PluginDir = $@"Custom\Scripts\{PluginAuthor}\{PluginName.Replace(" ", "")}";

        private bool _initialized;
        private int _physicsIteration;

        private IValuesSource _valuesSource;
        private IMotionTarget _motionTarget;

        public override void Init()
        {
            base.Init();

            try
            {
                try
                {
                    var defaultPath = SuperController.singleton.GetFilesAtPath(PluginDir, "*.json")?.FirstOrDefault(s => s.EndsWith("default.json"));
                    if (defaultPath != null)
                        ConfigManager.LoadConfig(defaultPath, this);

                    SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;
                } catch { }
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        public override void InitUI()
        {
            base.InitUI();
            if (UITransform == null)
                return;

            try
            {
                CreateUI();

                _initialized = true;
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        protected void Update()
        {
            if (SuperController.singleton.isLoading)
                ComponentCache.Clear();

            if (!_initialized || SuperController.singleton.isLoading)
                return;

            var sb = new StringBuilder();
            sb.Append("Up:      ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.L0) ?? float.NaN).AppendLine()
              .Append("Right:   ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.L1) ?? float.NaN).AppendLine()
              .Append("Forward: ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.L2) ?? float.NaN).AppendLine()
              .Append("Yaw:     ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.R0) ?? float.NaN).AppendLine()
              .Append("Pitch:   ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.R1) ?? float.NaN).AppendLine()
              .Append("Roll:    ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.R2) ?? float.NaN).AppendLine();

            ValuesSourceReportText.val = sb.ToString();

            DebugDraw.Draw();
            DebugDraw.Enabled = DebugDrawEnableToggle.val;
            _physicsIteration = 0;
        }

        protected void FixedUpdate()
        {
            if (!_initialized || SuperController.singleton.isLoading)
                return;

            if (_physicsIteration == 0)
                DebugDraw.Clear();

            try
            {
                _valuesSource?.Update();

                if (!SuperController.singleton.freezeAnimation && _valuesSource != null && _motionTarget != null)
                {
                    var upValue = UpRangeSlider.val * (_valuesSource.GetValue(DeviceAxis.L0) - DeviceAxis.DefaultValue(DeviceAxis.L0)) * 2;
                    var rightValue = RightRangeSlider.val * (_valuesSource.GetValue(DeviceAxis.L1) - DeviceAxis.DefaultValue(DeviceAxis.L1)) * 2;
                    var forwardValue = ForwardRangeSlider.val * (_valuesSource.GetValue(DeviceAxis.L2) - DeviceAxis.DefaultValue(DeviceAxis.L2)) * 2;
                    var yawValue = YawRangeSlider.val * (_valuesSource.GetValue(DeviceAxis.R0) - DeviceAxis.DefaultValue(DeviceAxis.R0)) * 2;
                    var rollValue = RollRangeSlider.val * (_valuesSource.GetValue(DeviceAxis.R1) - DeviceAxis.DefaultValue(DeviceAxis.R1)) * 2;
                    var pitchValue = PitchRangeSlider.val * (_valuesSource.GetValue(DeviceAxis.R2) - DeviceAxis.DefaultValue(DeviceAxis.R2)) * 2;

                    var newUp = Vector3.zero;
                    if (UpDirectionChooser.val == "+Up") newUp = Vector3.up;
                    else if (UpDirectionChooser.val == "+Right") newUp = Vector3.right;
                    else if (UpDirectionChooser.val == "+Forward") newUp = Vector3.forward;
                    else if (UpDirectionChooser.val == "-Up") newUp = -Vector3.up;
                    else if (UpDirectionChooser.val == "-Right") newUp = -Vector3.right;
                    else if (UpDirectionChooser.val == "-Forward") newUp = -Vector3.forward;

                    var coordinatesRotation = Quaternion.FromToRotation(Vector3.up, newUp);
                    var rotation = Quaternion.Euler(coordinatesRotation * new Vector3(pitchValue, yawValue, rollValue));
                    var offset = coordinatesRotation * new Vector3(rightValue, upValue, forwardValue);

                    _motionTarget?.Apply(offset, rotation);
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }

            if (_physicsIteration == 0)
                DebugDraw.Enabled = false;

            _physicsIteration++;
        }

        protected void OnSceneLoaded()
        {
            MotionTargetChooserCallback(null);
        }

        protected void OnDestroy()
        {
            DebugDraw.Clear();

            _valuesSource?.Dispose();
            _motionTarget?.Dispose();

            _valuesSource = null;
            _motionTarget = null;
        }
    }
}