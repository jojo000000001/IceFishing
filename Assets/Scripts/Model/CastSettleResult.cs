using System.Collections.Generic;
using UnityEngine;

namespace IceFishing.Model
{
    /// <summary>
    /// 一局结束时的结算快照。只读展示，不含入账。
    /// </summary>
    public sealed class CastSettleResult
    {
        public int PeakDepthMeters;
        public int TotalCoins;
        public int TotalShells;
        public readonly List<FishSettleRow> Rows = new List<FishSettleRow>();

        public static CastSettleResult From(IReadOnlyList<FishDefinition> caught, float peakDepth)
        {
            var result = new CastSettleResult
            {
                PeakDepthMeters = Mathf.Max(0, Mathf.RoundToInt(peakDepth))
            };

            if (caught == null)
            {
                return result;
            }

            var index = new Dictionary<string, int>();
            for (var i = 0; i < caught.Count; i++)
            {
                var def = caught[i];
                if (def == null)
                {
                    continue;
                }

                result.TotalCoins += def.TokenValue;
                result.TotalShells += def.ShellValue;
                var key = string.IsNullOrEmpty(def.Id) ? def.name : def.Id;
                if (index.TryGetValue(key, out var rowIndex))
                {
                    result.Rows[rowIndex].Count++;
                    continue;
                }

                index.Add(key, result.Rows.Count);
                result.Rows.Add(new FishSettleRow
                {
                    Definition = def,
                    Count = 1,
                    CoinEach = def.TokenValue,
                    ShellEach = def.ShellValue,
                    Stars = Mathf.Max(1, def.Stars),
                    Sprite = ResolveSprite(def)
                });
            }

            return result;
        }

        static Sprite ResolveSprite(FishDefinition definition)
        {
            if (definition == null || definition.Prefab == null)
            {
                return null;
            }

            var renderer = definition.Prefab.GetComponentInChildren<SpriteRenderer>(true);
            return renderer != null ? renderer.sprite : null;
        }
    }

    public sealed class FishSettleRow
    {
        public FishDefinition Definition;
        public Sprite Sprite;
        public int Count;
        public int CoinEach;
        public int ShellEach;
        public int Stars;
    }
}
