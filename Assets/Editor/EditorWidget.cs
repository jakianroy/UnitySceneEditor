using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QDazzle
{
    public class EditorWidget 
    {
        // 
        public Rect rect = new Rect();
        public int depth = 0;
        public bool MouseActive = false;

        private int mHeight = 10;
        private int mWidth = 10;

        public int Height
        {
            get { return mHeight; }
            set { mHeight = value; }
        }

        public int Width
        {
            get { return mWidth; }
            set { mWidth = value; }
        }

        public Vector2 offset = Vector2.zero;

        public EditorWidget Parent = null;
        
        void Start()
        {

        }

        public virtual void OnDraw()
        {

        }

        public virtual void OnClick()
        {

        }

        public virtual void OnDrag()
        {

        }

        public virtual void OnPress(bool press)
        {

        }

        public void OnUpdate()
        {

        }
    }
}

