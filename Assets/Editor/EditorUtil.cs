using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.IO;
using FbxExporters.Editor;
using FbxExporters.EditorTools;


internal class DrawMeshScope : GUI.Scope
{
    private readonly Rect rect;
    private RenderTexture renderTexture;
    static Rect tileRect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

    public DrawMeshScope(Rect rec, float scaleFactor)
    {
        rect = rec;
        renderTexture = RenderTexture.GetTemporary(
            (int)(rect.width * scaleFactor),
            (int)(rect.height * scaleFactor),
            0);
        renderTexture.filterMode = FilterMode.Point;

        RenderTexture.active = renderTexture;
        GUI.matrix = Matrix4x4.identity;

        GL.Clear(true, true, new Color());
    }

    protected override void CloseScope()
    {
        RenderTexture.active = null;
        GUI.matrix = Matrix4x4.identity;

        Graphics.DrawTexture(
            rect
            , renderTexture
            , tileRect
            , 0
            , 0 
            , 0
            , 0);

        RenderTexture.ReleaseTemporary(renderTexture);
        renderTexture = null;
    }
}


public class Matrix2x2
{
    public float m00;
    public float m01;
    public float m10;
    public float m11;

    static Matrix2x2 _identity;
    public static Matrix2x2 identity
    {
        get
        {
            if (_identity == null)
            {
                _identity = new Matrix2x2();
                _identity.m00 = 1;
                _identity.m01 = 0;
                _identity.m10 = 0;
                _identity.m11 = 1;
            }

            return _identity;
        }
    }

    public void Rotate(float angle)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        Matrix2x2 tmp = this.Clone();
        m00 = tmp.m00 * cos + tmp.m01 * sin;
        m01 = tmp.m00 * -sin + tmp.m01 * cos;

        m10 = tmp.m10 * cos + tmp.m11 * sin;
        m11 = tmp.m01 * -sin + tmp.m11 * cos;
    }

    public static Vector2 operator *(Matrix2x2 lhs, Vector2 v)
    {
        Vector2 ret = new Vector2();
        ret.x = v.x * lhs.m00 + v.y * lhs.m10;
        ret.y = v.x * lhs.m01 + v.y * lhs.m11;
        return ret;
    }

    public static Rect operator *(Matrix2x2 lhs, Rect rect)
    {
        // 只支持旋转90°的倍数
        Vector2 center = rect.center;
        center.y *= -1;
        Rect ret = new Rect();
        // 先将rect的坐标系转换到正常坐标系中
        // 并平移到原点做旋转
        Vector2 leftTop = new Vector2(center.x - rect.width / 2, (center.y - rect.height / 2)) - center;
        Vector2 rightTop = new Vector2(center.x + rect.width / 2, (center.y - rect.height / 2)) - center;
        Vector2 leftBottom = new Vector2(center.x - rect.width / 2, (center.y + rect.height / 2)) - center;
        Vector2 rightBottom = new Vector2(center.x + rect.width / 2, (center.y + rect.height / 2)) - center;

        leftTop = lhs * leftTop;
        rightTop = lhs * rightTop;
        leftBottom = lhs * leftBottom;
        rightBottom = lhs * rightBottom;

        float minx = Mathf.Min(leftTop.x, Mathf.Min(leftBottom.x, Mathf.Min(rightBottom.x, rightTop.x)));
        float miny = Mathf.Min(leftTop.y, Mathf.Min(leftBottom.y, Mathf.Min(rightBottom.y, rightTop.y)));
        float maxx = Mathf.Max(leftTop.x, Mathf.Max(leftBottom.x, Mathf.Max(rightBottom.x, rightTop.x)));
        float maxy = Mathf.Max(leftTop.y, Mathf.Max(leftBottom.y, Mathf.Max(rightBottom.y, rightTop.y)));

        ret.width = maxx - minx;
        ret.height = maxy - miny;
        ret.x = minx + rect.center.x;
        ret.y = miny + rect.center.y;

        return ret;
    }

    public Matrix2x2 Clone()
    {
        Matrix2x2 ret = new Matrix2x2();
        ret.m00 = m00;
        ret.m01 = m01;
        ret.m10 = m10;
        ret.m11 = m11;

        return ret;
    }
}


