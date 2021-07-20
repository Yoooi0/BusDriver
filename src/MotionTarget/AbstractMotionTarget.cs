using System.Linq;
using BusDriver.UI;
using UnityEngine;

namespace BusDriver.MotionTarget
{
    public abstract class AbstractMotionTarget : IMotionTarget
    {
        protected Atom Atom { get; private set; }

        private JSONStorableStringChooser AtomChooser;

        public abstract void Apply(Vector3 offset, Quaternion rotation);
        public virtual void OnSceneChanged() { }
        public virtual void OnSceneChanging() { }

        protected virtual void CreateCustomUI(IUIBuilder builder) { }

        public void CreateUI(IUIBuilder builder)
        {
            AtomChooser = builder.CreateScrollablePopup("MotionSource:Person", "Select Person", null, null, AtomChooserCallback, true);

            CreateCustomUI(builder);
        }

        public virtual void DestroyUI(IUIBuilder builder)
        {
            builder.Destroy(AtomChooser);
        }

        protected void FindAtoms()
        {
            var people = SuperController.singleton.GetAtoms().Where(a => a.type == "Person");
            var atoms = people.Select(a => a.uid).ToList();

            var defaultPerson = atoms.FirstOrDefault(uid => uid == Atom?.uid) ?? atoms.FirstOrDefault() ?? "None";
            atoms.Insert(0, "None");

            AtomChooser.choices = atoms;
            AtomChooserCallback(defaultPerson);
        }

        protected virtual void AtomChooserCallback(string s)
        {
            Atom = SuperController.singleton.GetAtomByUid(s);
            AtomChooser.valNoCallback = Atom == null ? "None" : s;
        }

        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}
