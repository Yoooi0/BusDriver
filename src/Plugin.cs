using System;
using System.Linq;
using BusDriver.Config;
using DebugUtils;
using BusDriver.Utils;
using System.Text;
using BusDriver.ValuesSource;
using BusDriver.MotionTarget;
using System.Collections;
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
        private Atom _origin;
        private FreeControllerV3 _originController;
        private DebugUtils.LineDrawer _originDrawer;
        private Material _originMaterial;
        private StringBuilder _valuesSourceReportBuilder;

        private IEnumerator SpawnOriginObject()
        {
            const string originUid = "_BusDriverOrigin";
            yield return StartCoroutine(SuperController.singleton.AddAtomByType("Cube", originUid, userInvoked: false));
            _origin = SuperController.singleton.GetAtoms().FirstOrDefault(a => a.uid == originUid);
            _originController = _origin.GetComponentInChildren<FreeControllerV3>();

            var rigidbody = _origin.GetComponentInChildren<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            var meshRenderer = _origin.GetComponentInChildren<MeshRenderer>();
            meshRenderer.transform.parent = null;
            Destroy(meshRenderer.transform.gameObject);

            var meshFilter = _origin.GetComponentInChildren<MeshFilter>();
            meshFilter.transform.parent = null;
            Destroy(meshFilter.transform.gameObject);
        }

        public override void Init()
        {
            base.Init();

            _valuesSourceReportBuilder = new StringBuilder();
            StartCoroutine(SpawnOriginObject());
            _originDrawer = new DebugUtils.LineDrawer();
            _originMaterial = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));
            _originMaterial.color = Color.white;
            _originMaterial.SetFloat("_Offset", 1f);
            _originMaterial.SetFloat("_MinAlpha", 1f);
            _originMaterial.SetPass(0);

            try
            {
                try
                {
                    var defaultPath = SuperController.singleton.GetFilesAtPath(PluginDir, "*.json")?.FirstOrDefault(s => s.EndsWith("default.json"));
                    if (defaultPath != null)
                        ConfigManager.LoadConfig(defaultPath, this);
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


        private Color GetOriginVertexColor(float value, float min, float max)
        {
            return Color.Lerp(Color.white, Color.black, Mathf.Pow((value - min) / (max - min), 0.5f));
        }

        private void DrawOriginBox()
        {
            var t = _originController.transform;
            var camera = Camera.main.transform.position;
            var extents = new Vector3(RightRangeSlider.val, UpRangeSlider.val, ForwardRangeSlider.val);

            var p0 = t.position + t.rotation * new Vector3(-extents.x, -extents.y, -extents.z);
            var p1 = t.position + t.rotation * new Vector3( extents.x,  extents.y,  extents.z);
            var p2 = t.position + t.rotation * new Vector3(-extents.x, -extents.y,  extents.z);
            var p3 = t.position + t.rotation * new Vector3(-extents.x,  extents.y, -extents.z);
            var p4 = t.position + t.rotation * new Vector3( extents.x, -extents.y, -extents.z);
            var p5 = t.position + t.rotation * new Vector3(-extents.x,  extents.y,  extents.z);
            var p6 = t.position + t.rotation * new Vector3( extents.x, -extents.y,  extents.z);
            var p7 = t.position + t.rotation * new Vector3( extents.x,  extents.y, -extents.z);

            var count = 4;
            for (var i = 0; i < count; i++)
            {
                var tt = i / (count - 1f);
                var p51 = Vector3.Lerp(p5, p1, tt); var d51 = Vector3.Distance(camera, p51);
                var p37 = Vector3.Lerp(p3, p7, tt); var d37 = Vector3.Distance(camera, p37);
                var p26 = Vector3.Lerp(p2, p6, tt); var d26 = Vector3.Distance(camera, p26);
                var p04 = Vector3.Lerp(p0, p4, tt); var d04 = Vector3.Distance(camera, p04);
                var p52 = Vector3.Lerp(p5, p2, tt); var d52 = Vector3.Distance(camera, p52);
                var p16 = Vector3.Lerp(p1, p6, tt); var d16 = Vector3.Distance(camera, p16);
                var p74 = Vector3.Lerp(p7, p4, tt); var d74 = Vector3.Distance(camera, p74);
                var p30 = Vector3.Lerp(p3, p0, tt); var d30 = Vector3.Distance(camera, p30);
                var p71 = Vector3.Lerp(p7, p1, tt); var d71 = Vector3.Distance(camera, p71);
                var p46 = Vector3.Lerp(p4, p6, tt); var d46 = Vector3.Distance(camera, p46);
                var p35 = Vector3.Lerp(p3, p5, tt); var d35 = Vector3.Distance(camera, p35);
                var p02 = Vector3.Lerp(p0, p2, tt); var d02 = Vector3.Distance(camera, p02);

                var min = Mathf.Min(d51, d37, d26, d04, d52, d16, d74, d30, d71, d46, d35, d02);
                var max = Mathf.Max(d51, d37, d26, d04, d52, d16, d74, d30, d71, d46, d35, d02);
                
                _originDrawer.PushLine(p51, p37, GetOriginVertexColor(d51, min, max), GetOriginVertexColor(d37, min, max));
                _originDrawer.PushLine(p26, p04, GetOriginVertexColor(d26, min, max), GetOriginVertexColor(d04, min, max));
                _originDrawer.PushLine(p52, p16, GetOriginVertexColor(d52, min, max), GetOriginVertexColor(d16, min, max));
                _originDrawer.PushLine(p74, p30, GetOriginVertexColor(d74, min, max), GetOriginVertexColor(d30, min, max));
                _originDrawer.PushLine(p71, p46, GetOriginVertexColor(d71, min, max), GetOriginVertexColor(d46, min, max));
                _originDrawer.PushLine(p35, p02, GetOriginVertexColor(d35, min, max), GetOriginVertexColor(d02, min, max));
                _originDrawer.PushLine(p51, p26, GetOriginVertexColor(d51, min, max), GetOriginVertexColor(d26, min, max));
                _originDrawer.PushLine(p52, p30, GetOriginVertexColor(d52, min, max), GetOriginVertexColor(d30, min, max));
                _originDrawer.PushLine(p16, p74, GetOriginVertexColor(d16, min, max), GetOriginVertexColor(d74, min, max));
                _originDrawer.PushLine(p02, p46, GetOriginVertexColor(d02, min, max), GetOriginVertexColor(d46, min, max));
                _originDrawer.PushLine(p35, p71, GetOriginVertexColor(d35, min, max), GetOriginVertexColor(d71, min, max));
                _originDrawer.PushLine(p37, p04, GetOriginVertexColor(d37, min, max), GetOriginVertexColor(d04, min, max));
            }
        }

        private void DrawOriginAngles()
        {
            var t = _originController.transform;
            var radius = 0.2f;

            _originDrawer.PushLine(t.position, t.position + Quaternion.AngleAxis(-PitchRangeSlider.val, t.right) * t.up * radius, Color.red);
            _originDrawer.PushArc(t.position, t.right, t.up, radius, -PitchRangeSlider.val, PitchRangeSlider.val, Color.red);
            _originDrawer.PushLine(t.position, t.position + Quaternion.AngleAxis(PitchRangeSlider.val, t.right) * t.up * radius, Color.red);

            _originDrawer.PushLine(t.position, t.position + Quaternion.AngleAxis(-YawRangeSlider.val, t.up) * t.forward * radius, Color.green);
            _originDrawer.PushArc(t.position, t.up, t.forward, radius, -YawRangeSlider.val, YawRangeSlider.val, Color.green);
            _originDrawer.PushLine(t.position, t.position + Quaternion.AngleAxis(YawRangeSlider.val, t.up) * t.forward * radius, Color.green);

            _originDrawer.PushLine(t.position, t.position + Quaternion.AngleAxis(-RollRangeSlider.val, t.forward) * t.up * radius, Color.blue);
            _originDrawer.PushArc(t.position, t.forward, t.up, radius, -RollRangeSlider.val, RollRangeSlider.val, Color.blue);
            _originDrawer.PushLine(t.position, t.position + Quaternion.AngleAxis(RollRangeSlider.val, t.forward) * t.up * radius, Color.blue);
        }

        protected void Update()
        {
            if (SuperController.singleton.isLoading)
                ComponentCache.Clear();

            if (!_initialized || SuperController.singleton.isLoading)
                return;

            _originDrawer.Clear();
            if (_originController.selected || AlwaysDrawOriginToggle.val)
            {
                if (DrawOriginBoxToggle.val)
                    DrawOriginBox();
                if (DrawOriginAnglesToggle.val)
                    DrawOriginAngles();
            }

            _originDrawer.Draw(_originMaterial);

            _valuesSourceReportBuilder.Length = 0;
            _valuesSourceReportBuilder.Append("Up:      ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.L0) ?? float.NaN).AppendLine()
                                            .Append("Right:   ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.L1) ?? float.NaN).AppendLine()
                                            .Append("Forward: ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.L2) ?? float.NaN).AppendLine()
                                            .Append("Yaw:     ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.R0) ?? float.NaN).AppendLine()
                                            .Append("Pitch:   ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.R1) ?? float.NaN).AppendLine()
                                            .Append("Roll:    ").AppendFormat("{0:F3}", _valuesSource?.GetValue(DeviceAxis.R2) ?? float.NaN).AppendLine();

            ValuesSourceReportText.val = _valuesSourceReportBuilder.ToString();

            DebugDraw.Draw();
            DebugDraw.Enabled = DebugDrawEnableToggle.val;
            _physicsIteration = 0;
        }

        private bool _isLoading;
        protected void FixedUpdate()
        {
            if (!_initialized)
                return;

            var isLoading = SuperController.singleton.isLoading;
            if (!_isLoading && isLoading)
                OnSceneChanging();
            else if (_isLoading && !isLoading)
                OnSceneChanged();
            _isLoading = isLoading;

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

                    _motionTarget?.Apply(_originController.transform, offset, rotation);
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

        private void OnTargetChanged(object sender, TargetChangedEventArgs e)
        {
            if (e.Transform == null)
                return;

            _originController.transform.position = e.Transform.position;
            _originController.transform.rotation = e.Transform.rotation;
        }

        protected void OnSceneChanging()
        {
            _motionTarget?.OnSceneChanging();
        }

        protected void OnSceneChanged()
        {
            _motionTarget?.OnSceneChanged();
        }

        protected void OnDestroy()
        {
            DebugDraw.Clear();

            _valuesSource?.Dispose();
            _motionTarget?.Dispose();

            _valuesSource = null;
            _motionTarget = null;

            SuperController.singleton.RemoveAtom(_origin);
        }
    }
}