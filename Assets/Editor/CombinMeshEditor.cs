using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class CombinMeshEditor : SubEditor
{
    public CombinMeshEditor(EditorState s)
    {
        editorState = s;
    }

    public enum Size
    {
        x128 = 128,
        x256 = 256,
        x512 = 512,
        x1024 = 1024,
        x2048 = 2048,
    }


    public Size targetHeightSize = Size.x1024;
    public Size targetWidthSize = Size.x1024;
    public string ShaderName = string.Empty;
    // 包含法线贴图？
    public bool hadNormal = false;
    // 包含高光贴图？
    public bool hadSpecular = false;
    public int padding = 0;
    public bool powerOfTwo = true;
    bool bakeTexture = false;

    /// 顶点顺序
    ///    0      1 
    ///    ┍──┑
    ///    │    │ 
    ///    │    │
    ///    ┕──┙
    ///    3      2
    public Rect[] currentPivotRect = new Rect[4];
    public Rect currentNodeRect = new Rect();
    public List<CombinNode> nodeList = new List<CombinNode>();
    public CombinNode focusedNode;

    public int combinTextureWidth = 1024;
    public int combinTextureHeight = 1024;
    Vector2 scrollPos = Vector2.zero;

    public Rect CombinTextureRect
    {
        get
        {
            Rect rect = new Rect(0, 0, combinTextureWidth / editorState.zoom, combinTextureHeight / editorState.zoom);
            rect.x += editorState.panOffset.x / editorState.zoom;
            rect.y += editorState.panOffset.y / editorState.zoom;
            EditorUtil.RoundRect(ref rect);
            return rect;
        }
    }

    public override void Draw()
    {
        UpdateFocusedNodeRect();

        DrawCombinRect();

        for (int i = nodeList.Count - 1; i >= 0; --i)
        {
            nodeList[i].Draw(this);
        }

        DrawFocusedNode();
    }

    public override void OnSceneGUI(SceneView sceneView)
    {

    }

    public override void Update()
    {
        if (bakeTexture)
        {
            bakeTexture = false;
            EditorUtil.BakeTexture(this);
        }
    }

    public override void OnGUI()
    {
        using (new Vertical())
        {
            using (new Horizontal())
            {
                GUILayout.Label("贴图宽：");
                GUILayout.FlexibleSpace();
                targetWidthSize = (Size)EditorGUILayout.EnumPopup(targetWidthSize, "OL Title", GUILayout.Height(30));
                combinTextureWidth = (int)targetWidthSize;
            }

            using (new Horizontal())
            {
                GUILayout.Label("贴图高：");
                GUILayout.FlexibleSpace();
                targetHeightSize = (Size)EditorGUILayout.EnumPopup(targetHeightSize, "OL Title", GUILayout.Height(30));
                combinTextureHeight = (int)targetHeightSize;
            }

            using (new Horizontal())
            {
                GUILayout.Label("Padding：");
                GUILayout.FlexibleSpace();
                padding = EditorGUILayout.IntField(padding);
            }

            using (new Horizontal())
            {
                powerOfTwo = GUILayout.Toggle(powerOfTwo, "2的N次方？");
            }

            if (GUILayout.Button("自动排列","OL Title"))
            {
                AutoPackTexture();
            }
            GUILayout.Space(20);
            if (GUILayout.Button("合并", "OL Title", GUILayout.MinHeight(30)))
            {
                if (combinTextureWidth * 1.0f / combinTextureHeight > 2 || combinTextureHeight * 1.0f / combinTextureWidth > 2)
                {
                    EditorUtility.DisplayDialog("错误", "合并的贴图宽高或高宽比不能超过2", "确定");
                    return;
                }

                bakeTexture = true;
            }

            if(Selection.gameObjects.Length > 0)
            {
                List<GameObject> selectObject = new List<GameObject>();
                for (int k = 0; k < Selection.gameObjects.Length; ++k)
                {
                    bool has = false;
                    GameObject obj = Selection.gameObjects[k];
                    for (int i = 0; i < nodeList.Count; ++i)
                    {
                        for (int j = 0; j < nodeList[i].objects.Count; ++j)
                        {
                            if (nodeList[i].objects[j] == obj)
                            {
                                has = true;
                                break;
                            }
                        }

                        if (has)
                            break;
                    }

                    if(!has)
                    {
                        selectObject.Add(obj);
                    }
                }


                if(selectObject.Count > 0)
                {
                    using (new Horizontal())
                    {
                        GUI.color = Color.red;
                        GUILayout.Button("", "MiniSliderHorizontal", GUILayout.Width(200));
                        GUI.color = Color.white;
                    }

                    GUI.backgroundColor = Color.blue;
                    if (GUILayout.Button("添加选中物体", "OL Title"))
                    {
                        AddNewMesh(selectObject);
                    }
                    GUI.backgroundColor = Color.white;

                    for (int i = 0; i < selectObject.Count; ++i)
                    {
                        GUI.backgroundColor = i % 2 == 0 ? new Color(0, 255, 0) : new Color(0, 155, 155);
                        GUILayout.Button(selectObject[i].name, "OL Title");
                    }

                    GUI.backgroundColor = Color.white;
                }
                  
            }

            using (new Horizontal())
            {
                GUI.color = Color.red;
                GUILayout.Button("", "MiniSliderHorizontal", GUILayout.Width(200));
                GUI.color = Color.white;
            }
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            int index = 0;
            for (int i = 0; i < nodeList.Count; ++i)
            {
                for (int j = 0; j < nodeList[i].objects.Count; ++j)
                {
                    using (new Horizontal())
                    {
                        GUI.backgroundColor = Color.yellow;
                        GUILayout.Button("" + (index++), "OL Title", GUILayout.MaxWidth(20));
                        GUI.backgroundColor = index % 2 == 0 ? new Color(0, 255, 0) : new Color(0, 155, 155);
                        if (GUILayout.Button(nodeList[i].objects[j].name, "OL Title"))
                        {
                            Selection.activeGameObject = nodeList[i].objects[j];
                        }
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("×", "OL Title", GUILayout.MaxWidth(20)))
                        {
                            nodeList[i].objects.RemoveAt(j);
                            if (nodeList[i].objects.Count == 0)
                            {
                                nodeList.RemoveAt(i);
                            }

                            focusedNode = null;
                            return;
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }

            CheckList();

            GUILayout.EndScrollView();
        }
    }

    void CheckList()
    {
        if (nodeList.Count == 0)
        {
            ShaderName = string.Empty;
            hadNormal = false;
            hadSpecular = false;
            focusedNode = null;
        }
    }

    public void DrawCombinRect()
    {
        Rect cbRect = CombinTextureRect;
        //EditorUtil.DrawRect(cbRect, Color.red, 3);
        Vector2 size = EditorUtil.GetStringSize("" + combinTextureWidth);
        Rect labelRect = new Rect();

        labelRect.width = size.x;
        labelRect.height = size.y;

        labelRect.center = cbRect.center;
        labelRect.y = cbRect.y + cbRect.height + 10 / editorState.zoom;
        labelRect.center += Vector2.one * 2;
        GUI.color = Color.black;
        GUI.Label(labelRect, "" + combinTextureWidth);
        labelRect.center -= Vector2.one ;
        GUI.color = Color.white;
        GUI.Label(labelRect, "" + combinTextureWidth);

        size = EditorUtil.GetStringSize("" + combinTextureHeight);
        labelRect.width = size.x;
        labelRect.height = size.y;
        labelRect.center = cbRect.center;
        labelRect.x = cbRect.x + cbRect.width + 10 / editorState.zoom;

        using (new GUIMatrix())
        {
            EditorGUIUtility.RotateAroundPivot(90, labelRect.center);
            labelRect.center += new Vector2(1, -1);
            GUI.color = Color.black;
            GUI.Label(labelRect, "" + combinTextureHeight);
            labelRect.center -= new Vector2(1, -1);
            GUI.color = Color.white;
            GUI.Label(labelRect, "" + combinTextureHeight);
        }
    }


    public void UpdateFocusedNodeRect()
    {
        if (focusedNode == null)
            return;

        currentNodeRect = new Rect(focusedNode.drawRect);

        currentPivotRect[0] = new Rect(currentNodeRect.x - 7
                                        , currentNodeRect.y - 7
                                        , 14
                                        , 14);

        currentPivotRect[1] = new Rect(currentNodeRect.x + currentNodeRect.width - 7
                                        , currentNodeRect.y - 7
                                        , 14
                                        , 14);



        currentPivotRect[2] = new Rect(currentNodeRect.x + currentNodeRect.width - 7
                                        , currentNodeRect.y + currentNodeRect.height - 7
                                        , 14
                                        , 14);

        currentPivotRect[3] = new Rect(currentNodeRect.x - 7
                                        , currentNodeRect.y + currentNodeRect.height - 7
                                        , 14
                                        , 14);

    }

    public override Rect GetDrawRect()
    {
        return CombinTextureRect;
    }

    public void DrawFocusedNode()
    {
        if (focusedNode == null)
            return;

        EditorGUIUtility.AddCursorRect(focusedNode.drawRect, MouseCursor.MoveArrow);
        EditorUtil.DrawRect(focusedNode.drawRect, Color.yellow, 1.1f);
        string title = string.Format("{0}×{1}", Mathf.RoundToInt(focusedNode.drawRect.width * zoom), Mathf.RoundToInt(focusedNode.drawRect.height * zoom));
        Vector2 size = EditorUtil.GetStringSize(title);
        Rect labelRect = new Rect();

        labelRect.width = size.x;
        labelRect.height = size.y;

        labelRect.center = focusedNode.drawRect.center;
        labelRect.y = focusedNode.drawRect.y + focusedNode.drawRect.height - labelRect.height;
        GUI.color = Color.red;
        GUI.Label(labelRect, title);
        GUI.color = Color.white;
        for (int i = 0; i < 4; ++i)
        {
            EditorUtil.blueDot.Draw(currentPivotRect[i], GUIContent.none, editorState.id);
            EditorGUIUtility.AddCursorRect(currentPivotRect[i], MouseCursor.ScaleArrow);
        }
    }


    // 自动对齐贴图
    public void AutoPackTexture()
    {
        Texture2D packingTex = new Texture2D(1, 1, TextureFormat.Alpha8, false);

        float s = 1;
        List<Texture2D> readyTexList = new List<Texture2D>();
        for (int i = 0; i < nodeList.Count; ++i)
        {
            CombinNode ti = nodeList[i];

            Texture2D tex = new Texture2D((int)ti.rect.width, (int)(ti.rect.height), TextureFormat.Alpha8, false);
            readyTexList.Add(tex);
        }

        if (readyTexList.Count == 0)
            return;

        Rect[] rects = packingTex.PackTextures(readyTexList.ToArray(), padding, 2048);

        if (rects == null)
        {
            EditorUtility.DisplayDialog("错误", "自动排列失败，贴图太多太大，2048装不下，对贴图缩放一下", "确定");
        }

        int w = packingTex.width;
        int h = packingTex.height;

        combinTextureWidth = w;
        combinTextureHeight = h;

        targetHeightSize = (Size)h;
        targetWidthSize = (Size)w;

        for (int i = 0; i < nodeList.Count; ++i)
        {
            Rect rect = rects[i];
            CombinNode ti = nodeList[i];
            ti.rect.x = rect.x * w;
            ti.rect.y = rect.y * h;
            ti.rect.width = rect.width * w;
            ti.rect.height = rect.height * h;
        }

        readyTexList.Clear();
        readyTexList = null;
        rects = null;
    }


    // 添加新物体，并作检查
    public void AddNewMesh(List<GameObject> objects)
    {
        for(int i = 0; i < objects.Count; ++i)
        {
            GameObject obj = objects[i];
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("错误", "添加的物体[" + obj.name +"]不包含mesh!", "确定");
                return;
            }

            Renderer rd = obj.GetComponent<Renderer>();
            if (rd == null || rd.sharedMaterial == null)
            {
                EditorUtility.DisplayDialog("错误", "添加的物体[" + obj.name + "]不包含材质球，最好是添加预制件!", "确定");
                return;
            }

            Material mat = rd.sharedMaterial;

            if (mat.mainTexture == null || mat.mainTexture.height > 2048 || mat.mainTexture.width > 2048)
            {
                EditorUtility.DisplayDialog("错误", "添加的物体[" + obj.name + "]材质球没有贴图!或者贴图的尺寸>2048", "确定");
                return;
            }

            if (string.IsNullOrEmpty(ShaderName))
            {
                ShaderName = mat.shader.name;
                hadNormal = mat.HasProperty("_Normal") && mat.GetTexture("_Normal") != null;
                hadSpecular = mat.HasProperty("_SpecTex") && mat.GetTexture("_SpecTex") != null;
            }

            if (mat.shader.name != ShaderName)
            {
                EditorUtility.DisplayDialog("错误", "添加的物体[" + obj.name + "]材质球和之前添加的物体的着色器不相同!", "确定");
                return;
            }

            Mesh mesh = mf.sharedMesh;

            Texture2D texNormal = mat.HasProperty("_Normal") ? mat.GetTexture("_Normal") as Texture2D : null;
            Texture2D texSpecular = mat.HasProperty("_SpecTex") ? mat.GetTexture("_SpecTex") as Texture2D : null;
            if (hadNormal && texNormal == null)
            {
                EditorUtility.DisplayDialog("错误", "[" + obj.name + "]材质包含法线贴图，添加的物体不包含!", "确定");
                return;
            }

            if (hadSpecular && texSpecular == null)
            {
                EditorUtility.DisplayDialog("错误", "[" + obj.name + "]材质包含高光贴图，添加的物体不包含!", "确定");
                return;
            }

            for (int j = 0; j < nodeList.Count; ++j)
            {
                CombinNode ti = nodeList[j];
                if (ti.meshs.Contains(mesh))
                {
                    EditorUtility.DisplayDialog("错误", "已经添加过包含此Mesh的物体了!", "确定");
                    return;
                }
            }
        }

        for (int i = 0; i < objects.Count; ++i)
        {
            GameObject obj = objects[i];
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;
            Renderer rd = obj.GetComponent<Renderer>();
            Material mat = rd.sharedMaterial;

            Texture2D texNormal = mat.HasProperty("_Normal") ? mat.GetTexture("_Normal") as Texture2D : null;
            Texture2D texSpecular = mat.HasProperty("_SpecTex") ? mat.GetTexture("_SpecTex") as Texture2D : null;
            for (int j = 0; j < nodeList.Count; ++j)
            {
                CombinNode ti = nodeList[j];
                if (ti.texture == mat.mainTexture)
                {
                    ti.meshs.Add(mesh);
                    ti.objects.Add(obj);
                    focusedNode = ti;
                    return;
                }
            }

            CombinNode combinNode = new CombinNode();
            combinNode.texture = mat.mainTexture as Texture2D;
            combinNode.textureNormal = texNormal;
            combinNode.textureSpecular = texSpecular;
            combinNode.rect = new Rect(0, 0, combinNode.texture.width, combinNode.texture.height);
            combinNode.path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(combinNode.texture));
            combinNode.objects.Add(obj);
            combinNode.meshs.Add(mesh);
            nodeList.Add(combinNode);
            focusedNode = combinNode;
        }
    }

    // 处理鼠标移动事件，在节点上的
    [EventHandlerAttribute(EventType.MouseDrag, 50)]
    private static void HandleNodeAction(EditorInputInfo inputInfo)
    {
        CombinMeshEditor cme = inputInfo.editorState.editor as CombinMeshEditor;

        if (inputInfo.editorState.action == EditorState.Action.None || cme.focusedNode == null)
            return;

        inputInfo.editorState.dragOffset = inputInfo.inputPos - inputInfo.editorState.dragMouseStart;
        if (inputInfo.editorState.action == EditorState.Action.Move)
        {
            CombinNode cn = cme.focusedNode;
            cn.offset = inputInfo.editorState.dragOffset;
        }

        if (inputInfo.editorState.action == EditorState.Action.Scale)
        {
            CombinNode cn = cme.focusedNode;
            Vector2 offset = inputInfo.editorState.dragOffset;
            if (inputInfo.inputEvent.shift)
            {

                switch (inputInfo.editorState.pivot)
                {
                    case EditorState.Pivot.LeftTop:
                        {
                            float max = Mathf.Max(-offset.x, -offset.y);
                            offset = new Vector2(-max, -max);
                        }; break;
                    case EditorState.Pivot.LeftBottom:
                        {
                            float max = Mathf.Max(-offset.x, offset.y);
                            offset = new Vector2(-max, max);
                        }; break;
                    case EditorState.Pivot.RightTop:
                        {
                            float max = Mathf.Max(offset.x, -offset.y);
                            offset = new Vector2(max, -max);
                        }; break;
                    case EditorState.Pivot.RightBottom:
                        {
                            float max = Mathf.Max(offset.x, offset.y);
                            offset = new Vector2(max, max);
                        }; break;
                }
            }

            cn.scale = offset;
        }
    }


    // 鼠标抬起的时候判定到底哪个节点被选中
    [EventHandlerAttribute(EventType.MouseUp, 50)]
    private static void HandleNodeActionEnd(EditorInputInfo inputInfo)
    {
        CombinMeshEditor cme = inputInfo.editorState.editor as CombinMeshEditor;

        if (inputInfo.editorState.action == EditorState.Action.None && inputInfo.inputEvent.button == 0)
        {
            for (int i = 0; i < cme.nodeList.Count; ++i)
            {
                CombinNode cn = cme.nodeList[i];
                if (cn.drawRect.Contains(inputInfo.inputPos))
                {
                    cme.focusedNode = cn;
                    break;
                }
            }
        }

        if (inputInfo.editorState.action == EditorState.Action.Move && cme.focusedNode != null)
        {
            CombinNode cn = cme.focusedNode;

            cn.rect.x += inputInfo.editorState.dragOffset.x * inputInfo.editorState.zoom;
            cn.rect.y += inputInfo.editorState.dragOffset.y * inputInfo.editorState.zoom;
            cn.offset = Vector2.zero;
            inputInfo.editorState.dragOffset = Vector2.zero;
        }

        if (inputInfo.editorState.action == EditorState.Action.Scale && cme.focusedNode != null)
        {
            CombinNode cn = cme.focusedNode;
            Rect rect = cn.drawRect;

            float zoom = inputInfo.editorState.zoom;

            if (cme.powerOfTwo)
            {
                cn.rect.width = EditorUtil.ToNearPowerOfTwo((int)(rect.width * zoom));
                cn.rect.height = EditorUtil.ToNearPowerOfTwo((int)(rect.height * zoom));
            }
            else
            {
                cn.rect.width = rect.width * zoom;
                cn.rect.height = rect.height * zoom;
            }


            cn.rect.x = rect.x * zoom - inputInfo.editorState.panOffset.x;
            cn.rect.y = rect.y * zoom - inputInfo.editorState.panOffset.y;


            cn.scale = Vector2.zero;
            inputInfo.editorState.dragOffset = Vector2.zero;
        }


        inputInfo.editorState.action = EditorState.Action.None;
    }


    // 判断鼠标事件
    [EventHandlerAttribute(EventType.MouseDown,50)]
    private static void HandleNodeActionStart(EditorInputInfo inputInfo)
    {
        CombinMeshEditor cme = inputInfo.editorState.editor as CombinMeshEditor;

        if (cme.focusedNode == null || inputInfo.inputEvent.button != 0)
            return;

        EditorState state = inputInfo.editorState;

        Vector2 pos = inputInfo.inputEvent.mousePosition;
        state.action = EditorState.Action.None;

        int pivotIndex = -1;
        for (int i = 0; i < 4; ++i)
        {
            if (cme.currentPivotRect[i].Contains(pos))
            {
                pivotIndex = i;
                state.action = EditorState.Action.Scale;
                break;
            }
        }

        if (pivotIndex == -1)
        {
            if (cme.focusedNode.drawRect.Contains(pos))
            {
                state.action = EditorState.Action.Move;
            }
        }

        if (state.action != EditorState.Action.None)
        {
            state.pivot = (EditorState.Pivot)(pivotIndex + 1);
            state.dragMouseStart = pos;
            state.dragOffset = Vector2.zero;
        }


    }

    // 处理键盘事件
    [HotkeyAttribute(KeyCode.UpArrow, EventType.KeyDown)]
    [HotkeyAttribute(KeyCode.LeftArrow, EventType.KeyDown)]
    [HotkeyAttribute(KeyCode.RightArrow, EventType.KeyDown)]
    [HotkeyAttribute(KeyCode.DownArrow, EventType.KeyDown)]
    private static void HandleKey(EditorInputInfo inputInfo)
    {
        CombinMeshEditor cme = inputInfo.editorState.editor as CombinMeshEditor;

        if (GUIUtility.keyboardControl > 0)
            return;
        EditorState state = inputInfo.editorState;
        if (cme.focusedNode != null)
        {
            Vector2 pos = Vector2.zero;
            if (inputInfo.inputEvent.keyCode == KeyCode.RightArrow)
                pos += new Vector2(1, 0);

            if (inputInfo.inputEvent.keyCode == KeyCode.LeftArrow)
                pos += new Vector2(-1, 0);

            if (inputInfo.inputEvent.keyCode == KeyCode.DownArrow)
                pos += new Vector2(0, 1);

            if (inputInfo.inputEvent.keyCode == KeyCode.UpArrow)
                pos += new Vector2(0, -1);

            cme.focusedNode.rect.x += pos.x * state.zoom;
            cme.focusedNode.rect.y += pos.y * state.zoom;
            inputInfo.inputEvent.Use();
        }
    }

    [ContextEntryAttribute("左转90°")]
    private static void RightRotNode(EditorInputInfo inputInfo)
    {
        CombinMeshEditor cme = inputInfo.editorState.editor as CombinMeshEditor;
        if (cme.focusedNode != null)
        {

            cme.focusedNode.rotation += 90;
            inputInfo.inputEvent.Use();
        }
    }

    [ContextEntryAttribute("右转90°")]
    private static void LeftRotNode(EditorInputInfo inputInfo)
    {
        CombinMeshEditor cme = inputInfo.editorState.editor as CombinMeshEditor;
        if (cme.focusedNode != null)
        {
            cme.focusedNode.rotation -= 90;
            inputInfo.inputEvent.Use();
        }
    }

    // 处理菜单事件
    [EventHandlerAttribute(EventType.MouseDown, 50)]
    private static void HandleContextClicks(EditorInputInfo inputInfo)
    {
        CombinMeshEditor cme = inputInfo.editorState.editor as CombinMeshEditor;
        if (cme.focusedNode == null)
            return;

        if (Event.current.button == 1 && cme.focusedNode.drawRect.Contains(inputInfo.inputPos))
        {
            GenericMenu contextMenu = new GenericMenu();
            if (cme.focusedNode != null)
                EditorInputSystem.FillContextMenu(inputInfo, contextMenu);
            contextMenu.ShowAsContext();
            Event.current.Use();
        }
    }
}
