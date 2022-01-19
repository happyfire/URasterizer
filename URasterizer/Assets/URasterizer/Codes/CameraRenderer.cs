using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace URasterizer
{
    public class CameraRenderer : MonoBehaviour
    {        
        Rasterizer _rasterizer;

        public RawImage rawImg;

        public Color ClearColor = Color.black;
        
        private List<RenderingObject> renderingObjects = new List<RenderingObject>();
        
        private Camera _camera;        
        public bool WireframeMode;

        private void Start()
        {
            Init();
        }

        private void OnPostRender()
        {
            Render();
        }        

        void Init()
        {
            _camera = GetComponent<Camera>();

            var rootObjs = this.gameObject.scene.GetRootGameObjects();
            renderingObjects.Clear();
            foreach(var o in rootObjs)
            {
                renderingObjects.AddRange(o.GetComponentsInChildren<RenderingObject>());
            }
            
            Debug.Log($"Find rendering objs count:{renderingObjects.Count}");
            

            //手动设置的mesh
            {                
                var _mesh = new Mesh
                {
                    vertices = new Vector3[] { new Vector3(1f, 0f, -2f), new Vector3(0f, 2f, -2f), new Vector3(-1f, 0f, -2f),
                            new Vector3(1.5f, 0.5f, -1.5f), new Vector3(0.5f, 2.5f, -1.5f), new Vector3(-0.5f, 0.5f, -1.5f)},
                    triangles = new int[] { 0, 1, 2, 3, 4, 5 }
                };
                var go = new GameObject("_handmake_mesh_");
                var ro = go.AddComponent<RenderingObject>();
                ro.mesh = _mesh;
                go.AddComponent<MeshFilter>().mesh = _mesh;
                go.AddComponent<MeshRenderer>();

                renderingObjects.Add(ro);
            }

            RectTransform rect = rawImg.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Screen.width, Screen.height);
            int w = Mathf.FloorToInt(rect.rect.width);
            int h = Mathf.FloorToInt(rect.rect.height);
            Debug.Log($"screen size: {w}x{h}");

            _rasterizer = new Rasterizer(w, h);
            rawImg.texture = _rasterizer.texture;
            _rasterizer.ClearColor = ClearColor;
        }


        void Render()
        {
            var r = _rasterizer;
            r.Clear(BufferMask.Color | BufferMask.Depth);

            for(int i=0; i<renderingObjects.Count; ++i)
            {
                if (renderingObjects[i].gameObject.activeInHierarchy)
                {
                    r.Draw(renderingObjects[i], _camera, WireframeMode);
                }
            }                                 

            r.UpdateFrame();
        }
    }
}