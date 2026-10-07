using IceFishing.View;
using UnityEditor;
using UnityEngine;

namespace IceFishing.EditorTools
{
    [CustomEditor(typeof(SettleView))]
    public sealed class SettleViewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var view = (SettleView)target;
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("绑定预制体引用", GUILayout.Height(32f)))
            {
                view.EnsureFishCardSlots();
                EditorUtility.SetDirty(view);
            }

            EditorGUILayout.HelpBox(
                "布局以 SettleView 预制体为准。运行时从 FishCardSlot_0 克隆列表项并 Bind 数据，不再代码生成按钮或卡片层级。",
                MessageType.Info);
        }
    }
}
