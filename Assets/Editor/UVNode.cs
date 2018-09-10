using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor;
using System.Reflection;

namespace QDazzle
{
    public class Element
    {
        public int index = -1;
        public Rect bounds = new Rect();
        public UVNode node;
        public List<int> indices = new List<int>();
        public void AddTriangle(Triangle t)
        {
            for(int i = 0; i < 3; ++i)
            {
                indices.Add(t.p[i]);
            }
        }

        public void AddIndices(List<int> ics)
        {
            this.indices.AddRange(ics);
        }

        public void Build()
        {
            List<Vector2> posList = new List<Vector2>();
            for (int j = 0; j < indices.Count; ++j)
            {
                posList.Add(node.vertices[indices[j]]);
            }

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for(int i = 0; i < posList.Count; ++i)
            {
                Vector2 p = posList[i];
                minX = minX > p.x ? p.x : minX;
                maxX = maxX < p.x ? p.x : maxX;
                minY = minY > p.y ? p.y : minY;
                maxY = maxY < p.y ? p.y : maxY;
            }

            float baseLen = node.luvEditor.rectLength;
            bounds.width = maxX - minX;
            bounds.height = maxY - minY;
            bounds.x = minX;
            bounds.y = 1 - minY - bounds.height;

            bounds.x *= baseLen;
            bounds.y *= baseLen;
            bounds.width *= baseLen;
            bounds.height *= baseLen;
        }

        static Rect tempRect = new Rect();
        public bool IsPointIn(Vector2 p, EditorState state)
        {
            tempRect = GetShowRect();

            if (!tempRect.Contains(p))
            {
                return false;
            }

            float len = node.luvEditor.rectLength;
            Vector2 panOffset = node.luvEditor.editorState.panOffset;
            float zoom = node.luvEditor.editorState.zoom;
            for (int i = 0, iMax = indices.Count;  i < iMax; i += 3)
            {
                Vector3 a = node.vertices[indices[i]];

                a.y = ((1 - a.y) * len + panOffset.y) / zoom;
                a.x = (a.x * len + panOffset.x) / zoom;    
                Vector3 b = node.vertices[indices[i + 1]];
                b.y = ((1 - b.y) * len + panOffset.y) / zoom;
                b.x = (b.x * len + panOffset.x) / zoom;
                Vector3 c = node.vertices[indices[i + 2]];
                c.y = ((1 - c.y) * len + panOffset.y) / zoom;
                c.x = (c.x * len + panOffset.x) / zoom;

                if (EditorUtil.PointInTriangle(p, a, b, c))
                {
                    return true;
                }
            }

            return false;
        }

        public Rect GetShowRect()
        {
            EditorState state = node.luvEditor.editorState;
            Vector2 Offset = node.selectedElementOffset;
            tempRect.x = (bounds.x + state.panOffset.x + Offset.x) / state.zoom;
            tempRect.y = (bounds.y + state.panOffset.y + Offset.y) / state.zoom ;
            tempRect.width = bounds.width / state.zoom;
            tempRect.height = bounds.height / state.zoom;

            return tempRect;
        }

        public bool Intersect(Rect rect)
        {
            Rect rec = GetShowRect();
            return EditorUtil.RectIntersect(rec, rect);
        }

        public void ApplyOffset()
        {
            HashSet<int> idxs = new HashSet<int>();
            for(int i = 0, iMax = indices.Count; i < iMax; ++i)
            {
                idxs.Add(indices[i]);
            }

            Vector2 Offset = node.selectedElementOffset;
            Offset /= node.luvEditor.rectLength;
            Offset.y *= -1;
            foreach ( var i in idxs)
            {
                node.vertices[i] += (Vector3)(Offset);
            }
            Build();
        }

        public void Select(Rect rec, List<int> selectedIndex)
        {
            if(EditorUtil.RectIntersect(GetShowRect(), rec))
            {
                for (int i = 0; i < indices.Count; ++i)
                {
                    Vector3 p = node.vertices[indices[i]];
                    if (EditorUtil.PointInRect(p, rec))
                    {
                        selectedIndex.Add(indices[i]);
                    }
                }
            }
        }

