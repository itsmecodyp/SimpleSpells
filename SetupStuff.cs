using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace SimplyNeedsAName
{
    partial class MainClass : BaseUnityPlugin
    {
        public void HarmonyPatchSetup()
        {
            Harmony harmony = new Harmony(pluginName);
            MethodInfo original;
            MethodInfo patch;
            original = AccessTools.Method(typeof(InitLibraries), "initLibs");
            patch = AccessTools.Method(typeof(MainClass), "initLibs_Postfix");
            harmony.Patch(original, null, new HarmonyMethod(patch));
            Debug.Log(pluginName + ": Harmony patch finished: " + patch.Name);
        }

        public static void initLibs_Postfix()
        {
            SimpleMessages.Messages.AddPreachConversations();
            RegisterAllSpells();
            //SpellSetup();
            Debug.Log("our initLibs ran");
        }
    }
}
