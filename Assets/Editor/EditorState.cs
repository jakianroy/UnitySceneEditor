using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace QDazzle
{
    public class EditorState
    {
        public enum Action
        {
            None,
            Move,
            Scale,
            Rotate,
        }

        public enum Pivot
        {
            Center,
            LeftTop,
            RightTop,
            RightBottom,
            LeftBottom,
        }

       
        public Vector2 start;
        public Vector2 now;
        public Vector2 offset;
        public Pivot pivot = Pivot.Center;
        public Action action = Action.None;
        public bool press = false;
        private bool inDraging = false;
      

        public Vector2 panOffset = Vector2.zero;

        public float zoom = 2;
        public float gridStep = 1024 / 16.0f;
        public int id = 0;
        public Rect selectRect = new Rect();
        public bool panWindow = false;
        public bool selectGrid = false;
        public float toolBarWidth = 0;
        public List<Type> eventTypeList = new List<Type>();
        public Vector2 zoomPos { get { return canvasRect.size / 2; } }
        public Rect canvasRect;
        public Rect drawRect
        {
            get { return editor.GetDrawRect(); }
        }
        public SubEditor editor;
       

        public void Update()
        {
            editor.Update();
        }

        Texture2D tex;

        public void Draw()
        {
            EditorUtil.MaxRect = canvasRect;
            HandleCursor();
            DrawGird();
            
            editor.Draw();
            DrawSelectGrid();
            DrawShadow();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            editor.OnSceneGUI(sceneView);
        }

        void DrawSelectGrid()
        {
            EditorUtil.CreateLineMaterial();
            EditorUtil.lineMaterial.SetPass(0); 
            GL.Begin(GL.QUADS);
            EditorUtil.DrawQuad(selectRect, new Color(153.0f / 255, 204.0f/ 255, 1, 0.3f));
            GL.End();
        }

        public void DrawShadow()
        {
            Texture2D tex = EditorImage.GetImage(EditorImage.ImageType.SHADOW);

            Rect shadowRect = new Rect(canvasRect.width - tex.width / 4 , 0, tex.width / 4, canvasRect.height);
            GUI.DrawTextureWithTexCoords(shadowRect, tex, EditorImage.ShadowRect[EditorImage.ShadowDirect.RIGHT], true);// 2, canvasRect.height / tex.height / 2), true);

            shadowRect.x = 0;
            GUI.DrawTextureWithTexCoords(shadowRect, tex, EditorImage.ShadowRect[EditorImage.ShadowDirect.LEFT], true);

            shadowRect.width = canvasRect.width;
            shadowRect.height = tex.height / 4;
            shadowRect.x = 0;
            shadowRect.y = 0;

            GUI.DrawTextureWithTexCoords(shadowRect, tex, EditorImage.ShadowRect[EditorImage.ShadowDirect.TOP], true);
            shadowRect.y = canvasRect.height - tex.height / 4;
            GUI.DrawTextureWithTexCoords(shadowRect, tex, EditorImage.ShadowRect[EditorImage.ShadowDirect.BOTTOM], true);

        }
        public void DrawGird()
        {
            EditorUtil.CreateLineMaterial();
            EditorUtil.lineMaterial.SetPass(0);

            GL.Begin(GL.QUADS);
            GL.Color(new Color(102.0f / 255, 102.0f / 255, 102.0f / 255));
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(canvasRect.width, 0, 0);
            GL.Vertex3(canvasRect.width, canvasRect.height, 0);
            GL.Vertex3(0, canvasRect.height, 0);

            Rect rect = drawRect;
            GL.Color(new Color(116.0f / 255, 116.0f / 255, 116.0f / 255));
            GL.Vertex3(rect.x, Mathf.Max(0, rect.y), 0);
            GL.Vertex3(rect.x + rect.width, Mathf.Max(rect.y, 0), 0);
            GL.Vertex3(rect.x + rect.width, Mathf.Max(rect.y + rect.height, 0), 0);
            GL.Vertex3(rect.x, Mathf.Max(rect.y + rect.height, 0), 0);

            GL.End();
            GL.Begin(GL.LINES);



            GL.Color(EditorColor.GridLineColor);

            float step = gridStep;

            float offsetX = (int)(Mathf.Max(panOffset.x / zoom / step, 0) + 1) * step;
            float offsetY = (int)(Mathf.Max(panOffset.y / zoom / step, 0) + 1) * step;
            float x = panOffset.x / zoom - offsetX;
            while (true)
            {
                GL.Vertex3(x, 0, 0);
                GL.Vertex3(x, canvasRect.height, 0);
                x += step;
                if (x > canvasRect.width)
                    break;
            }
            float y = panOffset.y / zoom - offsetY;
            while (y < 0)
            {
                y += step;
            }
            while (true)
            {
                GL.Vertex3(0, y, 0);
                GL.Vertex3(canvasRect.width, y, 0);
                y += step;
                if (y > canvasRect.height)
                    break;
            }

            GL.Color(Color.blue);
            if (rect.y >= 0)
            {
                GL.Vertex3(rect.x, Mathf.Max(0, rect.y), 0);
                GL.Vertex3(rect.x + rect.width, Mathf.Max(0, rect.y), 0);
            }
            GL.Vertex3(rect.x + rect.width, Mathf.Max(0, rect.y), 0);
            GL.Vertex3(rect.x + rect.width, Mathf.Max(0, rect.y + rect.height), 0);
            if (rect.y + rect.height >= 0)
            {
                GL.Vertex3(rect.x + rect.width, Mathf.Max(0, rect.y + rect.height), 0);
                GL.Vertex3(rect.x, Mathf.Max(0, rect.y + rect.height), 0);
            }

            GL.Vertex3(rect.x, Mathf.Max(0, rect.y + rect.height), 0);
            GL.Vertex3(rect.x, Mathf.Max(0, rect.y), 0);

            GL.End();
        }

        public void OnGUI()
        {
            editor.OnGUI();
            GUILayout.Label(Event.current.mousePosition + "");
            //GUILayout.Label(Event.current.button + "");
        }
        
        public string dragUserID;
        public Vector2 dragMouseStart;
        public Vector2 dragObjectStart;
        public Vector2 dragOffset;
       
        public Vector2 dragObjectPos { get { return dragObjectStart + dragOffset; } }

        private void HandleCursor()
        {
            if(inDraging)
            {
                EditorGUIUtility.AddCursorRect(canvasRect, MouseCursor.Pan);
            }
        }

        public bool StartDrag(string userID, Vector2 mousePos, Vector2 objectPos)
        {
            if (!String.IsNullOrEmpty(dragUserID) && dragUserID != userID)
                return false;
            dragUserID = userID;
            dragMouseStart = mousePos;
            dragObjectStart = objectPos;
            dragOffset = Vector2.zero;
            inDraging = true;
            return true;

        }

        public void RegisterEventType(Type t)
        {
            if(!eventTypeList.Contains(t))
            {
                eventTypeList.Add(t);
            }
        }

        public void UnRegisterEventType(Type t)
        {
            if (eventTypeList.Contains(t))
            {
                eventTypeList.Remove(t);
            }
        }

        public void Select(Rect rect)
        {
            editor.Select(rect);
        }

        public Vector2 UpdateDrag(string userID, Vector2 newDragPos)
        {
            if (dragUserID != userID)
                throw new UnityException("User ID " + userID + " tries to interrupt drag from " + dragUserID);
            Vector2 prevOffset = dragOffset;
            dragOffset = (newDragPos - dragMouseStart) * zoom;
            return dragOffset - prevOffset;
        }

        public Vector2 EndDrag(string userID)
        {
            if (dragUserID != userID)
                throw new UnityException("User ID " + userID + " tries to end drag from " + dragUserID);
            Vector2 dragPos = dragObjectPos;
            dragUserID = "";
            dragOffset = dragMouseStart = dragObjectStart = Vector2.zero;
            inDraging = false;
            return dragPos;

        }

        [EventHandlerAttribute(EventType.MouseDown, 105)]
        private static void HandleWindowPanningStart(EditorInputInfo inputInfo)
        {
            EditorState state = inputInfo.editorState;
            if ( inputInfo.inputEvent.button == 2 && state.action == EditorState.Action.None)
            {
                state.panWindow = true;
                state.StartDrag("window", inputInfo.inputPos, state.panOffset);
            }

            if (inputInfo.inputEvent.button == 0 && state.action == EditorState.Action.None)
            {
                state.selectGrid = true;
                state.dragMouseStart = inputInfo.inputPos;
                state.dragOffset = Vector2.zero;
            }
        }

        [EventHandlerAttribute(EventType.MouseDrag)]
        private static void HandleWindowPanning(EditorInputInfo inputInfo)
        {
            EditorState state = inputInfo.editorState;
            if (state.panWindow)
            {
                if (inputInfo.editorState.dragUserID == "window")
                    state.panOffset += state.UpdateDrag("window", inputInfo.inputPos);
                else
                    state.panWindow = false;
            }

            if (state.selectGrid)
            {
                Vector2 mousePos = inputInfo.inputPos;

                state.selectRect.width = Mathf.Abs(state.dragMouseStart.x - mousePos.x);
                state.selectRect.height = Mathf.Abs(state.dragMouseStart.y - mousePos.y);
                state.selectRect.x = (state.dragMouseStart.x <= mousePos.x ? state.dragMouseStart.x : mousePos.x);
                state.selectRect.y = (state.dragMouseStart.y <= mousePos.y ? state.dragMouseStart.y : mousePos.y);
                state.Select(state.selectRect);
            }
        }

        [EventHandlerAttribute(EventType.MouseDown)]
        [EventHandlerAttribute(EventType.MouseUp)]
        private static void HandleWindowPanningEnd(EditorInputInfo inputInfo)
        {
            if (inputInfo.editorState.dragUserID == "window")
                inputInfo.editorState.panOffset = inputInfo.editorState.EndDrag("window");
            inputInfo.editorState.panWindow = false;
            inputInfo.editorState.selectGrid = false;
            inputInfo.editorState.selectRect.width = 0;
            inputInfo.editorState.selectRect.height = 0;
        }

        private const double _zoomIntensity = 0.2 / 4.0;
        [EventHandlerAttribute(EventType.ScrollWheel)]
        private static void HandleZooming(EditorInputInfo inputInfo)
        {
            // 以当前鼠标位置开始缩放
            float z = inputInfo.editorState.zoom;
            // magic number？no 用函数模拟器看下就知道了
            float zoomFactor = (float)Math.Exp(inputInfo.inputEvent.delta.y * _zoomIntensity);
            inputInfo.editorState.zoom = (float)Mathf.Min(16.0f, Mathf.Max(0.002f, inputInfo.editorState.zoom * zoomFactor));
            inputInfo.editorState.panOffset += inputInfo.inputEvent.mousePosition * (inputInfo.editorState.zoom - z);

            int row = 1;
            while (true)
            {
                float step = 1024 / inputInfo.editorState.zoom / row;
                if (step >= 25 && step <= 50)
                {
                    inputInfo.editorState.gridStep = step;
                    break;
                }
                

                row *= 2;
                if (row > 1024)
                    break;
            }

            if(row > 1024)
                inputInfo.editorState.gridStep = 1024 / 16.0f;
        }
    }
}