        public void Hide()
        {
            Color hideColor = new Color(1, 1, 1, 0);
            for (int i = 0; i < indices.Count; ++i)
            {
                node.lineColors[indices[i]] = hideColor;
                node.vertexColors[indices[i]] = hideColor;
            }
        }

        public void Show()
        {
            Color showLineColor = Color.green;
            Color showVertexColor = Color.white;
            for (int i = 0; i < indices.Count; ++i)
            {
                node.lineColors[indices[i]] = showLineColor;
                node.vertexColors[indices[i]] = showVertexColor;
            }
        }
    }

    public struct Triangle
    {
        public int[] p;
        public int index;
    }

    public class UVNode
    {
        public Mesh mesh;
        public Mesh lineMesh;
        public Mesh vertexMesh;
        public Mesh trangleMesh;
        public Vector3[] vertices;
        public int[] triangles;
        public Color[] lineColors;
        public Color[] vertexColors;
        public LightUVEditor luvEditor;
        public int hoverElement = -1;
        private int[] Indices;
        Mesh currentElementLineMesh;
        Mesh currentElementVetexMesh;
        public Vector2 selectedElementOffset = Vector2.zero;
        public List<Element> selectedElements = new List<Element>();
        public List<Element> elements = new List<Element>();
        Material _lineMaterial;
        Material lineMaterial
        {
            get
            {
                if (_lineMaterial == null)
                {
                    _lineMaterial = new Material(Shader.Find("Hidden/DrawLine"))
                    {
                        hideFlags = HideFlags.DontSave
                    };
                }

                return _lineMaterial;
            }
        }

        private static FieldInfo _editorScreenPointOffset;
        private static FieldInfo editorScreenPointOffset
        {
            get
            {

                if (_editorScreenPointOffset == null)
                {
                    _editorScreenPointOffset = typeof(GUIUtility).GetField(
                        "s_EditorScreenPointOffset",
                        BindingFlags.Static | BindingFlags.NonPublic);
                }

                return _editorScreenPointOffset;
            }
        }

        Material _vertexMaterial;
        Material vertexMaterial
        {
            get
            {
                if (_vertexMaterial == null)
                {
                    _vertexMaterial = new Material(Shader.Find("Hidden/DrawVertex"))
                    {
                        hideFlags = HideFlags.DontSave
                    };
                }
                return _vertexMaterial;
            }
          
        }

        public void InitMesh()
        {
            Func<Mesh> createMesh = () =>
            {
                return new Mesh()
                {
                    hideFlags = HideFlags.DontSave,
                    indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
                };
            };
            currentElementLineMesh = createMesh();
            currentElementVetexMesh = createMesh();
            lineMesh = createMesh();
            vertexMesh = createMesh();
            Indices = mesh.GetIndices(0);
            vertices = mesh.uv2.Select(point => (Vector3)point).ToArray();
            MeshTopology topology = mesh.GetTopology(0);
            if(topology != MeshTopology.Triangles)
            {
                Debug.Log("error !!!!!!!!!");
            }
            lineColors = new Color[mesh.vertices.Length];
            for (int i = 0; i < lineColors.Length; ++i)
            {
                lineColors[i] = Color.green;
            }

            vertexColors = new Color[mesh.vertices.Length];
            for (int i = 0; i < vertexColors.Length; ++i)
            {
                vertexColors[i] = Color.white;
            }

            RebuildMesh();
            double s = EditorApplication.timeSinceStartup;
            InitInfo();
            Debug.Log((EditorApplication.timeSinceStartup - s) + "  ssss");
        }


        public class Point 
        {
            public List<int> indices = new List<int>();
            public List<Triangle> triangles = new List<Triangle>();
        }

       

        void GetPointsInElement(ref Point p
                                ,ref Dictionary<int, Point> IndexToPoint
                                ,ref Element e
                                ,ref List<Triangle> triangleList
                                ,ref List<int> dealTriangles)
        {
            foreach(var triangle in p.triangles)
            {
                if(dealTriangles.Contains(triangle.index))
                {
                    continue;
                }

                if(triangleList.Contains(triangle))
                {
                    triangleList.Remove(triangle);
                }

                dealTriangles.Add(triangle.index);

                e.AddTriangle(triangle);

                for (int k = 0; k < 3; ++k)
                {
                    Point pp = IndexToPoint[triangle.p[k]];
                    GetPointsInElement(ref pp,ref IndexToPoint,ref e,ref triangleList,ref dealTriangles);
                }
            }
        }

