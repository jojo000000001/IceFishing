using UnityEngine;

namespace IceFishing.View
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class HookShieldVisual : MonoBehaviour
    {
        public void Apply()
        {
            ApplyFull();
        }

        public void ApplyFull()
        {
            var hook = transform.parent;
            if (hook == null)
            {
                return;
            }

            HookShieldSetup.EnsureChild(hook);
            HookShieldSetup.ApplyLayout(hook, transform);
        }

        void OnEnable()
        {
            var hook = transform.parent;
            if (hook != null && transform.Find(HookShieldSetup.FillChildName) != null)
            {
                HookShieldSetup.ApplyLayout(hook, transform);
            }
        }

        void OnValidate()
        {
            var hook = transform.parent;
            if (hook != null && transform.Find(HookShieldSetup.FillChildName) != null)
            {
                HookShieldSetup.ApplyLayout(hook, transform);
            }
        }
    }
}