public class GameViewSize : IDisposable
{
    PropertyInfo widthPro = null;
    PropertyInfo heightPro = null;
    object sizePro = null;
    int bakWidth;
    int bakHeight;

    public void Dispose()
    {
        widthPro.SetValue(sizePro, bakWidth, new object[0] { });
        heightPro.SetValue(sizePro, bakHeight, new object[0] { });
    }

    public GameViewSize()
    {
        InitGameView(ref widthPro, ref heightPro, ref sizePro);
        bakWidth = (int)widthPro.GetValue(sizePro, new object[0] { });
        bakHeight = (int)heightPro.GetValue(sizePro, new object[0] { });

        widthPro.SetValue(sizePro, 1024, new object[0] { });
        heightPro.SetValue(sizePro, 1024, new object[0] { });
    }

    void InitGameView(ref PropertyInfo widthPro, ref PropertyInfo heightPro, ref object sizePro)
    {
        var gameView = GetMainGameView();
        var prop = gameView.GetType().GetProperty("currentGameViewSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        sizePro = prop.GetValue(gameView, new object[0] { });
        var gvSizeType = sizePro.GetType();

        widthPro = gvSizeType.GetProperty("width", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        heightPro = gvSizeType.GetProperty("height", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        gvSizeType.GetProperty("height", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).SetValue(sizePro, 1024, new object[0] { });
        gvSizeType.GetProperty("width", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).SetValue(sizePro, 1024, new object[0] { });
    }

    UnityEditor.EditorWindow GetMainGameView()
    {
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetMainGameView = T.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetMainGameView.Invoke(null, null);
        return (UnityEditor.EditorWindow)Res;
    }
}


public class GUIColor : IDisposable
{
    public Color color;

    public GUIColor(Color c)
    {
        color = c;
        GUI.color = c;
    }

    public void Dispose()
    {
        GUI.color = color;
    }
}

public class GUIMatrix : IDisposable
{
    public Matrix4x4 matrix;

    public GUIMatrix()
    {
        matrix = GUI.matrix;
    }

    public void Dispose()
    {
        GUI.matrix = matrix;
    }
}

public class Horizontal : IDisposable
{
    public Horizontal()
    {
        GUILayout.BeginHorizontal();
    }

    public void Dispose()
    {
        GUILayout.EndHorizontal();
    }
}

public class Vertical : IDisposable
{
    public Vertical()
    {
        GUILayout.BeginVertical();
    }

    public void Dispose()
    {
        GUILayout.EndVertical();
    }
}

public class EditorColor
{
    public static Color BackGround = new Color(56 / 255.0f, 56 / 255.0f, 56 / 255.0f);
    public static Color GridLineColor = new Color(52 / 255.0f, 52 / 255.0f, 52 / 255.0f);
    public static Color SelectGridColor = new Color(128 / 255.0f, 205 / 255.0f, 1);
}


public class EditorUtil
{
    public static Rect MaxRect;
    public static Material lineMaterial;
    public static void CreateLineMaterial()
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }

    public static void BeginDrawLine(Color color)
    {
        CreateLineMaterial();
        lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(color);

    }

    public static void EndDrawLine()
    {
        GL.End();
    }

    public enum DrawAlign
    {
        TOP,
        TOPLEFT,
        TOPRIGHT,
        LEFT,
        RIGHT,
        CENTER,
        BOTTOM,
        BOTTOMLEFT,
        BOTTOMRIGHT,
    }

    static Rect tmpRect = new Rect();

    public static void ConvertRectWithAlign(ref Rect rect, DrawAlign align)
    {
        switch (align)
        {
            case DrawAlign.TOPLEFT:
                {
                    rect.x += rect.width / 2;
                    rect.y += rect.height / 2;
                }; break;
            case DrawAlign.TOP:
                {
                    rect.y += rect.height / 2;
                }; break;
            case DrawAlign.TOPRIGHT:
                {
                    rect.x -= rect.width / 2;
                    rect.y += rect.height / 2;
                }; break;
            case DrawAlign.LEFT:
                {
                    rect.x  += rect.width / 2;
                }; break;
            case DrawAlign.CENTER:
                {
                    // 什么都不做
                }; break;
            case DrawAlign.RIGHT:
                {
                    rect.x -= rect.width / 2;
                }; break;
            case DrawAlign.BOTTOM:
                {
                    rect.y -= rect.height / 2;
                }; break;
            case DrawAlign.BOTTOMLEFT:
                {
                    rect.x += rect.width / 2;
                    rect.y -= rect.height / 2;
                }; break;
            case DrawAlign.BOTTOMRIGHT:
                {
                    rect.x -= rect.width / 2;
                    rect.y -= rect.height / 2;
                }; break;

        }
    }


    public static bool RectIntersect(Rect a, Rect b)
    {
        if (a.xMin > b.xMax || a.xMax < b.xMin || a.yMin > b.yMax || a.yMax < b.yMin)
            return false;

        return true;
    }

    public static bool PointInRect(Vector3 a, Rect b)
    {
        if (a.x > b.xMax || a.x < b.xMin || a.y > b.yMax || a.y < b.yMin)
            return false;

        return true;
    }

    public static bool PointInTriangle(Vector2 P, Vector3 A , Vector3 B, Vector3 C)
    {
        Vector3 v0 = C - A;
        Vector3 v1 = B - A;
        Vector3 v2 = (Vector3)P - A;

        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1);
        float dot02 = Vector3.Dot(v0, v2);
        float dot11 = Vector3.Dot(v1, v1);
        float dot12 = Vector3.Dot(v1, v2);

        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return (u >= 0) && (v >= 0) && (u + v < 1);
    }

    public static void DrawTexture(Vector2 pos, int width, int height, Texture2D tex, Rect tile, Color color, DrawAlign align = DrawAlign.CENTER)
    {
        tmpRect.width = width;
        tmpRect.height = height;
        tmpRect.center = pos;

        ConvertRectWithAlign(ref tmpRect, align);
        using (new GUIColor(color))
        {
            GUI.DrawTextureWithTexCoords(tmpRect, tex, tile, true);
        }
                
    }

    public static void DrawLabel(Vector2 pos,string lb, Color color, DrawAlign align = DrawAlign.CENTER, bool shadow = false)
    {
        Vector2 size = EditorUtil.GetStringSize(lb);

        tmpRect.width = size.x;
        tmpRect.height = size.y;
        tmpRect.center = pos;
        ConvertRectWithAlign(ref tmpRect, align);

        if (shadow)
        {
            using (new GUIColor(Color.black))
            {
                tmpRect.center += Vector2.one;
                GUI.Label(tmpRect, lb);
                tmpRect.center -= Vector2.one;
            }
        }

        using (new GUIColor(color))
        {
            GUI.Label(tmpRect, lb);
        }
    }

    public static void DrawQuad(Rect rect, Color color)
    {
        GL.Color(color);
        GL.Vertex3(rect.x, rect.y, 0);
        GL.Vertex3(rect.x + rect.width, rect.y, 0);
        GL.Vertex3(rect.x + rect.width, rect.y + rect.height, 0);
        GL.Vertex3(rect.x, rect.y + rect.height, 0);
    }

    public static void DrawTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        if(a.y < 0 && b.y < 0 && c.y < 0)
        {
            return;
        }
    }

