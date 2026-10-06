using System.Collections.Generic;
using UnityEngine;

namespace SimplyNeedsAName
{
    public class TileObject
    {
        public Vector3 hoverLocalOffset; // Local offset relative to caster

        public GameObject tileObj;
        public SpriteRenderer spriteRenderer;
        public Vector3 posToHoverAt;
        public float hoverAmplitude = 0.1f; // Adjust this value to control the hover intensity
        public float hoverSpeed = 1f; // Adjust this value to control the speed of the hover

        public float hoverOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        public Vector3 originalOffset;

        //tiletypes
        public string tileType = "";
        public string topTileType = "";

        //building
        public string buildingID = "";
        public GameObject buildingObj;
        public SpriteRenderer buildingRenderer;

        //1 actor
        //public string buildingID = "";
        public GameObject actorObj;
        public SpriteRenderer actorRenderer;

        //actors
        public List<Actor> actorsOnTile = new List<Actor>();

        //city for setting zone ownership
        public City citySaved;
        public static List<TileObject> tileObjects = new List<TileObject>();

        public TileObject(WorldTile tileTarget, Vector3 originalOffset)
        {
            tileObj = new GameObject("TileObject");
            tileObj.transform.position = tileTarget.posV3;
            tileObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            spriteRenderer = tileObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerID = -1663423246;
            spriteRenderer.GetComponent<SpriteRenderer>().sortingLayerName = "EffectsTop";

            Sprite tileSprite = MapBox.instance.tilemap.getVariation(tileTarget).sprite;
            spriteRenderer.sprite = tileSprite;
            // Store the original offset
            posToHoverAt = tileTarget.posV3 + originalOffset;

            // Store the original offset
            this.originalOffset = originalOffset;


            //store tileType for changing when "placed" again later
            tileType = tileTarget.main_type.id;
            if (tileTarget.top_type != null)
            {
                topTileType = tileTarget.top_type.id;
            }

            //store building for placing again later
            if (tileTarget.building != null && tileTarget.building.current_tile == tileTarget)
            {
                buildingID = tileTarget.building.asset.id;

                buildingObj = new GameObject("BuildingObject");
                buildingObj.transform.position = tileTarget.posV3 + new Vector3(0f, 0f, -1f);
                buildingObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                //buildingObj.transform.localPosition = ;
                //buildingObj.transform.parent = tileObj.transform;

                buildingRenderer = buildingObj.AddComponent<SpriteRenderer>();
                buildingRenderer.sortingLayerID = -1663423245;
                buildingRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
                buildingRenderer.GetComponent<SpriteRenderer>().sortingLayerName = "EffectsTop";

                buildingRenderer.sprite = tileTarget.building.last_main_sprite;
            }

            //store units for constant moving, have to cancel movement as well in update
            if (tileTarget._units.Count > 0)
            {
                foreach (Actor actor in tileTarget._units)
                {
                    //actor.last_sprite_renderer_enabled = false;
                    actorsOnTile.Add(actor);
                }
                actorObj = new GameObject("ActorObject");
                actorObj.transform.position = tileTarget.posV3 + new Vector3(0f, 0f, 1f);
                actorObj.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                //buildingObj.transform.localPosition = ;
                //actorObj.transform.parent = tileObj.transform;

                actorRenderer = actorObj.AddComponent<SpriteRenderer>();
                actorRenderer.sortingLayerID = -1663423246;
                actorRenderer.GetComponent<SpriteRenderer>().sortingLayerName = "EffectsTop";

                actorRenderer.sprite = tileTarget._units.GetRandom().getSpriteToRender();
                //actorsOnTile = tileTarget._units;
            }

            //decrease and flash tile for "ripping up" effect
            //MapAction.terraformMain(tileTarget, AssetManager.tiles.get("deep_ocean"));
            //if we do this, moved actors "swim" >:c

            //MapAction.decreaseTile(tileTarget); MapAction.decreaseTile(tileTarget); MapAction.decreaseTile(tileTarget);
            MapBox.instance.flash_effects.flashPixel(tileTarget, 20, ColorType.White);

            if (tileTarget.zone.city != null)
            {
                citySaved = tileTarget.zone.city;
                //Debug.Log("City in tileObj set to: " + tileTarget.zone.city.data.id);
            }

            //add to list for updating later
            tileObjects.Add(this);
        }
    }

}
