using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;
using System;
using FbxExporters.Editor;
using FbxExporters.EditorTools;


public class ModelToolEditor : EditorWindow
{
    public Rect canvasWindowRect { get { return new Rect(0, 0, position.width, position.height); } }
    public Rect ToolBarRect { get { return new Rect(position.width - toobarWidth, 0, toobarWidth, position.height); } }
    public static int toobarWidth = 200;

    [MenuItem("Window/地编工具")]
    public static void ShowWindow()
    {
        ModelToolEditor window = EditorWindow.GetWindow<ModelToolEditor>() as ModelToolEditor;
        window.maxSize = new Vector2(2048, 2048);
        window.minSize = new Vector2(512, 700);
        window.titleContent = new GUIContent("地编工具");
        window.Show();
    }

    private bool initShow = false;
    private void InitShowPos()
    {
        if (initShow)
            return;

        initShow = true;
        float w = (canvasWindowRect.width - toobarWidth) * 0.66f;
        editorState.zoom = 1024.0f / w;
        Vector2 offset = Vector2.zero;
        offset.x = (canvasWindowRect.height - toobarWidth) / 6.0f;
        offset.y = offset.x;
        editorState.panOffset = offset * editorState.zoom;
    }

    EditorState eState;
    EditorState editorState
    {
        get
        {
            if (eState == null)
            {
                eState = new EditorState();
                eState.toolBarWidth = toobarWidth;
            }

            if (eState.editor == null)
            {
                eState.editor = new CombinMeshEditor(eState);
                eState.RegisterEventType(typeof(CombinMeshEditor));
                eState.canvasRect = canvasWindowRect;
            }


            return eState;
        }
    }

    private void OnFocus()
    {
        EditorInputSystem.SetupInput(editorState.eventTypeList);
    }

    void Update()
    {
        InitShowPos();
        Repaint();
        editorState.Update();
    }

    public void OnSceneGUI(SceneView sceneView)
    {
        editorState.OnSceneGUI(sceneView);
    }

    void OnGUI()
    {
        Rect actionWindowRect = new Rect(106f, 100f, 300, 340f);
        using (new Horizontal())
        {
            editorState.id = GUIUtility.GetControlID(this.GetHashCode(), FocusType.Passive);

            EditorUtil.painWindowRect = new Rect(canvasWindowRect);
            EditorUtil.painWindowRect.width -= 200;
            editorState.canvasRect = GUILayoutUtility.GetRect(canvasWindowRect.width - toobarWidth, canvasWindowRect.height);

            if (Event.current.type == EventType.Repaint)
            {
                editorState.Draw();
                // 用这个矩形覆盖掉toolbar下面的东西
                GUI.DrawTexture(ToolBarRect, EditorUtil.ToolBarBackground, ScaleMode.StretchToFill);
            }

            using (new Vertical())
            {
                Rect tool = GUILayoutUtility.GetRect(toobarWidth, 40);
                editorState.OnGUI();
            }
        }
        EditorInputSystem.HandleInputEvents(editorState);

        EditorInputSystem.HandleLateInputEvents(editorState);
    }

}