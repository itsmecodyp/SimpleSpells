using BepInEx;
using System.Collections.Generic;
using UnityEngine;
using static SimplyNeedsAName.MainClass.SpriteMovement;

namespace SimplyNeedsAName
{
	partial class MainClass : BaseUnityPlugin
	{
		public static GameObject pokeballPrefab;
        public static List<SpriteMovement> activePokeballMovements = new List<SpriteMovement>();

		public static bool usePokeball = true;
		public void TestItemControl()
		{
			if (Input.GetKeyDown(KeyCode.P))
			{
				usePokeball = !usePokeball;
			}
			if (Input.GetKeyDown(KeyCode.O))
			{
				Actor closest = ClosestActorToTile(World.world.getMouseTilePos(), 2f, controlledActorSc);
				if (closest != null)
				{
					bool hasWeapon = closest.equipment?.weapon?.isEmpty() == false;
					if (hasWeapon)
					{
						//Actor actor = TurnItemBackIntoUnit(closest, VeryRandomNeighborTile(closest.current_tile));
						SpawnPokeball(null, closest, PokeballActionType.Release, VeryRandomNeighborTile(closest.current_tile).posV3);

					}
					else
					{
						if (usePokeball)
						{
							SpawnPokeball(ClosestActorToTile(closest.current_tile, 15f, closest), closest, PokeballActionType.Capture);
						}
						else
						{
							TurnUnitIntoItem(ClosestActorToTile(closest.current_tile, 15f, closest), closest, Randy.randomBool());
						}
					}
				}
			}
		}


		public static void SpawnPokeball(Actor targetActor, Actor actorWhoEquipsItem, PokeballActionType actionType, Vector3? overrideTargetPos = null)
		{
			if (pokeballPrefab == null)
			{
				pokeballPrefab = new GameObject("Pokeball");
				pokeballPrefab.AddComponent<SpriteRenderer>();
				SpriteRenderer renderer = pokeballPrefab.GetComponent<SpriteRenderer>();
				renderer.sprite = (Sprite)Resources.Load("ui/Icons/iconMoodHappy", typeof(Sprite));
				renderer.sortingLayerName = "EffectsTop";

				pokeballPrefab.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
				pokeballPrefab.SetActive(false);
			}

			Vector3 startPos = actorWhoEquipsItem.current_tile.posV3;
			Vector3 endPos = overrideTargetPos ?? targetActor.current_tile.posV3;


			GameObject pokeball = Instantiate(pokeballPrefab, actorWhoEquipsItem.current_tile.posV3, Quaternion.identity);
			pokeball.SetActive(true);
			SpriteRenderer spriteRenderer = pokeball.GetComponent<SpriteRenderer>();
			spriteRenderer.color = Color.black;

			activePokeballMovements.Add(new SpriteMovement(
		pokeball.transform,
		startPos,
		endPos,
		1.0f,
		targetActor,
		actorWhoEquipsItem,
		actionType
	));

		}

		private void UpdatePokeballs()
		{
			for (int i = activePokeballMovements.Count - 1; i >= 0; i--)
			{
				SpriteMovement movement = activePokeballMovements[i];
				movement.elapsedTime += Time.deltaTime;
				float t = movement.elapsedTime * movement.speed;
				if (t > 1.0f) t = 1.0f;
				Vector3 midPoint = Vector3.Lerp(movement.startPos, movement.targetPos, 0.5f);
				midPoint.y += 1.5f; // Add height to the arc

				Vector3 a = Vector3.Lerp(movement.startPos, midPoint, t);
				Vector3 b = Vector3.Lerp(midPoint, movement.targetPos, t);
				Vector3 pos = Vector3.Lerp(a, b, t);

				movement.transform.position = pos;
				if (t >= 1.0f)
				{
					if (movement.actionType == PokeballActionType.Capture)
					{
						if (movement.targetedActor != null)
						{
							TurnUnitIntoItem(movement.targetedActor, movement.actorWhoEquips, false);
						}
					}
					else if (movement.actionType == PokeballActionType.Release)
					{
						
							TurnItemBackIntoUnitInternal(movement.actorWhoEquips, World.world.GetTile((int)pos.x, (int)pos.y));
					}
					Destroy(movement.transform.gameObject);
					activePokeballMovements.RemoveAt(i);
				}

			}
		}

