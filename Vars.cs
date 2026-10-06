using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;

namespace SimplyNeedsAName
{
    partial class MainClass : BaseUnityPlugin
    {
        public static bool useMouseInstead = true;
        public static Dictionary<BaseSimObject, List<TileObject>> heldTileGroups = new Dictionary<BaseSimObject, List<TileObject>>();
        public static int radius = 3;
        public static bool useCircleRadius = true;
        public static bool aboutToUseSpell;
        private Actor lastControlledActor;
        public const string pluginGuid = "cody.simple.spells";
        public const string pluginName = "SimpleSpells";
        public const string pluginVersion = "0.0.0.2";
        public static MainClass Instance { get; private set; }
        public bool showWindow;
        public Rect controlWindowRect = new Rect(126f, 1f, 1f, 1f);
        private int currentHotkeySet = 0; // 0-based (set 0 = keys 1–9, set 1 = 10–18, etc.)
        private KeyCode? heldHotkey = null;

        private readonly Dictionary<int, SpellAsset> hotkeySpells = new Dictionary<int, SpellAsset>();

        private ConfigEntry<KeyCode>[] hotkeyConfig;
        private KeyCode[] hotkeys;
        public static SpellAsset selectedAsset;
        private readonly KeyCode[] pageKeys =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9
        };
        //controlled actor shortcut
        public static Actor controlledActorSc => getSelectedControlled();

        public static Actor getSelectedControlled()
        {
            if (global::Config.game_loaded)
            {
				Actor c = null;
				if (PlayerControl.isControllingUnit())
				{
					return ControllableUnit._unit_main;
				}
				else
				{
					return c;
				}
			}
            return null;
        }

        public static List<WorldTile> GetTilesAround(WorldTile centerTile, int radius, bool useCircle = false)
        {
            List<WorldTile> tiles = new List<WorldTile>();

            if (centerTile == null) return tiles;

            Vector2Int centerPos = centerTile.pos;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Skip tiles outside the circle if useCircle is true
                    if (useCircle && (x * x + y * y) > (radius * radius))
                        continue;

                    Vector2Int offset = new Vector2Int(centerPos.x + x, centerPos.y + y);
                    WorldTile tile = MapBox.instance.GetTile(offset.x, offset.y);
                    if (tile != null)
                    {
                        tiles.Add(tile);
                    }
                }
            }

            return tiles;
        }

    }
}
