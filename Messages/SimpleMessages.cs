using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleMessages
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public partial class Messages : BaseUnityPlugin {
        public static HarmonyLib.Harmony harmony;

        public void Awake()
        {
            Debug.Log("Messages awake");
            self = this;
            mainColorHex = Config.Bind("Style", "MainColorHex", "808080", "Main background color of the message (hex string, e.g. 808080 or FF0000)");
            borderColorHex = Config.Bind("Style", "BorderColorHex", "FFFFFF", "Border/content color of the message (hex string)");
            fontSize = Config.Bind("Style", "FontSize", 12, "Font size of the message text");
            Debug.Log("Messages awake end");
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
            if (enable)
            {
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
                {
                    showSubMod = true;
                }
            }
        }

        public void OnGUI()
        {
            UIStuff();
            if (isEnabled)
            {
                if (listOfMessages.Count >= 1)
                {
                    for (int i = 0; i < listOfMessages.Count; i++)
                    {
                        ModMessage activeMessage = listOfMessages[i];
                        if (activeMessage.startTime + activeMessage.duration > Time.realtimeSinceStartup)
                        {
                            Actor actor = activeMessage.assignedActor;
                            ActorData data = null;
                            if (actor != null)
                                data = actor.data;
                            if (data != null && actor.isAlive())
                            {
                                Vector2 textDimensions = GUI.skin.window.CalcSize(new GUIContent(activeMessage.MessageText));
                                if (Camera.main != null)
                                {
                                    Vector3 position = Camera.main.WorldToScreenPoint(actor.cur_transform_position);
                                    // adding a random number (3536) to make sure theres no conflict with window id in other mods
                                    Rect window = GUILayout.Window(activeMessage.id + 3536,
                                        new Rect(position.x - (textDimensions.x / 2), Screen.height - position.y - (textDimensions.y * 2), textDimensions.x, textDimensions.y),
                                        ActorMessageDisplayWindow,
                                        "",
                                        new GUIStyle());
                                }
                            }
                        }
                    }
                }
            }
        }

        public static bool isEnabled;

		public static void ActorSay(Actor targetActor, string messageText, float duration = 3f)
        {
            if(Messages.isEnabled == false)
            {
                return;
            }
			if(actorsOnCooldown.Contains(targetActor)) {
                return;
			}
            ModMessage newMessage = new ModMessage
            {
                id = messageID // id starts at 0, random start addition to make sure no conflict with other mod menu ids
            };
            messageID++; // and increments upwards each time
            newMessage.assignedActor = targetActor;
            newMessage.duration = duration;
            newMessage.startTime = Time.realtimeSinceStartup;
            newMessage.MessageText = messageText;
            listOfMessages.Add(newMessage);
            actorsOnCooldown.Add(targetActor);
            IEnumerator coroutine = WaitAndRemoveActorFromCooldown(duration, targetActor);
            self.StartCoroutine(coroutine);
        }

        public static IEnumerator WaitAndRemoveActorFromCooldown(float time, Actor target)
        {
            yield return new WaitForSeconds(time);
			if(actorsOnCooldown.Contains(target)) {
                actorsOnCooldown.Remove(target);
            }
        }

        private Color ParseHexColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length != 6) return Color.white;

            try
            {
                float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
                float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
                float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
                return new Color(r, g, b, 1f);
            }
            catch
            {
                return Color.white;
            }
        }

        public void ActorMessageDisplayWindow(int windowID)
        {
            ModMessage activeMessage = listOfMessages[windowID - 3536]; // removing the conflict number
            if(activeMessage != null) {
                Color originalBg = GUI.backgroundColor;
                Color originalContent = GUI.contentColor;

                GUI.backgroundColor = ParseHexColor(mainColorHex?.Value ?? "808080");
                GUI.contentColor = ParseHexColor(borderColorHex?.Value ?? "FFFFFF");

                GUIStyle newStyle = new GUIStyle(GUI.skin.box);
                newStyle.alignment = TextAnchor.MiddleCenter;
                newStyle.fontSize = fontSize?.Value ?? 12;

                GUILayout.Box(activeMessage.MessageText, newStyle);

                GUI.backgroundColor = originalBg;
                GUI.contentColor = originalContent;
            }
            GUI.DragWindow();
        }

		public static Actor ClosestActorToTile(WorldTile pTarget, Actor excluded = null)
		{
			HashSet<Actor> listToSearch = new HashSet<Actor>();
			if(MapBox.instance.units.visible_units.count > 1)
			{
                foreach (Actor actorToCheck in MapBox.instance.units.visible_units.array)
				{
					if(actorToCheck != excluded)
					{
						listToSearch.Add(actorToCheck);
					}
				}
            }

            Actor tBest = null;
            float tBestDist = int.MaxValue;
            Vector2Int tTargetPos = pTarget.pos;
            foreach (Actor tActor in listToSearch)
            {
                float tDist = Toolbox.DistTile(pTarget, tActor.current_tile);
                if (tDist < tBestDist)
                {
                    tBestDist = tDist;
                    tBest = tActor;
                }
            }
            return tBest;
        }

        public static void AddPreachConversations()
        {
            divineName = NameGenerator.generateNameFromTemplate("orc_clan");

            string preachInput; // easier assignment of responses this way
            string temp; // mostly for emotion, maybe something else

            preachInput = "Have you heard of " + divineName + "?";
            preachStarters.Add(preachInput);
            //these have responses that we can do we coroutine delay, or some other method this time

            temp = emotions.GetRandom();
            preachInput = "Through " + divineName + " we are more in tune with our " + temp + ".";
            preachStarters.Add(preachInput);
            preachStarters.Add("The key to salvation is " + divineName + "'s " + emotions.GetRandom() + "!");
            preachStarters.Add("Ask yourself, what would " + divineName + " do" + "?");
            preachStarters.Add(divineName + " will bring a new world!");

        }
        public static string divineName;
        public static List<string> preachStarters = new List<string>();
        public static List<string> emotions = new List<string>() { "anger", "bloodshed", "insanity", "rage", "fury", "wrath", "hate", "love", "joy" };
    }
}