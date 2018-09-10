using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace QDazzle
{
    public class LightUVEditor : SubEditor
    {
        public GameObject focusedObject;
        public Mesh focusedMesh;
        public UVNode node = new UVNode();
        public int rectLength = 1024;
        Rect rectangle = new Rect(0, 0, 1024, 1024);
        static float angle = 90;
        public SelectModel selectMode = SelectModel.Triangle;

        public enum SelectModel
        {
            Point,
            Line,
            Triangle
        }

        public LightUVEditor(EditorState s)
        {
            editorState = s;
            node.luvEditor = this;
        }

        public override void OnGUI()
        {
            GameObject obj = EditorGUILayout.ObjectField(focusedObject, typeof(GameObject), true) as GameObject;
            if (focusedObject != obj)
            {
                MeshFilter mf = obj.GetComponent<MeshFilter>();
                if (mf == null)
                    return;

                Mesh mesh = mf.sharedMesh;
                if (mesh == null || mesh.uv2 == null)
                    return;

                focusedMesh = mesh;
                focusedObject = obj;
                node.mesh = mesh;
                Material mat = new Material(Shader.Find("Unlit/ShowUV"));
                MeshRenderer mr = obj.GetComponent<MeshRenderer>();
                if(mr == null)
                {
                    mr = obj.AddComponent<MeshRenderer>();
                }

                
                mr.sharedMaterial = mat;
                node.InitMesh();
            }

            angle = EditorGUILayout.FloatField(angle);

            if(GUILayout.Button("生成2U"))
            {
                GenerateLightMapUV2(focusedObject);
            }

            if (GUILayout.Button("生成2U"))
            {
                for(int i = 1; i < 100; ++i)
                {
                    Debug.Log("i:" + i + " step:" + 1024 / i);
                }
            }
        }

        private static void GenerateLightMapUV2(GameObject gameObject)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();

            
            if (meshFilter != null && meshRenderer != null)
            {
            
                Mesh meshLightmapped = meshFilter.sharedMesh;
                string path = AssetDatabase.GetAssetPath(meshLightmapped);
                ModelImporter model = AssetImporter.GetAtPath(path) as ModelImporter;
                model.generateSecondaryUV = true;
                UnwrapParam up = new UnwrapParam();
                up.hardAngle = angle;
                Unwrapping.GenerateSecondaryUVSet(meshLightmapped, up);
                AssetDatabase.ImportAsset(path);
            }
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            Event current = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            RaycastHit hit;

            int controlID = GUIUtility.GetControlID(sceneView.GetHashCode(), FocusType.Passive);
            switch (current.GetTypeForControl(controlID))
            {
               
                case EventType.MouseUp:
                    {
                        if (Physics.Raycast(ray, out hit, float.MaxValue, LayerMask.GetMask(new string[] { LayerMask.LayerToName(focusedObject.layer) })))
                        {
                            if (hit.transform == focusedObject.transform)
                            {
                                //node.trangelIndex = hit.triangleIndex;
                            }
                        }
                        current.Use();
                    }
                    break;
               
            }

        }

        private void MoveNode(Vector2 offset)
        {
            if(node.selectedElements.Count > 0)
            {
                node.selectedElementOffset = offset * zoom;;
            }
        }

        public override Rect GetDrawRect()
        {
            return new Rect(editorState.panOffset.x / zoom, editorState.panOffset.y / zoom, rectLength / zoom, rectLength / zoom);
        }


        public override void Draw()
        {
            DrawRectangle();

            if (focusedMesh == null)
                return;

            node.Draw(this);
            //DrawProgress();
            if(Event.current.button == 0)
            {
                node.Hover(Event.current.mousePosition);
            }

            

        }

        public override void Select(Rect rect)
        {
            node.Select(rect);
        }

        void DrawProgress()
        {
            Rect progressRect = new Rect();
            progressRect.width = 100;
            progressRect.height = 20;
            progressRect.x = editorState.canvasRect.width - 100 - 3;
            progressRect.y = editorState.canvasRect.height - 20 - 3;

            EditorUtil.CreateLineMaterial();
            EditorUtil.lineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            EditorUtil.DrawRect(progressRect, Color.green);
            GL.End();

            progressRect.width *= 0.5f;// node.progress;
            GL.Begin(GL.QUADS);
            GL.Color(new Color(1, 0, 0, 0.5f));
            GL.Vertex3(progressRect.x, progressRect.y,0);
            GL.Vertex3(progressRect.x + progressRect.width, progressRect.y, 0);
            GL.Vertex3(progressRect.x + progressRect.width, progressRect.y + progressRect.height, 0);
            GL.Vertex3(progressRect.x, progressRect.y + progressRect.height, 0);
            GL.End();
        }

        void DrawRectangle()
        {
            Rect rect = new Rect(editorState.panOffset.x / zoom, editorState.panOffset.y / zoom, rectLength / zoom, rectLength / zoom);

            string lb = "(0,0)";
            Vector2 size = EditorUtil.GetStringSize(lb);
            Rect labelRect = new Rect();
            labelRect.width = size.x;
            labelRect.height = size.y;
            labelRect.center = new Vector2(rect.x , rect.y + rect.height + size.y / 2);
            GUI.Label(labelRect, lb);

            labelRect.center = new Vector2(rect.x + rect.width, rect.y - size.y / 2);
            GUI.Label(labelRect, "(1,1)");
        }

        public override void Update() { }

        [EventHandlerAttribute(EventType.MouseUp, 50)]
        private static void HandleNodeMouseUp(EditorInputInfo inputInfo)
        {
            if(inputInfo.editorState.action == EditorState.Action.Move)
            {
                LightUVEditor luv = inputInfo.editorState.editor as LightUVEditor;
                luv.node.ApplyOffset();
                inputInfo.editorState.dragOffset = Vector2.zero;
                inputInfo.editorState.dragMouseStart = Vector2.zero;
            }

            inputInfo.editorState.action = EditorState.Action.None;
        }

        [EventHandlerAttribute(EventType.MouseDown, 50)]
        private static void HandleNodeMouseDown(EditorInputInfo inputInfo)
        {
            if (Event.current.button == 0)
            {
                LightUVEditor luv = inputInfo.editorState.editor as LightUVEditor;
                if(luv.node.GetSelectElementWithPoint(inputInfo.inputPos))
                {
                    inputInfo.editorState.action = EditorState.Action.Move;
                    inputInfo.editorState.dragOffset = Vector2.zero;
                    inputInfo.editorState.dragMouseStart = inputInfo.inputPos;
                }
                else
                {
                    luv.node.SelectElement(-1);
                }
            }
        }

        [EventHandlerAttribute(EventType.MouseDrag, 50)]
        private static void HandleNodeDrag(EditorInputInfo inputInfo)
        {
            if(inputInfo.editorState.action == EditorState.Action.Move)
            {
                LightUVEditor luv = inputInfo.editorState.editor as LightUVEditor;
                inputInfo.editorState.dragOffset = inputInfo.inputPos - inputInfo.editorState.dragMouseStart;
                luv.MoveNode(inputInfo.editorState.dragOffset);
            }
        }
    }
}