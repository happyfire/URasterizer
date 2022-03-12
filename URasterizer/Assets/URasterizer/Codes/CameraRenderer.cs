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

        bool _lastUseUnityNativeRendering;

        private void Start()
        {
            Init();
            _lastUseUnityNativeRendering = _config.UseUnityNativeRendering;
            OnOffUnityRendering();
        }

        void OnOffUnityRendering()
        {
            if(_config.UseUnityNativeRendering){
                rawImg.gameObject.SetActive(false);
                _camera.cullingMask = 0xfffffff;
                _statsPanel.SetRasterizerType("Unity Native");
            }
            else{
                rawImg.gameObject.SetActive(true);
                _camera.cullingMask = 0;
                if(_rasterizer!=null){
                    _statsPanel.SetRasterizerType(_rasterizer.Name);
                }
            }
        }

        private void OnPostRender()
        {           
            if(!_config.UseUnityNativeRendering){
                Render();
            }   

            if(_lastUseUnityNativeRendering != _config.UseUnityNativeRendering){
                OnOffUnityRendering();                                
                _lastUseUnityNativeRendering = _config.UseUnityNativeRendering;
            }            
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

            r.SetupUniforms(_camera, _mainLight);
                        
            
            for (int i=0; i<renderingObjects.Count; ++i)
            {
                if (renderingObjects[i].gameObject.activeInHierarchy)
                {                    
                    r.DrawObject(renderingObjects[i]);
                }
            }    
                                                    
            r.UpdateFrame();            

            ProfileManager.EndSample();
        }
    }
}