    public static void DrawRect(Rect rect, Color color)
    {
        GL.Color(color);
        GL.Vertex3(rect.x, Mathf.Max(0, rect.y), 0);
        GL.Vertex3(rect.x + rect.width, Mathf.Max(0, rect.y), 0);
        GL.Vertex3(rect.x + rect.width, Mathf.Max(0, rect.y), 0);
        GL.Vertex3(rect.x + rect.width, Mathf.Max(0, rect.y + rect.height), 0);
        GL.Vertex3(rect.x + rect.width, Mathf.Max(0, rect.y + rect.height), 0);
        GL.Vertex3(rect.x, Mathf.Max(0, rect.y + rect.height), 0);
        GL.Vertex3(rect.x, Mathf.Max(0, rect.y + rect.height), 0);
        GL.Vertex3(rect.x, Mathf.Max(0, rect.y), 0);
    }

    public static void DrawLine(Vector2 pointA, Vector2 pointB)
    {
        GL.Vertex3(pointA.x, pointA.y, 0);
        GL.Vertex3(pointB.x, pointB.y, 0);
    }

    static Texture2D _bgTex;
    public static Texture2D Background
    {
        get
        {
            if (_bgTex == null)
            {
                _bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Painter/background.png");
            }

            return _bgTex;
        }
    }

