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
            if (GUILayout.Button("生成 Grid 下 6 个白色鱼卡占位", GUILayout.Height(32f)))
            {
                view.EnsureFishCardSlots();
                EditorUtility.SetDirty(view);
            }

            EditorGUILayout.HelpBox(
                "白色占位：调 Grid cell 与边距。鱼与奖励（Rewards/Coin、Shell 下图标与 Value 文字）均在 FishCardSlot_0 里调 RectTransform，运行时克隆沿用。列表纵向滚动展示全部鱼种。",
                MessageType.Info);
        }
    }
}