        public float progress = 0;
        private void InitInfo()
        {
            Dictionary<int, Point> IndexToPoint = new Dictionary<int, Point>();
            Dictionary<Vector3, Point> vPoint = new Dictionary<Vector3, Point>();
            List<Point> PointList = new List<Point>();
            int[] triangleIndex = mesh.triangles;
            int triangleIndexCount = mesh.triangles.Length;
            for (int i = 0; i < triangleIndexCount; i ++) 
            {
                int index = triangleIndex[i];
                if (!IndexToPoint.ContainsKey(index))
                {
                    Vector3 vector = vertices[index];
                    Point p = new Point();
                    p.indices.Add(index);

                    if(!vPoint.ContainsKey(vector))
                    {
                        vPoint.Add(vector, p);
                        PointList.Add(p);
                    }
                    else
                    {
                        p = vPoint[vector];
                        p.indices.Add(index);
                    }

                    if(!IndexToPoint.ContainsKey(index))
                    {
                        IndexToPoint.Add(index, p);
                    }
                }
            }

            List<Triangle> triangleList = new List<Triangle>();
            Triangle[] triangles = new Triangle[mesh.triangles.Length / 3];

            for (int i = 0; i < triangleIndexCount; i += 3)
            {
                Triangle triangle = triangles[i / 3];
                triangle.p = new int[3];
                triangle.index = i / 3;
                for (int t = 0; t < 3; ++t)
                {
                    Vector3 v = vertices[triangleIndex[t + i]];
                    triangle.p[t] = triangleIndex[t + i];
                }

                for (int t = 0; t < 3; ++t)
                {
                    Point p = IndexToPoint[triangleIndex[t + i]];
                    p.triangles.Add(triangle);
                }
                triangles[i / 3] = triangle;
            }

            triangleList.AddRange(triangles);
            elements.Clear();

            int elementIndex = 0;
            for (int i = 0; ;)
            {
                if (i >= triangleList.Count)
                    break;

                Element e = new Element();
                e.node = this;
                e.index = elementIndex ++;
                Triangle triangle = triangleList[0];
                triangleList.RemoveAt(0);
                e.AddTriangle(triangle);
                List<int> indices = new List<int>();
                List<int> dealTriangles = new List<int>();
                dealTriangles.Add(triangle.index);

                for (int j = 0; j < 3; ++j)
                {
                    Point p = IndexToPoint[triangle.p[j]];
                    GetPointsInElement(ref p,ref IndexToPoint,ref e,ref triangleList,ref dealTriangles);
                }

                e.Build();
                elements.Add(e);
            }
        }


        public void SelectElement(int index)
        {
            for(int i = 0, iMax = selectedElements.Count; i < iMax; ++i)
            {
                selectedElements[i].Show();
            }

            selectedElements.Clear();
           
               
            if(index >= 0 && index < elements.Count)
            {
                elements[index].Hide();
                selectedElements.Add(elements[index]);
            }

            RebuildSelectedElementMesh();
            ResetColor();
        }

        private void RebuildSelectedElementMesh()
        {
            if (selectedElements.Count == 0)
                return;

            currentElementLineMesh.Clear();
            currentElementVetexMesh.Clear();
            List<int> indices = new List<int>();
            Dictionary<int, int> indexToNewIndex = new Dictionary<int, int>();
            List<Vector3> verts = new List<Vector3>();
            List<int> triangles = new List<int>();

            int index = 0;
            for (int i = 0, iMax = selectedElements.Count; i < iMax; ++i)
            {
                Element e = selectedElements[i];
               
                for (int j = 0, jMax = e.indices.Count; j < jMax; ++j)
                {
                    int cindex = e.indices[j];
                    if (!indexToNewIndex.ContainsKey(cindex))
                    {
                        indexToNewIndex.Add(e.indices[j], index++);
                        verts.Add(vertices[cindex]);
                    }

                    triangles.Add(indexToNewIndex[cindex]);
                }
            }

            Color[] colors = new Color[verts.Count];
            Color redColor = Color.red;
            for(int i = 0; i < colors.Length; ++i)
            {
                colors[i] = redColor;
            }

            int[] meshSegmentIndices;
            HashSet<int> meshVertexIndices;

            BuildSegmentIndices(verts.ToArray(), triangles.ToArray(), out meshSegmentIndices, out meshVertexIndices);
            SetViewMeshForGeometryShader(meshSegmentIndices, meshVertexIndices, currentElementLineMesh, currentElementVetexMesh, verts.ToArray());
            currentElementLineMesh.colors = colors;
            currentElementVetexMesh.colors = colors;
        }
        
