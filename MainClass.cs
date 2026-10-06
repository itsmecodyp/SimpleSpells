using BepInEx;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BepInEx.Configuration;
using static SimpleMessages.Messages;

namespace SimplyNeedsAName
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    partial class MainClass : BaseUnityPlugin
    {
        public static HarmonyLib.Harmony harmony;

        public void Awake()
        {
            Instance = this;
            hotkeyConfig = new ConfigEntry<KeyCode>[9];
            hotkeys = new KeyCode[9];

            KeyCode[] defaults =
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

            for (int i = 0; i < 9; i++)
            {
                hotkeyConfig[i] = Config.Bind(
                    "Hotkeys",
                    $"Spell Slot {i + 1}",
                    defaults[i],
                    $"Hotkey for spell slot {i + 1}"
                );

                hotkeys[i] = hotkeyConfig[i].Value;
            }
            HarmonyPatchSetup();


            Debug.Log("Loaded " + pluginName);
        }
        public void OnDestroy()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }


        public void Update()
        {
            if (global::Config.game_loaded)
            {
                Actor current = controlledActorSc;

                if (current != lastControlledActor)
                {
                    lastControlledActor = current;
                    AssignHotkeysToSpells();
                }
                if (Input.GetKeyDown(KeyCode.L))
                { // test say to make sure it works
                    foreach (Actor unit in MapBox.instance.units.units_only_alive.ToList())
                    {
                        if (unit.data != null && unit.data.name != null)
                            ActorSay(unit, unit.data.name);
                    }
                }

                //update power clicks
                if (Input.GetMouseButtonDown(0))
                {
                    WorldTile tile = World.world.getMouseTilePos();
                    if (tile != null)
                    {
                        if (selectedAsset != null && selectedAsset.action != null)
                        {
                            if (tile.hasUnits())
                            {
                                selectedAsset.action(controlledActorSc, tile._units.First(), tile);
                            }
                            else
                            {
                                selectedAsset.action(controlledActorSc, null, tile);
                            }
                            selectedAsset = null;
                            aboutToUseSpell = false;
                        }
                    }
                }
                bool isCtrlHeld = Input.GetKey(KeyCode.LeftControl);

                if (isCtrlHeld)
                {
                    for (int i = 0; i < pageKeys.Length; i++)
                    {
                        if (Input.GetKeyDown(pageKeys[i]))
                        {
                            currentHotkeySet = i;
                            AssignHotkeysToSpells();

                            Debug.Log($"Switched to hotkey set {i + 1}");
                            return;
                        }
                    }
                }
                for (int i = 0; i < hotkeys.Length; i++)
                {
                    if (Input.GetKeyDown(hotkeys[i]))
                    {
                        if (hotkeySpells.TryGetValue(i, out var spell))
                        {
                            selectedAsset = spell;
                            heldHotkey = hotkeys[i];

                            Debug.Log($"Holding spell: {selectedAsset.id}");
                        }

                        return;
                    }
                }
                // Handle releasing hotkey to cast
                if (heldHotkey.HasValue && Input.GetKeyUp(heldHotkey.Value))
                {
                    TryCastSelectedSpell();
                    heldHotkey = null;
                }
            }
            controlWindowRect.height = 0f;
			TestItemControl();
      UpdatePokeballs();
		}

		public static WorldTile VeryRandomNeighborTile(WorldTile start)
		{
			return start.neighbours.GetRandom().neighbours.GetRandom().neighbours.GetRandom().neighbours.GetRandom().neighbours.GetRandom().neighbours.GetRandom().neighbours.GetRandom().neighbours.GetRandom();
		}

		public void OnGUI()
        {
            if (GUILayout.Button("menu"))
            {
                showWindow = !showWindow;
            }
            if (showWindow)
            {
                controlWindowRect = GUILayout.Window(13364, controlWindowRect, controlWindow, "c", GUILayout.MaxWidth(300f), GUILayout.MinWidth(200f));
            }
        }

        private void AssignHotkeysToSpells()
        {
            hotkeySpells.Clear();

            if (controlledActorSc == null || !controlledActorSc.hasSpells()) return;

            List<SpellAsset> allSpells = new List<SpellAsset>();

            allSpells.AddRange(controlledActorSc._spells._spells);

            if (controlledActorSc.hasSubspecies())
                allSpells.AddRange(controlledActorSc.subspecies.spells._spells);

            if (controlledActorSc.hasReligion())
                allSpells.AddRange(controlledActorSc.religion.spells._spells);

            if (controlledActorSc.asset.hasDefaultSpells())
                allSpells.AddRange(controlledActorSc.asset.spells._spells);

            int offset = currentHotkeySet * hotkeys.Length;

            for (int i = 0; i < hotkeys.Length; i++)
            {
                int spellIndex = offset + i;
                if (spellIndex < allSpells.Count)
                {
                    hotkeySpells[i] = allSpells[spellIndex];
                }
            }
        }

        private void TryCastSelectedSpell()
        {
            if (selectedAsset != null && selectedAsset.action != null)
            {
                WorldTile tile = World.world.getMouseTilePos();
                if (tile != null)
                {
                    if (tile.hasUnits())
                    {
                        selectedAsset.action(controlledActorSc, tile._units.First(), tile);
                    }
                    else
                    {
                        selectedAsset.action(controlledActorSc, null, tile);
                    }
                    Debug.Log($"Cast spell: {selectedAsset.id}");
                    selectedAsset = null;
                    aboutToUseSpell = false;
                }
            }
        }

        public void controlWindow(int windowID)
        {
            if (controlledActorSc != null)
            {
                if (GUILayout.Button("AddAllSpells"))
                {
                    foreach(ActorTrait spell in AssetManager.traits.list)
                    {
                        if(spell.hasSpells() && controlledActorSc.hasTrait(spell) == false)
                        {
                            controlledActorSc.addTrait(spell);
                        }
                    }
                    controlledActorSc.setStatsDirty();
                    AssignHotkeysToSpells();
                }
                if(controlledActorSc.hasSpells())
                {
                    GUILayout.Label($"Hotkey Set: {currentHotkeySet + 1}");
                    GUILayout.Label("Spells (Hotkeys 1–9):");
                    for (int i = 0; i < hotkeys.Length; i++)
                    {
                        if (hotkeySpells.TryGetValue(i, out SpellAsset spell))
                        {
                            GUILayout.BeginHorizontal();

                            bool isHeld = heldHotkey == hotkeys[i];
                            GUIStyle keyStyle = new GUIStyle(GUI.skin.label);
                            keyStyle.normal.textColor = isHeld ? Color.yellow : Color.white;

                            GUIStyle spellStyle = new GUIStyle(GUI.skin.button);
                            if (selectedAsset == spell)
                                spellStyle.normal.textColor = Color.green;

                            GUILayout.Label(hotkeys[i].ToString(), keyStyle, GUILayout.Width(60));
                            GUILayout.Label(spell.id, spellStyle);
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }
            GUI.DragWindow();
        }
    }


}