		private static Actor TurnItemBackIntoUnitInternal(Actor actorHoldingItem, WorldTile targetTile)
		{
			// The full code of your current TurnItemBackIntoUnit goes here,
			// but REMOVE the item fetch and `throwOutItem()` call, since it's already done
			if (actorHoldingItem.equipment?.weapon?.getItem() != null)
			{
				Item itemToConvert = actorHoldingItem.equipment?.weapon?.getItem();
				// Check if this is actually a unit item
				if (!itemToConvert.data.hasFlag("isUnit"))
				{
					Debug.LogError("Attempted to convert non-unit item back to unit");
					return null;
				}


				itemToConvert.data.get("actor_stats_id", out string statsID);
				Actor newActor = MapBox.instance.units.createNewUnit(statsID, targetTile, pAdultAge: true);

				if (newActor != null)
				{
					// Restore basic data
					string actorName;
					itemToConvert.data.get("actor_name", out actorName);
					newActor.data.name = actorName;

					int sexInt;
					itemToConvert.data.get("actor_sex", out sexInt);
					newActor.data.sex = (ActorSex)sexInt;

					int level;
					itemToConvert.data.get("actor_level", out level);
					newActor.data.level = level;

					int kills;
					itemToConvert.data.get("actor_kills", out kills);
					newActor.data.kills = kills;

					int experience;
					itemToConvert.data.get("actor_experience", out experience);
					newActor.data.experience = experience;

					int head;
					itemToConvert.data.get("actor_head", out head);
					newActor.data.head = head;

					if (itemToConvert.data.hasFlag("hasSubspecies"))
					{
						itemToConvert.data.get("subspeciesID", out string species_id);
						if (string.IsNullOrEmpty(species_id) == false)
						{
							foreach (Subspecies species in World.world.subspecies.list)
							{
								if (species.data.name == species_id)
								{
									newActor.setSubspecies(species);
									itemToConvert.data.get("skinID", out int skin_id);
									newActor.subspecies.data.skin_id = skin_id;
								}
							}
						}
					}



					// Restore cityID and kingdom ID
					long cityID;
					itemToConvert.data.get("cityID", out cityID);
					newActor.data.cityID = cityID;

					itemToConvert.data.get("actor_phenotype", out int phenotype);
					newActor.data.phenotype_index = phenotype;
                    itemToConvert.data.get("actor_phenotypeShade", out int phenotypeShade);
                    newActor.data.phenotype_shade = phenotypeShade;


                    long kingdomID;
					itemToConvert.data.get("civ_kingdom_id", out kingdomID);
					newActor.data.civ_kingdom_id = kingdomID;
					itemToConvert.data.get("actor_traits", out string traitsStr);
					if (!string.IsNullOrEmpty(traitsStr))
					{
						string[] traits = traitsStr.Split(',');
						foreach (string trait in traits)
						{
							if (!string.IsNullOrEmpty(trait))
							{
								newActor.addTrait(trait);
							}
						}
					}

					newActor.data.custom_data_bool = new CustomDataContainer<bool>();
					newActor.data.custom_data_float = new CustomDataContainer<float>();
					newActor.data.custom_data_int = new CustomDataContainer<int>();
					newActor.data.custom_data_flags = new HashSet<string>();
					newActor.data.custom_data_long = new CustomDataContainer<long>();
					newActor.data.custom_data_string = new CustomDataContainer<string>();


					// Restore custom data - we need to iterate through all the item's data
					if (itemToConvert.data.custom_data_string != null)
					{
						foreach (var key in new List<string>(itemToConvert.data.custom_data_string.Keys))
						{
							if (key.StartsWith("actor_custom_str_"))
							{
								string actualKey = key.Substring("actor_custom_str_".Length);
								string value;
								itemToConvert.data.get(key, out value);
								newActor.data.custom_data_string[actualKey] = value;
							}
						}
					}

					if (itemToConvert.data.custom_data_int != null)
					{
						foreach (var key in new List<string>(itemToConvert.data.custom_data_int.Keys))
						{
							if (key.StartsWith("actor_custom_int_"))
							{
								string actualKey = key.Substring("actor_custom_int_".Length);
								itemToConvert.data.get(key, out int value);
								newActor.data.custom_data_int[actualKey] = value;
							}
							else if (key.StartsWith("actor_custom_bool_"))
							{
								string actualKey = key.Substring("actor_custom_bool_".Length);
								int value;
								itemToConvert.data.get(key, out value);
								newActor.data.custom_data_bool[actualKey] = value == 1;
							}
						}
					}

					if (itemToConvert.data.custom_data_float != null)
					{
						foreach (var key in new List<string>(itemToConvert.data.custom_data_float.Keys))
						{
							if (key.StartsWith("actor_custom_float_"))
							{
								string actualKey = key.Substring("actor_custom_float_".Length);
								float value;
								itemToConvert.data.get(key, out value);
								newActor.data.custom_data_float[actualKey] = value;
							}

						}
					}

				}

				if (newActor != null)
				{
					// Make sure the actor is ready to go
					newActor.setStatsDirty();
					newActor.restoreHealth(10 ^ 9); // Should be 10^9 (1 billion), check if this should be Math.Pow(10, 9) instead
				}
				actorHoldingItem.equipment.weapon.takeAwayItem();
				return newActor;
			}
			else
			{
				Debug.Log("cant turn item back into unit if unit has no item");
				return null;
			}
		}