    static Texture2D _toolBarBackground;
    public static Texture2D ToolBarBackground
    {
        get
        {
            if (_toolBarBackground == null)
            {
                _toolBarBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                _toolBarBackground.SetPixel(0, 0, new Color(56 / 255.0f, 56 / 255.0f, 56 / 255.0f));
                _toolBarBackground.Apply();
            }

            return _toolBarBackground;
        }
    }

    static Texture2D _buttonTex;
    public static Texture2D ButtonTex
    {
        get
        {
            if (_buttonTex == null)
            {
                _buttonTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                _buttonTex.SetPixel(0, 0, new Color(1, 1, 1));
                _buttonTex.Apply();
            }
            return _buttonTex;
        }

    }

    public static void DrawRectFill(Rect rect, Color color)
    {
        using (new GUIColor(color))
        {
            GUI.DrawTexture(rect, ButtonTex, ScaleMode.StretchToFill);
        }
    }


    public static int ToNearPowerOfTwo(int v)
    {
        int next = Mathf.NextPowerOfTwo(v);
        int pre = next / 2;

        return next - v > v - pre ? pre : next;
    }

    public static Vector2 GetStringSize(string str)
    {
        return GUI.skin.label.CalcSize(new GUIContent(str));
    }

    public void DrawLine(Rect rect) { DrawLine(rect, GUI.contentColor, 1.0f); }
    public static void DrawRect(Rect rec, Color color, float width, string title = "")
    {
        Rect rect = new Rect(rec);
        RoundRect(ref rect);
        Vector2 leftTop = new Vector2(rect.center.x - rect.width / 2, rect.center.y - rect.height / 2);
        Vector2 rightTop = new Vector2(rect.center.x + rect.width / 2, rect.center.y - rect.height / 2);
        Vector2 leftBottom = new Vector2(rect.center.x - rect.width / 2, rect.center.y + rect.height / 2);
        Vector2 rightBottom = new Vector2(rect.center.x + rect.width / 2, rect.center.y + rect.height / 2);

        //Rect painWindowRect = new Rect(UVCombin.canvasWindowRect);
        //painWindowRect.width -= 200;

        DrawLine(leftTop, leftBottom, color, width);
        DrawLine(rightTop, rightBottom, color, width);
        DrawLine(leftTop, rightTop, color, width);
        DrawLine(rightBottom, leftBottom, color, width);
    }

    public static void DrawRectWithQuadsLine(Rect rect, Color color, float width)
    {
        Vector2 leftTop = new Vector2(rect.center.x - rect.width / 2, rect.center.y - rect.height / 2);
        Vector2 rightTop = new Vector2(rect.center.x + rect.width / 2, rect.center.y - rect.height / 2);
        Vector2 leftBottom = new Vector2(rect.center.x - rect.width / 2, rect.center.y + rect.height / 2);
        Vector2 rightBottom = new Vector2(rect.center.x + rect.width / 2, rect.center.y + rect.height / 2);

        float halfWidth = width / 2;
        CreateLineMaterial();
        lineMaterial.SetPass(0);
        GL.Begin(GL.QUADS);
        GL.Color(color);
        GL.Vertex3(leftTop.x - halfWidth, leftTop.y - halfWidth, 0);
        GL.Vertex3(leftTop.x + halfWidth, leftTop.y + halfWidth, 0);
        GL.Vertex3(leftBottom.x + halfWidth, leftBottom.y - halfWidth, 0);
        GL.Vertex3(leftBottom.x - halfWidth, leftBottom.y + halfWidth, 0);

        GL.Vertex3(leftTop.x - halfWidth, leftTop.y - halfWidth, 0);
        GL.Vertex3(rightTop.x + halfWidth, rightTop.y - halfWidth, 0);
        GL.Vertex3(rightTop.x - halfWidth, rightTop.y + halfWidth, 0);
        GL.Vertex3(leftTop.x + halfWidth, leftTop.y + halfWidth, 0);

        GL.Vertex3(rightTop.x + halfWidth, rightTop.y - halfWidth, 0);
        GL.Vertex3(rightBottom.x + halfWidth, rightBottom.y + halfWidth, 0);
        GL.Vertex3(rightBottom.x - halfWidth, rightBottom.y - halfWidth, 0);
        GL.Vertex3(rightTop.x - halfWidth, rightTop.y + halfWidth, 0);

        GL.Vertex3(rightBottom.x - halfWidth, rightBottom.y - halfWidth, 0);
        GL.Vertex3(rightBottom.x + halfWidth, rightBottom.y + halfWidth, 0);
        GL.Vertex3(leftBottom.x - halfWidth, leftBottom.y + halfWidth, 0);
        GL.Vertex3(leftBottom.x + halfWidth, leftBottom.y - halfWidth, 0);

        GL.End();

        //Rect rect = new Rect(rec);
        //RoundRect(ref rect);
           

        ////Rect painWindowRect = new Rect(UVCombin.canvasWindowRect);
        ////painWindowRect.width -= 200;

        //DrawLine(leftTop, leftBottom, color, width);
        //DrawLine(rightTop, rightBottom, color, width);
        //DrawLine(leftTop, rightTop, color, width);
        //DrawLine(rightBottom, leftBottom, color, width);
    }

