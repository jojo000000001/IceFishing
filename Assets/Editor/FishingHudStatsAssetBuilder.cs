using IceFishing.Model;
using IceFishing.View;
using UnityEditor;
using UnityEngine;

namespace IceFishing.EditorTools
{
    static class FishingHudStatsAssetBuilder
    {
        const string StatsFolder = "Assets/Data/UI";
        const string ProtectionPath = StatsFolder + "/HudStat_Protection.asset";
        const string HaulPath = StatsFolder + "/HudStat_Haul.asset";
        const string DepthPath = StatsFolder + "/HudStat_Depth.asset";

        [MenuItem("IceFishing/Ensure Fishing HUD Stats")]
        public static void EnsureFromMenu()
        {
            Ensure();
        }

        public static FishingHudStatsDefinition Ensure()
        {
            IceFishingSceneBuilder.EnsureFolder("Assets/Data");
            IceFishingSceneBuilder.EnsureFolder(StatsFolder);

            var protection = EnsureStat(
                ProtectionPath,
                FishingHudStatKind.Protection,
                "Assets/Art/Icon/Reel.png",
                0f,
                5f,
                -24f);
            var haul = EnsureStat(
                HaulPath,
                FishingHudStatKind.Haul,
                "Assets/Art/Icon/Hook.png",
                2f,
                2f,
                -96f);
            var depth = EnsureStat(
                DepthPath,
                FishingHudStatKind.Depth,
                "Assets/Art/Icon/Spool.png",
                0f,
                150f,
                -168f);

            var profile = AssetDatabase.LoadAssetAtPath<FishingHudStatsDefinition>(FishingHudStatsDefinition.DefaultAssetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<FishingHudStatsDefinition>();
                AssetDatabase.CreateAsset(profile, FishingHudStatsDefinition.DefaultAssetPath);
            }

            profile.SharedPill = AssetDatabase.LoadAssetAtPath<Sprite>(IceFishingSceneBuilder.HudArtFolder + "/HudPill.png");
            profile.Stats = new[] { protection, haul, depth };
            EditorUtility.SetDirty(profile);

            AssetDatabase.SaveAssets();
            return profile;
        }

        static FishingHudStatDefinition EnsureStat(
            string path,
            FishingHudStatKind kind,
            string iconPath,
            float previewCurrent,
            float maxValue,
            float topOffsetY)
        {
            var stat = AssetDatabase.LoadAssetAtPath<FishingHudStatDefinition>(path);
            if (stat == null)
            {
                stat = ScriptableObject.CreateInstance<FishingHudStatDefinition>();
                AssetDatabase.CreateAsset(stat, path);
            }

            stat.Kind = kind;
            stat.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            stat.PreviewCurrent = previewCurrent;
            stat.MaxValue = maxValue;
            stat.TopOffsetY = topOffsetY;
            stat.PillOverride = null;
            EditorUtility.SetDirty(stat);
            return stat;
        }
    }
}