		public class SpriteMovement
		{
			public Transform transform;
			public Vector3 startPos;
			public Vector3 targetPos;
			public float elapsedTime;
			public float speed;
			public Actor targetedActor;
			public Actor actorWhoEquips;
			public enum PokeballActionType
			{
				Capture, // going in
				Release  // going out
			}


			public PokeballActionType actionType;

			public SpriteMovement(Transform _transform, Vector3 _startPos, Vector3 _targetPos, float _speed,
					Actor targetActor, Actor whoEquips, PokeballActionType _actionType, Item item = null)
			{
				transform = _transform;
				startPos = _startPos;
				targetPos = _targetPos;
				elapsedTime = 0f;
				speed = _speed;
				targetedActor = targetActor;
				actorWhoEquips = whoEquips;
				actionType = _actionType;
			}

		}

		public static Actor ClosestActorToTile(WorldTile pTarget, float range, Actor excludingActor = null)
		{
			Actor returnActor = null;
			foreach (Actor actorToCheck in MapBox.instance.units)
			{
				float actorDistanceFromTile = Toolbox.Dist(actorToCheck.cur_transform_position.x, actorToCheck.cur_transform_position.y, pTarget.pos.x, pTarget.pos.y);
				if (actorDistanceFromTile < range && actorToCheck != excludingActor)
				{
					range = actorDistanceFromTile;
					returnActor = actorToCheck;
				}
			}
			if (returnActor == null)
			{
				Debug.Log("return actor for closestActor is null");
			}
			return returnActor;
		}

