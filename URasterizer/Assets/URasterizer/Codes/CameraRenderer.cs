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
        
        private List<RenderingObject> renderingObjects = new List<RenderingObject>();
        
        private Camera _camera;
        public RenderingConfig _config;

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
            if(false){                
                //手动模型也使用左手系
                var _mesh = new Mesh
                {
                    vertices = new Vector3[] { new Vector3(1f, 0f, 2f), new Vector3(0f, 2f, 2f), new Vector3(-1f, 0f, 2f),
                            new Vector3(1.5f, 0.5f, 1.5f), new Vector3(0.5f, 2.5f, 1.5f), new Vector3(-0.5f, 0.5f, 1.5f)},
                    triangles = new int[] { 0, 2, 1, 3, 5, 4 }
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

            _rasterizer = new Rasterizer(w, h, _config);
            rawImg.texture = _rasterizer.texture;

            var statPanel = this.GetComponent<StatsPanel>();
            if (statPanel != null) {
                _rasterizer.StatDelegate += statPanel.StatDelegate;
            }
        }


        void Render()
        {
            var r = _rasterizer;
            r.Clear(BufferMask.Color | BufferMask.Depth);

            for(int i=0; i<renderingObjects.Count; ++i)
            {
                if (renderingObjects[i].gameObject.activeInHierarchy)
                {
                    r.Draw(renderingObjects[i], _camera);
                }
            }                                 

            r.UpdateFrame();
        }
    }
}