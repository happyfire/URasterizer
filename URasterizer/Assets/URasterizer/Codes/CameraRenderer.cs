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

        [SerializeField]
        private Light _mainLight;

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
            ProfileManager.BeginSample("CameraRenderer.Render");

            var r = _rasterizer;
            r.Clear(BufferMask.Color | BufferMask.Depth);

            switch (_config.FragmentShaderType)
            {
                case ShaderType.VertexColor:
                    r.CurrentFragmentShader = ShaderContext.FSVertexColor;
                    break;
                case ShaderType.BlinnPhong:
                    r.CurrentFragmentShader = ShaderContext.FSBlinnPhong;
                    break;
                case ShaderType.NormalVisual:
                    r.CurrentFragmentShader = ShaderContext.FSNormalVisual;
                    break;
                default:
                    r.CurrentFragmentShader = ShaderContext.FSBlinnPhong;
                    break;
            }

            ShaderContext.Config = _config;

            var camPos = transform.position;
            camPos.z *= -1;
            ShaderContext.Uniforms.WorldSpaceCameraPos = camPos;            

            var lightDir = _mainLight.transform.forward;
            lightDir.z *= -1;
            ShaderContext.Uniforms.WorldSpaceLightDir = -lightDir;
            ShaderContext.Uniforms.LightColor = _mainLight.color * _mainLight.intensity;
            ShaderContext.Uniforms.AmbientColor = _config.AmbientColor;
            
            for (int i=0; i<renderingObjects.Count; ++i)
            {
                if (renderingObjects[i].gameObject.activeInHierarchy)
                {                    
                    r.Draw(renderingObjects[i], _camera);
                }
            }    
                                                    
            r.UpdateFrame();            

            ProfileManager.EndSample();
        }
    }
}