		public static void TurnUnitIntoItem(Actor targetActor, Actor actorWhoEquipsTheNewItem, bool directToStorage = false)
		{
			UnitData copiedData = CopyUnit(targetActor);
			Item pData = World.world.items.generateItem(AssetManager.items.get("stick_wood"));
			pData.data.addFlag("isUnit");
			pData.data.set("cityID", copiedData.data.cityID);
			pData.data.set("civ_kingdom_id", copiedData.data.civ_kingdom_id);
			pData.data.set("originalActorID", targetActor.asset.id);
			pData.data.set("actor_name", targetActor.data.name);
			pData.data.set("actor_sex", (int)targetActor.data.sex);
			pData.data.set("actor_stats_id", targetActor.data.asset_id);
			pData.data.set("actor_level", targetActor.data.level);
			pData.data.set("actor_kills", targetActor.data.kills);
			pData.data.set("actor_experience", targetActor.data.experience);
			pData.data.set("actor_x", targetActor.current_tile.pos.x);
			pData.data.set("actor_y", targetActor.current_tile.pos.y);
			pData.data.set("actor_head", targetActor.data.head);
			pData.data.set("actor_phenotype", targetActor.data.phenotype_index);
            pData.data.set("actor_phenotypeShade", targetActor.data.phenotype_shade);

            if (targetActor.data.saved_traits != null && targetActor.data.saved_traits.Count > 0)
			{
				pData.data.set("actor_traits", string.Join(",", targetActor.data.saved_traits));
			}

			// Store custom data in a special format to separate from item's own custom data
			if (targetActor.data.custom_data_string != null)
			{
				foreach (var pair in targetActor.data.custom_data_string.dict)
				{
					pData.data.set("actor_custom_str_" + pair.Key, pair.Value);
				}
			}
			if (targetActor.data.custom_data_int != null)
			{
				foreach (var pair in targetActor.data.custom_data_int.dict)
				{
					pData.data.set("actor_custom_int_" + pair.Key, pair.Value);
				}
			}
			if (targetActor.data.custom_data_float != null)
			{
				foreach (var pair in targetActor.data.custom_data_float.dict)
				{
					pData.data.set("actor_custom_float_" + pair.Key, pair.Value);
				}
			}
			if (targetActor.data.custom_data_bool != null)
			{
				foreach (var pair in targetActor.data.custom_data_bool.dict)
				{
					pData.data.set("actor_custom_bool_" + pair.Key, pair.Value ? 1 : 0);
				}
			}

			if(targetActor.hasSubspecies())
			{
				pData.data.addFlag("hasSubspecies");
				pData.data.set("subspeciesID", targetActor.subspecies.data.name);
				pData.data.set("skinID", targetActor.subspecies.data.skin_id);
			}


			pData.data.favorite = true;
			pData.data.name = targetActor.getName() + "'s soul stick";
			pData.data.created_time = targetActor.data.created_time;
																															
			if (directToStorage)
			{
				if (actorWhoEquipsTheNewItem.city != null)
				{
					actorWhoEquipsTheNewItem.city.tryToPutItemInStorage(pData);
					/*
					{
						Debug.Log("city failed to put item into storage for some reason, giving to unit directly");
						if (actorWhoEquipsTheNewItem.equipment == null)
						{
							Debug.LogError("Actor equipment is null, creating so items can be given");
							actorWhoEquipsTheNewItem.equipment = new ActorEquipment();
						}
						actorWhoEquipsTheNewItem.equipment.getSlot(EquipmentType.Weapon).setItem(pData, actorWhoEquipsTheNewItem);
					}
					else
					{
					*/
						Debug.Log("city obtained new transformed unit-item");
					//}
				}
			}
			else
			{
				if (actorWhoEquipsTheNewItem.equipment == null)
				{
					Debug.LogError("Actor equipment is null, creating so items can be given");
					actorWhoEquipsTheNewItem.equipment = new ActorEquipment();
				}
				actorWhoEquipsTheNewItem.equipment.getSlot(EquipmentType.Weapon).setItem(pData, actorWhoEquipsTheNewItem);
			}
			targetActor.die(true, AttackType.Metamorphosis, false);
		}

