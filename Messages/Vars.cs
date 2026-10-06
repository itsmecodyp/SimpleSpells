using BepInEx;
using System;
using System.Collections.Generic;
using UnityEngine;

using BepInEx.Configuration;

namespace SimpleMessages
{
	public partial class Messages : BaseUnityPlugin
	{
		public static ConfigEntry<string> mainColorHex;
		public static ConfigEntry<string> borderColorHex;
		public static ConfigEntry<int> fontSize;
		public static float timerToWait = 10f;

		public static bool enable = true;
		public static bool showMenu;
		public static Rect menuRect;

		public const string pluginGuid = "cody.worldbox.simple.messagesmini";
		public const string pluginName = "SimpleMessages";
		public const string pluginVersion = "0.0.0.1";
		public static int windowInUse = 0;
		public static List<ModMessage> listOfMessages = new List<ModMessage>();
		public static int messageID;
		public float lastTimer;
		public static Messages self;

		public static List<Actor> actorsOnCooldown = new List<Actor>();
		//change to list or dict of multiple conversations?

		public bool showSubMod = true;

		[Serializable]
		public class ModMessage
		{
			public int id;
			public Actor assignedActor;
			public string TitleText = "";
			public string MessageText = "";
			public float duration = 3.0f;
			public float startTime;
		}
	}
}
