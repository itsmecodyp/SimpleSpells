using BepInEx;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimplyNeedsAName
{
	partial class MainClass : BaseUnityPlugin
	{
		//spell actions
		public static bool SpellActionMethod(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
			if (pTile != null)
			{
				MainClass.Instance.StartCoroutine(ChainLightning(pTile, 5)); // e.g., 5 jumps
			}
			return true;
		}

		public static bool ExtinguishActionMethod(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
			if (pTile != null)
			{
				List<WorldTile> nearbyTiles = GetTilesAround(pTile, 15); // Radius of 3

				foreach (WorldTile tile in nearbyTiles)
				{
					if (tile.isOnFire())
					{
						tile.stopFire();
					}

					if (tile.building != null && tile.building.hasStatus("burning"))
					{
						tile.building.stopFire();
					}
				}
			}
			return true;
		}

		public static bool FlamethrowerAction(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
			WorldTile targetTile = null;
			if (pSelf.isActor() && pSelf.a == controlledActorSc)
			{
				//is controlled, use mouse to aim
				if (MapBox.instance.getMouseTilePos() != null)
				{
					targetTile = MapBox.instance.getMouseTilePos();
				}
			}
			if (targetTile != null)
			{
				MainClass.Instance.StartCoroutine(ThrowFlame(targetTile, 60, pSelf.a));
			}
			return true;
		}

		public static bool ThrowTileSpell(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
			if (!heldTileGroups.ContainsKey(pSelf))
			{
				WorldTile centerTile = World.world.getMouseTilePos();//pSelf.current_tile;

				List<WorldTile> tilesToLift = MainClass.GetTilesAround(centerTile, radius, true);
				List<TileObject> tileObjects = new List<TileObject>();

				foreach (var tile in tilesToLift)
				{
					// Calculate the world offset relative to the caster's position
					Vector3 worldOffset = tile.posV3 - centerTile.posV3;
					// Add base height to make them float higher
					Vector3 baseHover = new Vector3(0f, 1.2f, 0f); // height boost
					Vector3 hoverLocalOffset = worldOffset + baseHover;
					tileObjects.Add(new TileObject(tile, hoverLocalOffset));
					MapAction.decreaseTile(tile, true);
					tile.building?.getHit(10000);
				}

				heldTileGroups[pSelf] = tileObjects;

				foreach (var tile in tileObjects)
				{
					// Start hovering the tiles individually
					MainClass.Instance.StartCoroutine(HoverTileAboveActor(pSelf, tile));
				}
			}
			else if (pTile != null)
			{
				List<TileObject> held = heldTileGroups[pSelf];
				heldTileGroups.Remove(pSelf);

				foreach (var tile in held)
				{
					// Calculate landing point with random spread
					Vector3 start = tile.tileObj.transform.position;
					Vector3 end = pTile.posV3 + tile.originalOffset + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, 0f); // random spread at landing
					MainClass.Instance.StartCoroutine(LaunchTileAtTarget(tile, start, end));
				}
			}

			return true;
		}

		public static bool ForceAllToPointAction(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
			if (pTile != null)
			{
				foreach (Actor unit in World.world.units.units_only_alive.ToList())
				{
					if (pTile != null)
					{
						if (controlledActorSc != null)
						{
							if (unit != controlledActorSc)
							{
								PushActorTowardsTile(pTile.pos, unit);
							}
						}
						else
						{
							if (unit != pSelf)
							{
								PushActorTowardsTile(pTile.pos, unit, time: 1f, height: 1f);
							}
						}
					}

				}
			}
			return true;
		}

		public static void OnTileLanding(TileObject tile, Vector3 position)
		{
			Debug.Log("Tile landed at: " + position);
			WorldTile landedtile = World.world.GetTile((int)position.x, (int)position.y);
			EffectsLibrary.spawnAtTileRandomScale("fx_explosion_tiny", landedtile, 0.15f, 0.3f);
			MusicBox.playSound("event:/SFX/EXPLOSIONS/ExplosionSmall", landedtile, false, false);
			MapAction.explodeTile(landedtile, 3, 3, landedtile, AssetManager.terraform.get("bomb"));
			// Destroy the visuals for now
			GameObject.Destroy(tile.tileObj);
			if (tile.buildingObj) GameObject.Destroy(tile.buildingObj);
			if (tile.actorObj) GameObject.Destroy(tile.actorObj);
		}

		public static void PushActorTowardsTile(Vector2Int point, Actor pActor, float time = 1f, float velocitySpeed = 1f, float height = 1f)
		{
			float pX = (point.x - pActor.current_position.x) / (time * velocitySpeed);
			float pY = (point.y - pActor.current_position.y) / (time * velocitySpeed);

			float damp = 10f;

			// We replace "lift * time * damp" with a value based on the desired height.
			// Rearranged to hit peak height at midpoint (basic parabola assumption):
			float pZ = (height * 2f / time) - (pActor.position_height / (time * damp));

			pActor.velocity_speed = velocitySpeed;
			pActor.velocity.x = pX;
			pActor.velocity.y = pY;
			pActor.velocity.z = pZ;
			pActor.under_forces = true;
		}

		public static bool AntimatterAction(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
			if (pTile != null)
			{
				DropsLibrary.action_antimatter_bomb(pTile);
			}
			return true;
		}

		public static bool FarmBlessAction(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
			if (pTile != null)
			{
				List<WorldTile> tilesToBless = MainClass.GetTilesAround(pTile, 4, true);
				foreach (WorldTile tile in tilesToBless)
				{
					DropsLibrary.tryToGrowWheat(tile);
				}
			}
			return true;
		}

		public static bool WildBlessAction(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
			if (pTile != null)
			{
				List<WorldTile> tilesToBless = MainClass.GetTilesAround(pTile, 4, true);
				foreach (WorldTile tile in tilesToBless)
				{
					if (Randy.randomBool())
					{
						DropsLibrary.action_fertilizer_trees(tile);
					}
					else
					{
						DropsLibrary.action_fertilizer_plants(tile);
					}
					/*
					if(tile.Type.biome_asset != null)
					{
									DropsLibrary.useSeedOn(tile, tile.Type.biome_asset.getTileLow(), tile.Type.biome_asset.getTileHigh());
					}
					*/
				}
			}
			return true;
		}
		
		public static bool WildBlessAction2(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
			if (pTile != null)
			{
				List<WorldTile> tilesToBless = MainClass.GetTilesAround(pTile, 4, true);
				foreach (WorldTile tile in tilesToBless)
				{
					if (tile.Type.biome_asset != null)
					{
						string dropID = "seeds_" + tile.Type.biome_asset.id.Replace("biome_", "");
						if (AssetManager.drops.get(dropID) != null)
						{
							DropsLibrary.action_drop_seeds(tile, dropID);
						}
					}
				}
			}
			return true;
		}

		public static bool StormSummonAction(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile = null)
		{
			if (pTile != null)
			{
				BaseEffect rainCloud = EffectsLibrary.spawn("fx_cloud", pTile, "cloud_rain", null, 0f, -1f, -1f);
				rainCloud.setScale(0.1f);
			}
			return true;
		}
	}

	//effects that happen over time/multiple times during spells and needed enumerator
	partial class MainClass : BaseUnityPlugin
	{
		public static IEnumerator ChainLightning(WorldTile startTile, int jumps)
		{
			WorldTile currentTile = startTile;

			for (int i = 0; i < jumps; i++)
			{
				if (currentTile == null) yield break;

				EffectsLibrary.spawnAtTile("fx_lightning_medium", currentTile, 0.25f);
				yield return new WaitForSeconds(0.2f);

				List<WorldTile> nearby = currentTile.neighbours.ToList();
				currentTile = nearby.GetRandom();
			}
		}

		public static IEnumerator ThrowFlame(WorldTile startTile, int jumps, Actor caster)
		{
			for (int i = 0; i < jumps; i++)
			{
				WorldTile currentTarget = World.world.getMouseTilePos();
				if (currentTarget == null) yield break;

				World.world.projectiles.spawn(
						caster,
						null,
						"red_orb",
						caster.cur_transform_position,
						currentTarget.neighbours.GetRandom().posV3,
						0,
						0.25f
				);

				yield return new WaitForSeconds(0.025f);
			}
		}

		public static IEnumerator HoverTileAboveActor(BaseSimObject caster, TileObject tile)
		{
			float hoverSpeed = tile.hoverSpeed;
			float amplitude = tile.hoverAmplitude;
			float phase = tile.hoverOffset;

			while (MainClass.heldTileGroups.ContainsKey(caster) && MainClass.heldTileGroups[caster].Contains(tile))
			{
				float yOffset = Mathf.Sin((Time.time + phase) * hoverSpeed) * amplitude;

				if (MainClass.useMouseInstead)
				{
					tile.tileObj.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + tile.originalOffset + new Vector3(0f, yOffset, 0f);
				}
				else
				{
					tile.tileObj.transform.position = caster.cur_transform_position + tile.originalOffset + new Vector3(0f, yOffset, 0f);
				}

				if (tile.buildingObj != null)
					tile.buildingObj.transform.position = tile.tileObj.transform.position + new Vector3(0f, 0f, -1f);

				if (tile.actorObj != null)
					tile.actorObj.transform.position = tile.tileObj.transform.position + new Vector3(0f, 0f, 1f);

				yield return null;
			}
		}

		public static IEnumerator LaunchTileAtTarget(TileObject tile, Vector3 startPos, Vector3 endPos)
		{
			float duration = 0.75f;
			float elapsed = 0f;																

			while (elapsed < duration)
			{
				float t = elapsed / duration;
				float height = Mathf.Sin(t * Mathf.PI) * 2f;

				Vector3 position = Vector3.Lerp(startPos, endPos, t) + new Vector3(0, height, 0);
				tile.tileObj.transform.position = position;

				if (tile.buildingObj != null)
					tile.buildingObj.transform.position = position + new Vector3(0f, 0f, -1f);

				if (tile.actorObj != null)
					tile.actorObj.transform.position = position + new Vector3(0f, 0f, 1f);

				elapsed += Time.deltaTime;
				yield return null;
			}

			tile.tileObj.transform.position = endPos;
			MainClass.OnTileLanding(tile, endPos);
		}
    }
}