		public static Actor TurnItemBackIntoUnit(Actor actorHoldingItem, WorldTile targetTile)
		{
			if (actorHoldingItem.equipment?.weapon?.getItem() != null)
			{
				Item itemToConvert = actorHoldingItem.equipment?.weapon?.getItem();
				// Check if this is actually a unit item
				if (!itemToConvert.data.hasFlag("isUnit"))
				{
					Debug.LogError("Attempted to convert non-unit item back to unit");
					return null;
				}


				itemToConvert.data.get("actor_stats_id", out string statsID);
				Actor newActor = MapBox.instance.units.createNewUnit(statsID, targetTile, pAdultAge: true);

				if (newActor != null)
				{
					// Restore basic data
					string actorName;
					itemToConvert.data.get("actor_name", out actorName);
					newActor.data.name = actorName;

					int sexInt;
					itemToConvert.data.get("actor_sex", out sexInt);
					newActor.data.sex = (ActorSex)sexInt;

					int level;
					itemToConvert.data.get("actor_level", out level);
					newActor.data.level = level;

					int kills;
					itemToConvert.data.get("actor_kills", out kills);
					newActor.data.kills = kills;

					int experience;
					itemToConvert.data.get("actor_experience", out experience);
					newActor.data.experience = experience;

					int head;
					itemToConvert.data.get("actor_head", out head);
					newActor.data.head = head;

					if(itemToConvert.data.hasFlag("hasSubspecies"))
					{
						itemToConvert.data.get("subspeciesID", out string species_id);
						if(string.IsNullOrEmpty(species_id) == false)
						{
										foreach(Subspecies species in World.world.subspecies.list)
							{
								if(species.data.name == species_id)
								{
									newActor.setSubspecies(species);
									itemToConvert.data.get("skinID", out int skin_id);
									newActor.subspecies.data.skin_id = skin_id;
								}
							}
						}
					}



					// Restore cityID and kingdom ID
					long cityID;
					itemToConvert.data.get("cityID", out cityID);
					newActor.data.cityID = cityID;

					itemToConvert.data.get("actor_phenotype", out int phenotype);
					newActor.data.phenotype_index = phenotype;
                    itemToConvert.data.get("actor_phenotypeShade", out int phenotypeShade);
                    newActor.data.phenotype_shade = phenotypeShade;

                    long kingdomID;
					itemToConvert.data.get("civ_kingdom_id", out kingdomID);
					newActor.data.civ_kingdom_id = kingdomID;
					itemToConvert.data.get("actor_traits", out string traitsStr);
					if (!string.IsNullOrEmpty(traitsStr))
					{
						string[] traits = traitsStr.Split(',');
						foreach (string trait in traits)
						{
							if (!string.IsNullOrEmpty(trait))
							{
								newActor.addTrait(trait);
							}
						}
					}

					newActor.data.custom_data_bool = new CustomDataContainer<bool>();
					newActor.data.custom_data_float = new CustomDataContainer<float>();
					newActor.data.custom_data_int = new CustomDataContainer<int>();
					newActor.data.custom_data_flags = new HashSet<string>();
					newActor.data.custom_data_long = new CustomDataContainer<long>();
					newActor.data.custom_data_string = new CustomDataContainer<string>();


					// Restore custom data - we need to iterate through all the item's data
					if (itemToConvert.data.custom_data_string != null)
					{
						foreach (var key in new List<string>(itemToConvert.data.custom_data_string.Keys))
						{
							if (key.StartsWith("actor_custom_str_"))
							{
								string actualKey = key.Substring("actor_custom_str_".Length);
								string value;
								itemToConvert.data.get(key, out value);
								newActor.data.custom_data_string[actualKey] = value;
							}
						}
					}

					if (itemToConvert.data.custom_data_int != null)
					{
						foreach (var key in new List<string>(itemToConvert.data.custom_data_int.Keys))
						{
							if (key.StartsWith("actor_custom_int_"))
							{
								string actualKey = key.Substring("actor_custom_int_".Length);
								itemToConvert.data.get(key, out int value);
								newActor.data.custom_data_int[actualKey] = value;
							}
							else if (key.StartsWith("actor_custom_bool_"))
							{
								string actualKey = key.Substring("actor_custom_bool_".Length);
								int value;
								itemToConvert.data.get(key, out value);
								newActor.data.custom_data_bool[actualKey] = value == 1;
							}
						}
					}

					if (itemToConvert.data.custom_data_float != null)
					{
						foreach (var key in new List<string>(itemToConvert.data.custom_data_float.Keys))
						{
							if (key.StartsWith("actor_custom_float_"))
							{
								string actualKey = key.Substring("actor_custom_float_".Length);
								float value;
								itemToConvert.data.get(key, out value);
								newActor.data.custom_data_float[actualKey] = value;
							}

						}
					}
								 
				}

				if (newActor != null)
				{
					// Make sure the actor is ready to go
					newActor.setStatsDirty();
					newActor.restoreHealth(10 ^ 9); // Should be 10^9 (1 billion), check if this should be Math.Pow(10, 9) instead
				}
				actorHoldingItem.equipment.weapon.takeAwayItem();
				return newActor;
			}
			else
			{
				Debug.Log("cant turn item back into unit if unit has no item");
				return null;
			}
		}
		public static UnitData CopyUnit(Actor targetActor, bool isForResize = false)
		{
			if (targetActor != null && targetActor.data != null)
			{
				ActorData data = targetActor.data;
				if(data != null)
				{
					if (data.custom_data_bool == null) data.custom_data_bool = new CustomDataContainer<bool>();
					if (data.custom_data_float == null) data.custom_data_float = new CustomDataContainer<float>();
					if (data.custom_data_int == null) data.custom_data_int = new CustomDataContainer<int>();
					if (data.custom_data_flags == null) data.custom_data_flags = new HashSet<string>();
					if (data.custom_data_long == null) data.custom_data_long = new CustomDataContainer<long>();
					if (data.custom_data_bool.dict == null) data.custom_data_bool.dict = new Dictionary<string, bool>();
					if (data.custom_data_float.dict == null) data.custom_data_float.dict = new Dictionary<string, float>();
					if (data.custom_data_int.dict == null) data.custom_data_int.dict = new Dictionary<string, int>();
					if (data.custom_data_long.dict == null) data.custom_data_long.dict = new Dictionary<string, long>();

				}
				targetActor.prepareForSave();
				UnitData newSavedUnit = new UnitData();
				foreach (string trait in data.saved_traits)
				{
					newSavedUnit.traits.Add(trait);
				}
				newSavedUnit.statsID = targetActor.asset.id;
				newSavedUnit.equipment = targetActor.equipment;

				ActorData data0 = new ActorData
				{
					saved_traits = data.saved_traits,
					created_time = data.created_time,
					culture = data.culture,
					experience = data.experience,
					name = data.name,
					sex = data.sex,
					head = data.head,
					homeBuildingID = data.homeBuildingID,
					nutrition = data.nutrition,
					kills = data.kills,
					level = data.level,
					profession = data.profession,
					phenotype_index = data.phenotype_index,
                    phenotype_shade = data.phenotype_shade,
                    asset_id = data.asset_id,
					inventory = data.inventory,
					clan = data.clan
				};
				newSavedUnit.customData = targetActor.data;
				newSavedUnit.data = data0;
				unitClipboardDict.Add(unitClipboardDictNum.ToString(), newSavedUnit);
				newSavedUnit.oldPos = targetActor.current_position;
				if (actorPositionsOnMap.ContainsKey(targetActor.current_tile.pos) == false)
				{
					actorPositionsOnMap.Add(targetActor.current_tile.pos, unitClipboardDictNum);
				}
				newSavedUnit.isForResize = isForResize;
				unitClipboardDictNum++;
				selectedUnitToPaste = newSavedUnit;
				return newSavedUnit;
			}
			return null;
		}
		public static UnitData selectedUnitToPaste;
		public static Dictionary<Vector2Int, int> actorPositionsOnMap = new Dictionary<Vector2Int, int>();
		public static int unitClipboardDictNum;
		public static Dictionary<string, UnitData> unitClipboardDict = new Dictionary<string, UnitData>(); // id, data

		public class UnitData
		{
			public string statsID = "";
			public List<string> traits = new List<string>();
			public ActorEquipment equipment;
			public ActorData data;
			public BaseObjectData customData;
			public bool isForResize;
			public Vector2 oldPos;
		}

	}
}