        private void RebuildMesh()
        {
            lineMesh.Clear();
            vertexMesh.Clear();
            
            int[] meshSegmentIndices;
            HashSet<int> meshVertexIndices;
            BuildSegmentIndices(vertices, Indices, out meshSegmentIndices, out meshVertexIndices);
            SetViewMeshForGeometryShader(meshSegmentIndices, meshVertexIndices, lineMesh, vertexMesh, vertices);
            vertexMesh.colors = vertexColors;
            lineMesh.colors = lineColors;
        }

        private void ResetColor()
        {
            vertexMesh.colors = vertexColors;
            lineMesh.colors = lineColors;
        }

        private static void BuildSegmentIndices(Vector3[] verts
                                                , int[] Indices
                                                , out int[] subMeshSegmentIndices
                                                , out HashSet<int> subMeshVertexIndices)
        {
            subMeshVertexIndices = new HashSet<int>();
            var indexPairs = new HashSet<int>[verts.Length];

            for (int ti = 0, tiMax = Indices.Length; ti < tiMax; ti += 3)
            {
                int i0 = Indices[ti];
                int i1 = Indices[ti + 1];
                int i2 = Indices[ti + 2];

                var pair0 = indexPairs[i0] ?? (indexPairs[i0] = new HashSet<int>());
                var pair1 = indexPairs[i1] ?? (indexPairs[i1] = new HashSet<int>());
                var pair2 = indexPairs[i2] ?? (indexPairs[i2] = new HashSet<int>());

                pair0.Add(i1);
                pair1.Add(i2);
                pair2.Add(i0);
            }

            var segmentIndices = new List<int>();
            var vertexIndices = new HashSet<int>();

            for (int startIndex = 0; startIndex < indexPairs.Length; ++startIndex)
            {
                HashSet<int> pairs = indexPairs[startIndex];

                if (pairs == null || pairs.Count == 0)
                {
                    continue;
                }

                vertexIndices.Add(startIndex);

                foreach (var endIndex in pairs)
                {
                    segmentIndices.Add(startIndex);
                    segmentIndices.Add(endIndex);
                    vertexIndices.Add(endIndex);
                }
            }

            Array.Clear(indexPairs, 0, indexPairs.Length);

            subMeshSegmentIndices = segmentIndices.ToArray();
            subMeshVertexIndices = vertexIndices;
        }


        private static void SetViewMeshForGeometryShader(
            int[] meshSegmentIndices,
            HashSet<int> meshVertexIndices,
            Mesh lMesh,
            Mesh vMesh,
             Vector3[] verts)
        {
            lMesh.subMeshCount = 1;
            lMesh.vertices = verts;

            int[] segmentIndices = meshSegmentIndices;
            lMesh.SetIndices(segmentIndices, MeshTopology.Lines, 0, false);

            vMesh.subMeshCount = 1;
            vMesh.vertices = verts;

            HashSet<int> vertexIndices = meshVertexIndices;
            vMesh.SetIndices(vertexIndices.ToArray(), MeshTopology.Points, 0, false);
        }


        public void DrawGizmos(LightUVEditor luv)
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];

                Vector3 pa = mesh.vertices[a];
                Vector3 pb = mesh.vertices[b];
                Vector3 pc = mesh.vertices[c];

