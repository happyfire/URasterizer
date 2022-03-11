using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace URasterizer
{
    public class CameraRenderer : MonoBehaviour
    {        
        IRasterizer _rasterizer;
        IRasterizer _lastRasterizer;

        CPURasterizer _cpuRasterizer;
        GPURasterizer _gpuRasterizer;

        public RawImage rawImg;        
        
        private List<RenderingObject> renderingObjects = new List<RenderingObject>();
        
        private Camera _camera;

        [SerializeField]
        private Light _mainLight;

        public RenderingConfig _config;

        StatsPanel _statsPanel;

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

            _cpuRasterizer = new CPURasterizer(w, h, _config);
            _gpuRasterizer = new GPURasterizer(w, h, _config);

            _statsPanel = this.GetComponent<StatsPanel>();
            if (_statsPanel != null) {
                _cpuRasterizer.StatDelegate += _statsPanel.StatDelegate;
                _gpuRasterizer.StatDelegate += _statsPanel.StatDelegate;
            }
        }


        void Render()
        {
            ProfileManager.BeginSample("CameraRenderer.Render");

            if(_config.RasterizerType == RasterizerType.GPUDriven && _config.ComputeShader != null){
                _rasterizer = _gpuRasterizer;                
            }
            else{
                _rasterizer = _cpuRasterizer;                
            }

            if(_rasterizer != _lastRasterizer){
                _lastRasterizer = _rasterizer;
                
                rawImg.texture = _rasterizer.ColorTexture;
                _statsPanel.SetRasterizerType(_rasterizer.Name);   
            }

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