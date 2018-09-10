using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace QDazzle
{
    public class CombinNode
    {
        public Rect rect;
        public Rect drawRect;
        public Vector2 offset = Vector2.zero;
        public Vector2 scale = Vector2.zero;
        public Texture2D texture;
        public Texture2D textureNormal;
        public Texture2D textureSpecular;
        public List<GameObject> objects = new List<GameObject>();
        public List<Mesh> meshs = new List<Mesh>();
        public GameObject bakedObject = null;
        public Material bakeMat = null;
        public string path;
        public float angle = 0;
        Vector2 lastScale = Vector2.zero;
        public float rotation
        {
            get
            {
                return angle;
            }
            set
            {
                if (angle == value)
                    return;
                RotTexture(value - angle);
                angle = value;
                angle %= 360;

                Vector2 center = rect.center;
                float width = rect.width;
                rect.width = rect.height;
                rect.height = width;
                rect.center = center;
            }
        }
        
        void RotTexture(float a)
        {
            texture = EditorUtil.RotTexture90(texture, a);
            if(textureNormal != null)
            {
                textureNormal = EditorUtil.RotTexture90(textureNormal, a);
            }

            if (textureSpecular != null)
            {
                textureSpecular = EditorUtil.RotTexture90(textureSpecular, a);
            }
        }

        public void Draw(CombinMeshEditor cme)
        {
            EditorState state = cme.editorState;
            float zoom = state.zoom;
            drawRect = new Rect(rect);
            drawRect.width /= zoom;
            drawRect.height /= zoom;
            drawRect.x = (drawRect.x + state.panOffset.x) / zoom + offset.x;
            drawRect.y = (drawRect.y + state.panOffset.y) / zoom + offset.y;

            if (cme.focusedNode == this && state.action == EditorState.Action.Scale)
            {
                if(cme.powerOfTwo)
                {
                    if (state.pivot == EditorState.Pivot.LeftTop || state.pivot == EditorState.Pivot.LeftBottom)
                    {
                        scale.x = -1 * (EditorUtil.ToNearPowerOfTwo((int)((drawRect.width - scale.x) * zoom)) / zoom - drawRect.width);
                    }
                    else
                    {
                        scale.x = EditorUtil.ToNearPowerOfTwo((int)((drawRect.width + scale.x) * zoom)) / zoom - drawRect.width;
                    }

                    if (state.pivot == EditorState.Pivot.LeftTop || state.pivot == EditorState.Pivot.RightTop)
                    {
                        scale.y = -1 * (EditorUtil.ToNearPowerOfTwo((int)((drawRect.height - scale.y) * zoom)) / zoom - drawRect.height);
                    }
                    else
                    {
                        scale.y = EditorUtil.ToNearPowerOfTwo((int)((drawRect.height + scale.y) * zoom)) / zoom - drawRect.height;
                    }

                    lastScale = Vector2.Lerp(lastScale, scale, 0.1f);
                }
                else
                {
                    lastScale = scale;
                }
                


                switch (state.pivot)
                {
                    case EditorState.Pivot.LeftTop:
                        {
                            drawRect.width -= lastScale.x;
                            drawRect.height -= lastScale.y;

                            drawRect.x += lastScale.x;
                            drawRect.y += lastScale.y;

                        }; break;
                    case EditorState.Pivot.LeftBottom:
                        {
                            
                            drawRect.x += lastScale.x;

                            drawRect.width -= lastScale.x;
                            drawRect.height += lastScale.y;
                        }; break;
                    case EditorState.Pivot.RightTop:
                        {
                            drawRect.y += lastScale.y;

                            drawRect.width += lastScale.x;
                            drawRect.height -= lastScale.y;
                        }; break;
                    case EditorState.Pivot.RightBottom:
                        {
                            drawRect.width += lastScale.x;
                            drawRect.height += lastScale.y;
                        }; break;
                }
            }

            GUI.DrawTexture(drawRect, texture);
                
        }
    }
}