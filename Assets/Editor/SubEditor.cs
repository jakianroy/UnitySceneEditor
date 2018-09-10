using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace QDazzle
{
    public abstract class SubEditor
    {
        
        public EditorState editorState;
        public float zoom
        {
            get{return editorState.zoom;}
        }

       
        public virtual void OnSceneGUI(SceneView sceneView) { }
        public virtual void OnGUI() { }
        public virtual void Draw() { }
        public virtual void Update() { }
        public virtual Rect GetDrawRect() { return new Rect(); }
        public virtual void Select(Rect rect) { }
    }

}