#define CodyBuild

using BepInEx;
using UnityEngine;

namespace SimpleMessages
{

	public partial class Messages : BaseUnityPlugin
	{
		public void UIStuff()
		{
			if (showSubMod)
			{
				if (GUI.Button(new Rect(Screen.width - 120, 160, 95, 20), "Messages"))
					showMenu = !showMenu;

				if (GUI.Button(new Rect(Screen.width - 25, 160, 25, 20), "x"))
					showSubMod = false;
			}
            //GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(GuiMain.scaleFactor, GuiMain.scaleFactor, 1));
            if (showMenu)
			{
				menuRect = GUILayout.Window(9675309, menuRect, MainMessageWindow, "Messages", GUILayout.MaxWidth(200f), GUILayout.MinWidth(200f));
			}
            GUI.matrix = Matrix4x4.identity;
        }

        private void MainMessageWindow(int windowID)
		{
			Color or = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (isEnabled == true)
            {
                GUI.backgroundColor = Color.green;
            }
			else
			{
                GUI.backgroundColor = Color.red;
            }
            if (GUILayout.Button("Enable messages"))
                isEnabled = !isEnabled;
			GUI.backgroundColor = or;
			GUI.DragWindow();
		}
	}
}
