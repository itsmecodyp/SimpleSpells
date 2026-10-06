using BepInEx;
using System;
using System.Collections.Generic;
using static SimplyNeedsAName.MainClass.SpriteMovement;

namespace SimplyNeedsAName
{
	partial class MainClass : BaseUnityPlugin
	{
		public static void RegisterAllSpells()
		{
			RegisterSpell("spellTest", SpellActionMethod);
			RegisterSpell("spellFlamethrower", FlamethrowerAction);
			RegisterSpell("spellExtinguish", ExtinguishActionMethod);
			RegisterSpell("forceAll", ForceAllToPointAction);
			RegisterSpell("throwTile", ThrowTileSpell);
			RegisterSpell("spellAntimatter", AntimatterAction);
			RegisterSpell("spellFarmBless", FarmBlessAction);
			RegisterSpell("spellWildBless1", WildBlessAction);
			ActorTrait biomeSpreader = RegisterSpell("biomeSpread", WildBlessAction2);
			//biomeSpreader.action_special_effect = new WorldAction(WildBlessSpecialAction);
			RegisterSpell("spellStormSummon", StormSummonAction);

			RegisterSpell("spellCaptureUnitAsWeapon", captureUnitAsWeapon);


			AssetManager.spells.linkAssets();
			AssetManager.traits.linkAssets();
		}

		public static bool captureUnitAsWeapon(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
            Actor closest = ClosestActorToTile(World.world.getMouseTilePos(), 2f, controlledActorSc);
            bool hasWeapon = pSelf.a.equipment?.weapon?.isEmpty() == false;
			if (!hasWeapon)
			{
				if (closest != null)
				{
                    SpawnPokeball(closest, controlledActorSc, PokeballActionType.Capture, VeryRandomNeighborTile(closest.current_tile).posV3);
                }
            }
			else
			{
				if (pSelf.a.equipment.weapon._item.data.hasFlag("isUnit")){
                    SpawnPokeball(null, controlledActorSc, PokeballActionType.Release, MapBox.instance.getMousePos());
                }
            }
			return true;
		}


		public static bool WildBlessSpecialAction(BaseSimObject pTarget, WorldTile pTile = null)
		{
			if (Randy.randomChance(0.1f))
			{
				if (pTile != null)
				{
					List<WorldTile> tilesToBless = MainClass.GetTilesAround(pTile, 4, true);
					foreach (WorldTile tile in tilesToBless)
					{
						if (tile?.Type.biome_asset != null)
						{
							string dropID = "seeds_" + tile.Type.biome_asset.id.Replace("biome_", "");
							if (AssetManager.drops.get(dropID) != null)
							{
								DropsLibrary.action_drop_seeds(tile, dropID);
							}
						}
					}
				}
			}
			return true;
		}


		private static ActorTrait RegisterSpell(string id, Func<BaseSimObject, BaseSimObject, WorldTile, bool> action, int spellCost = 0)
		{
			SpellAsset spell = new SpellAsset { id = id, action = new AttackAction(action) };
			spell.cost_mana = spellCost;
			AssetManager.spells.add(spell);

			ActorTrait trait = new ActorTrait
			{
				id = id.Replace("spell", "").ToLower(),
				path_icon = AssetManager.traits.get("stupid").path_icon,
				group_id = "special"
			};
			trait.addSpell(spell.id);
			AssetManager.traits.add(trait);
            LocalizedTextManager.instance._localized_text.Add("trait_" + trait.id, trait.id);
            return trait;
		}


	}
}
