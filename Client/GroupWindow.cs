using System;
using UnityEngine;

namespace DarkMultiPlayer
{
    public class GroupWindow
    {
        private static GroupWindow singleton;
        public bool workerEnabled = false;
        public bool display = false;
        private bool initialized = false;
        private bool isWindowLocked = false;
        //GUI
        private GUIStyle windowStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUILayoutOption[] layoutOptions;
        private Rect windowRect;
        private Rect moveRect;
        private const float WINDOW_HEIGHT = 400;
        private const float WINDOW_WIDTH = 300;

        public static GroupWindow fetch
        {
            get
            {
                return singleton;
            }
        }

        private void Update()
        {
            if (workerEnabled)
            {

            }
        }

        private void Draw()
        {
            if (display)
            {
                if (!initialized)
                {
                    initialized = true;
                    InitGUI();
                }
                windowRect = DMPGuiUtil.PreventOffscreenWindow(GUILayout.Window(6714 + Client.WINDOW_OFFSET, windowRect, DrawContent, "DarkMultiPlayer - Group", windowStyle, layoutOptions));
            }
        }

        private void InitGUI()
        {
            //Setup GUI stuff
            windowRect = new Rect(Screen.width * 0.5f - WINDOW_WIDTH, Screen.height / 2f - WINDOW_HEIGHT / 2f, WINDOW_WIDTH, WINDOW_HEIGHT);
            moveRect = new Rect(0, 0, 10000, 20);
            windowStyle = new GUIStyle(GUI.skin.window);
            labelStyle = new GUIStyle(GUI.skin.label);
            buttonStyle = new GUIStyle(GUI.skin.button);
            layoutOptions = new GUILayoutOption[4];
            layoutOptions[0] = GUILayout.MinWidth(WINDOW_WIDTH);
            layoutOptions[1] = GUILayout.MaxWidth(WINDOW_WIDTH);
            layoutOptions[2] = GUILayout.MinHeight(WINDOW_HEIGHT);
            layoutOptions[3] = GUILayout.MaxHeight(WINDOW_HEIGHT);
        }

        private void DrawContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            //TODO:
            GUILayout.Label("Put some stuff in here plox.", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("YES", buttonStyle))
            {
            }
            if (GUILayout.Button("NO", buttonStyle))
            {
            }
            if (GUILayout.Button("FILE_NOT_FOUND", buttonStyle))
            {
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void CheckWindowLock()
        {
            if (!Client.fetch.gameRunning)
            {
                RemoveWindowLock();
                return;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                RemoveWindowLock();
                return;
            }

            if (display)
            {
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                bool shouldLock = windowRect.Contains(mousePos);

                if (shouldLock && !isWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS,  "DMP_GroupWindowLock");
                    isWindowLocked = true;
                }
                if (!shouldLock && isWindowLocked)
                {
                    RemoveWindowLock();
                }
            }

            if (!display && isWindowLocked)
            {
                RemoveWindowLock();
            }
        }

        private void RemoveWindowLock()
        {
            if (isWindowLocked)
            {
                isWindowLocked = false;
                InputLockManager.RemoveControlLock("DMP_GroupWindowLock");
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                    Client.drawEvent.Remove(singleton.Draw);
                    Client.updateEvent.Remove(singleton.Update);
                }
                singleton = new GroupWindow();
                Client.drawEvent.Add(singleton.Draw);
                Client.updateEvent.Add(singleton.Update);
            }
        }
    }
}

