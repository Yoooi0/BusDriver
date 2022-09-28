using System;
using System.Linq;
using BusDriver.UI;
using BusDriver.Utils;
using MeshVR;
using SimpleJSON;
using UnityEngine;

namespace BusDriver.MotionTarget
{
    public abstract class AbstractMotionTarget : IMotionTarget
    {
        protected Atom Atom { get; private set; }

        private JSONStorableStringChooser AtomChooser;
        private UIDynamicButton ResetOriginButton;
        private UIDynamicButton RefreshButton;

        private JSONStorableAction ResetOriginAction;
        private JSONStorableAction RefreshAction;

        public abstract event EventHandler<TransformEventArgs> OriginReset;

        public abstract void Apply(Transform origin, Vector3 offset, Quaternion rotation);
        public abstract void ResetOrigin();
        protected abstract void PosePostLoadCallback();

        public virtual void OnSceneChanged() { }
        public virtual void OnSceneChanging() { }

        protected virtual void CreateCustomUI(IUIBuilder builder) { }
        
        public void CreateUI(IUIBuilder builder)
        {
            AtomChooser = builder.CreateScrollablePopup("MotionTarget:Person", "Select Person", null, null, AtomChooserCallback, true);

            CreateCustomUI(builder);

            ResetOriginButton = builder.CreateButton("Reset origin", ResetOrigin, true); 
            RefreshButton = builder.CreateButton("Refresh", RefreshButtonCallback, true);
            RefreshButton.buttonColor = new Color(0, 0.75f, 1f) * 0.8f;
            RefreshButton.textColor = Color.white;

            ResetOriginAction = UIManager.CreateAction("Reset origin", ResetOrigin);
            RefreshAction = UIManager.CreateAction("Refresh target", RefreshButtonCallback);
        }

        public virtual void DestroyUI(IUIBuilder builder)
        {
            builder.Destroy(AtomChooser);
            builder.Destroy(ResetOriginButton);
            builder.Destroy(RefreshButton);

            UIManager.RemoveAction(ResetOriginAction);
            UIManager.RemoveAction(RefreshAction);
        }

        public virtual void RestoreConfig(JSONNode config)
        {
            config.Store(AtomChooser);
            FindAtoms(AtomChooser.val);
        }

        public virtual void StoreConfig(JSONNode config)
        {
            config.Restore(AtomChooser);
        }

        protected virtual void RefreshButtonCallback() => FindAtoms(AtomChooser.val);

        protected void FindAtoms(string defaultUid = null)
        {
            var people = SuperController.singleton.GetAtoms().Where(a => a.type == "Person");
            var atoms = people.Select(a => a.uid).ToList();

            if (!atoms.Contains(defaultUid))
                defaultUid = atoms.FirstOrDefault(uid => uid == Atom?.uid) ?? atoms.FirstOrDefault() ?? "None";
            atoms.Insert(0, "None");

            AtomChooser.choices = atoms;
            AtomChooserCallback(defaultUid);
        }

        private void AddPoseListener()
        {
            if (Atom == null)
                return;

            var posePresetsManagerControl = Atom.presetManagerControls.First(c => c.name == "PosePresets");
            var posePresetsManager = posePresetsManagerControl.GetComponent<PresetManager>();
            posePresetsManager.postLoadEvent.AddListener(PosePostLoadCallback);
        }

        private void RemovePoseListener()
        {
            if (Atom == null)
                return;

            var posePresetsManagerControl = Atom.presetManagerControls.First(c => c.name == "PosePresets");
            var posePresetsManager = posePresetsManagerControl.GetComponent<PresetManager>();
            posePresetsManager.postLoadEvent.RemoveListener(PosePostLoadCallback);
        }

        protected virtual void AtomChooserCallback(string s)
        {
            RemovePoseListener();
            Atom = SuperController.singleton.GetAtomByUid(s);
            AtomChooser.valNoCallback = Atom == null ? "None" : s;
            AddPoseListener();
        }

        protected virtual void Dispose(bool disposing)
        {
            RemovePoseListener();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