                Gizmos.DrawLine(pa, pb);
                Gizmos.DrawLine(pa, pc);
                Gizmos.DrawLine(pc, pb);
            }
        }

        private void DrawLineMesh(Matrix4x4 matrix)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Graphics.DrawMeshNow(lineMesh, matrix, 0);
        }

        void DrawQuadsWithPoint(Vector2 p)
        {
            GL.Vertex3(p.x - 3, p.y - 3, 0);
            GL.Vertex3(p.x + 3, p.y - 3, 0);
            GL.Vertex3(p.x + 3, p.y + 3, 0);
            GL.Vertex3(p.x - 3, p.y + 3, 0);
        }

        public bool GetSelectElementWithPoint(Vector2 p)
        {
            for(int i = 0, iMax = elements.Count; i < iMax; ++i)
            {
                Element element = elements[i];
                if(element.IsPointIn(p, luvEditor.editorState))
                {
                    SelectElement(element.index);
                    return true;
                }
            }

            return false;
        }

        List<Element> hoverList = new List<Element>();
        public void Hover(Vector2 p)
        {
            hoverList.Clear();
            for (int i = 0, iMax = elements.Count; i < iMax; ++i)
            {
                Element element = elements[i];
                
                if (element.IsPointIn(p, luvEditor.editorState))
                {
                    hoverList.Add(element);
                }
            }
        }

        public void ApplyOffset()
        {
            for (int i = 0, iMax = selectedElements.Count; i < iMax; i++)
            {
                selectedElements[i].ApplyOffset();
            }

            RebuildSelectedElementMesh();
            RebuildMesh();
            selectedElementOffset = Vector2.zero;
        }

        public void Select(Rect rec)
        {
            selectedElements.Clear();
            List<int> selectedIndex = new List<int>();
            for (int i = 0, iMax = elements.Count; i < iMax; i++)
            {
                Element e = elements[i];
                if(EditorUtil.RectIntersect(e.GetShowRect(), rec))
                {
                    selectedElements.Add(e);
                }
            }

            RebuildSelectedElementMesh();
            RebuildMesh();
        }

        //public void Set

        public void Draw(LightUVEditor luv)
        {
            if (vertices.Length == 0)
                return;
            float zoom = luv.editorState.zoom;
            float len = luv.rectLength;

            using (new DrawMeshScope(luv.editorState.canvasRect, EditorGUIUtility.pixelsPerPoint))
            {
                var offset = (Vector2)editorScreenPointOffset.GetValue(null);
                offset -= GUIUtility.GUIToScreenPoint(Vector2.zero);

                Color lineColor = Color.green;

                lineMaterial.SetColor("_Color", lineColor);
                lineMaterial.SetFloat("_Thickness", 1);
                lineMaterial.SetPass(0);

                Matrix4x4 matrix = Matrix4x4.TRS((luv.editorState.panOffset + Vector2.up * len) / zoom + offset, Quaternion.identity, new Vector3(len / zoom, -len / zoom, 1));
                Graphics.DrawMeshNow(lineMesh, matrix, 0);
                
                vertexMaterial.SetColor("_Color", Color.white);
                vertexMaterial.SetFloat("_Radius", 3);
                vertexMaterial.SetPass(0);
                Graphics.DrawMeshNow(vertexMesh, matrix, 0);
                if (selectedElements.Count > 0)
                {
                    matrix = Matrix4x4.TRS((luv.editorState.panOffset + Vector2.up * len + selectedElementOffset) / zoom + offset, Quaternion.identity, new Vector3(len / zoom, -len / zoom, 1));
                    lineMaterial.SetPass(0);
                    Graphics.DrawMeshNow(currentElementLineMesh, matrix, 0);
                    vertexMaterial.SetPass(0);
                    Graphics.DrawMeshNow(currentElementVetexMesh, matrix, 0);

                    //Rect selectRect = selectedElement.GetShowRect();
                    //EditorGUIUtility.AddCursorRect(selectRect, MouseCursor.MoveArrow);
                    //selectRect.x += offset.x;
                    //selectRect.y += offset.y;
                    //EditorUtil.DrawRectWithQuadsLine(selectRect, EditorColor.SelectGridColor, 2f);
                }
            }
        }
    }
}