    static Texture2D lineTex;
    public static Rect painWindowRect;


    public static void DrawLine(Rect rect, Color color) { DrawLine(rect, color, 1.0f); }
    public static void DrawLine(Rect rect, float width) { DrawLine(rect, GUI.contentColor, width); }
    public static void DrawLine(Rect rect, Color color, float width) { DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x + rect.width, rect.y + rect.height), color, width); }

    public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
    {
        //painWindowRect.width -= 200;

        if ((pointA.x < 0 && pointB.x < 0)
            || (pointA.y < 0 && pointB.y < 0)
            || (pointA.y > painWindowRect.height && pointB.y > painWindowRect.height)
            || (pointA.x > painWindowRect.width && pointB.x > painWindowRect.width)
            )
            return;



        var matrix = GUI.matrix;

        if (!lineTex)
        {
            lineTex = new Texture2D(1, 1);
            lineTex.SetPixel(0, 0, Color.white);
            lineTex.Apply();
        }


        pointA.x = Mathf.Max(0, Mathf.Min(painWindowRect.width, pointA.x));
        pointA.y = Mathf.Max(0, Mathf.Min(painWindowRect.height, pointA.y));
        pointB.x = Mathf.Max(0, Mathf.Min(painWindowRect.width, pointB.x));
        pointB.y = Mathf.Max(0, Mathf.Min(painWindowRect.height, pointB.y));

        if ((pointA - pointB).magnitude < 1)
            return;

        var savedColor = GUI.color;
        GUI.color = color;

        var angle = Vector3.Angle(pointB - pointA, Vector2.right);

        if (pointA.y > pointB.y) { angle = -angle; }

        GUIUtility.ScaleAroundPivot(new Vector2((pointB - pointA).magnitude, width), new Vector2(pointA.x, pointA.y + 0.5f));

        GUIUtility.RotateAroundPivot(angle, pointA);

        GUI.DrawTexture(new Rect(pointA.x, pointA.y, 1, 1), lineTex);

        GUI.matrix = matrix;
        GUI.color = savedColor;
    }

    static GUIStyle _blueDot = null;

    public static GUIStyle blueDot
    {
        get
        {
            if (_blueDot == null)
            {
                _blueDot = "sv_label_1";
            }

            return _blueDot;
        }
    }

    public static void RoundRect(ref Rect rect)
    {
        rect.x = Mathf.RoundToInt(rect.x);
        rect.y = Mathf.RoundToInt(rect.y);
        rect.width = Mathf.RoundToInt(rect.width);
        rect.height = Mathf.RoundToInt(rect.height);
    }



    // 处理UV并导出模型
    public static void ExportMesh(CombinMeshEditor cme)
    {
        int w = cme.combinTextureWidth;
        int h = cme.combinTextureHeight;
        for (int i = 0; i < cme.nodeList.Count; ++i)
        {
            CombinNode ti = cme.nodeList[i];
            Matrix2x2 matrix = Matrix2x2.identity.Clone();
            matrix.Rotate(-ti.rotation * Mathf.PI / 180);
            for (int j = 0; j < ti.objects.Count; ++j)
            {
                Mesh mesh = ti.meshs[j];
                string fileDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(mesh));
                string fbxName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mesh)) + "_combin.FBX";
                Vector2[] uvBak = new Vector2[mesh.uv.Length];
                Array.Copy(mesh.uv, uvBak, mesh.uv.Length);
                Vector2[] uv = new Vector2[mesh.uv.Length];

                for (int mi = 0; mi < mesh.uv.Length; ++mi)
                {
                    Vector2 u = Vector2.zero;
                    Vector2 v = mesh.uv[mi];
                    v.x -= 0.5f;
                    v.y -= 0.5f;
                    v = matrix * v;
                    v.x += 0.5f;
                    v.y += 0.5f;

                    u.x = v.x * ti.rect.width / w + ti.rect.x / w;
                    u.y = v.y * ti.rect.height / h + (h - ti.rect.y - ti.rect.height) / h;
                    uv[mi] = u;
                }

                mesh.uv = uv;

                var options = new ExportModelSettingsSerialize();
                options.exportFormat = ExportSettings.ExportFormat.Binary;
                options.include = ExportSettings.Include.Model;
                options.objectPosition = ExportSettings.ObjectPosition.LocalCentered;
                options.exportUnrendered = true;
                ModelExporter.ExportObject(fileDir + "/" + fbxName, ti.objects[j], options);
                mesh.uv = uvBak;
            }
        }
    }

    public delegate Texture2D getTexture(int i);

    public static void CombinTexture(CombinMeshEditor cme, getTexture d, string textureName)
    {
        using (new GameViewSize())
        {
            for (int i = 0; i < cme.nodeList.Count; ++i)
            {
                Material mat = cme.nodeList[i].bakeMat;
                mat.shader = Shader.Find("Unlit/Texture");
                mat.mainTexture = d(i);
            }

            int size = 2048;
            RenderTexture rt = RenderTexture.GetTemporary(size, size, 32, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            camera.targetTexture = rt;
            camera.Render();

            int width = cme.combinTextureWidth; ;
            int height = cme.combinTextureHeight;
            Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);
            RenderTexture.active = rt;
            texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            for (int i = 0; i < cme.nodeList.Count; ++i)
            {
                Material mat = cme.nodeList[i].bakedObject.GetComponent<Renderer>().sharedMaterial;
                mat.shader = Shader.Find("Unlit/BakeAlpha");
            }
            camera.Render();

            Texture2D textureAlpha = new Texture2D(width, height, TextureFormat.ARGB32, false);
            textureAlpha.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            textureAlpha.Apply();

            for (int w = 0; w < width; ++w)
            {
                for (int h = 0; h < height; ++h)
                {
                    Color color = texture2D.GetPixel(w, h);
                    Color alphaColor = textureAlpha.GetPixel(w, h);
                    color.a = alphaColor.r;
                    texture2D.SetPixel(w, h, color);
                }
            }

            texture2D.Apply();

            EditorUtil.ExportMesh(cme);
            RenderTexture.active = null;
            Texture firstTexture = cme.nodeList[0].texture;
            string fileName = cme.nodeList[0].path + "/" + textureName + ".png";
            File.WriteAllBytes(fileName, texture2D.EncodeToPNG());
        }
    }

    public static void CreateMesh(Transform parent, CombinNode ti, float offset)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.layer = LayerMask.NameToLayer("UI");
        ti.bakedObject = quad;
        Transform quadTrans = quad.transform;
        quadTrans.parent = parent;

        quadTrans.localScale = new Vector3(ti.rect.width / 1024.0f, ti.rect.height / 1024.0f, 1);
        quadTrans.localPosition = new Vector3(ti.rect.center.x / 1024.0f - 1, ti.rect.center.y / 1024.0f * -1 + 1, 1 + offset);
        MeshRenderer mr = quad.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Unlit/Texture"));
        mr.material = mat;
        ti.bakeMat = mat;
    }

    // 合并贴图
    public static void BakeTexture(CombinMeshEditor cme)
    {
        if (cme.nodeList.Count == 0)
            return;

        CreateCameraThings();

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.parent = camera.transform;
        quad.transform.localPosition = new Vector3(0, 0, 4);
        quad.transform.localScale = Vector3.one * 2;

        float offset = 0.1f;
        for (int i = 0; i < cme.nodeList.Count; ++i)
        {
            offset += 0.1f;
            CreateMesh(camera.transform, cme.nodeList[i], offset);
        }

        CombinTexture(cme, (i) => { return cme.nodeList[i].texture; }, "CombinTexture");
        if (cme.hadNormal)
        {
            CombinTexture(cme, (i) => { return cme.nodeList[i].textureNormal; }, "CombinNormalTexture");
        }

        if (cme.hadSpecular)
        {
            CombinTexture(cme, (i) => { return cme.nodeList[i].textureNormal; }, "CombinSpecularTexture");
        }

        DestroyCameraThings();
        EditorUtility.DisplayDialog("提示", "模型合并贴图处理完毕！", "确定");
        AssetDatabase.Refresh();
    }

    static Camera camera = null;

    static void CreateCameraThings()
    {
        GameObject destroyObj = GameObject.Find("BakeCamera");
        GameObject.DestroyImmediate(destroyObj);

        GameObject cameraObj = new GameObject("BakeCamera");
        cameraObj.hideFlags = HideFlags.HideAndDontSave;
        cameraObj.transform.position = Vector3.one * 2000;
        cameraObj.transform.rotation = Quaternion.identity;
        camera = cameraObj.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 1;
        camera.farClipPlane = 4;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
        camera.backgroundColor = Color.black;
    }

    static void DestroyCameraThings()
    {
        GameObject destroyObj = GameObject.Find("BakeCamera");
        GameObject.DestroyImmediate(destroyObj);
        camera = null;
    }



    // 之所以这么绕，是因为有些贴图不可读
    public static Texture2D RotTexture90(Texture2D texture, float angle)
    {
        CreateCameraThings();

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.parent = camera.transform;
        quad.transform.localPosition = new Vector3(0, 0, 4);
        quad.transform.localScale = Vector3.one * 2;

        GameObject m = GameObject.CreatePrimitive(PrimitiveType.Quad);
        m.layer = LayerMask.NameToLayer("UI");

        Transform mt = m.transform;
        mt.parent = camera.transform;

        Rect rect = new Rect(0, 0, texture.width, texture.height);

        mt.localScale = new Vector3(rect.width / 1024.0f, rect.height / 1024.0f, 1);
        mt.localRotation = Quaternion.Euler(0, 0, angle);

        Matrix2x2 matrix = Matrix2x2.identity.Clone();
        matrix.Rotate(angle * Mathf.PI / 180);

        rect = matrix * rect;
        rect.x = 0;
        rect.y = 0;

        mt.localPosition = new Vector3(rect.center.x / 1024.0f - 1, rect.center.y / 1024.0f * -1 + 1, 1);

        MeshRenderer mr = m.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Unlit/Texture"));
        mat.mainTexture = texture;
        mr.material = mat;


        RenderTexture rt = RenderTexture.GetTemporary(2048, 2048, 32, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Point;
        camera.targetTexture = rt;
        camera.Render();

        Texture2D texture2D = new Texture2D(texture.height, texture.width, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        texture2D.ReadPixels(new Rect(0, 0, texture.height, texture.width), 0, 0);

        mat = new Material(Shader.Find("Unlit/BakeAlpha"));
        mat.mainTexture = texture;
        mr.material = mat;
        camera.Render();

        Texture2D textureAlpha = new Texture2D(texture.height, texture.width, TextureFormat.ARGB32, false);
        textureAlpha.ReadPixels(new Rect(0, 0, texture.height, texture.width), 0, 0);
        textureAlpha.Apply();

        for (int w = 0; w < texture.height; ++w)
        {
            for (int h = 0; h < texture.width; ++h)
            {
                Color color = texture2D.GetPixel(w, h);
                Color alphaColor = textureAlpha.GetPixel(w, h);
                color.a = alphaColor.r;
                texture2D.SetPixel(w, h, color);
            }
        }

        texture2D.Apply();

        DestroyCameraThings();
        return texture2D;
    }


    //public static Dictionary<T, Texture2D> textureList = new Dictionary<T, Texture2D>();
    public static  void WriteTexture()
    {
        //foreach(var v in t.cl)
        //{
        //    Texture2D texture2D = new Texture2D(0, 0, TextureFormat.ARGB32, false, true);
        //    //texture2D.hideFlags = 61;
        //    texture2D.wrapMode =  TextureWrapMode.Clamp;
        //    ImageConversion.LoadImage(texture2D, Convert.FromBase64String(v.Value));
        //    File.WriteAllBytes("Assets/png/" + v.Key.ToString() + ".png", texture2D.EncodeToPNG());
        //}
    }
}
