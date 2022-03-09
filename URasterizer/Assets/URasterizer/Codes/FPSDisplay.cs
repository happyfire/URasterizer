using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    public float SampleTime = 1f;    

    public int FontSize = 20;
    public Color TextColor = Color.white;

    GUIStyle style;

    int frameCount;
    float timeTotal;
    string textDisplay;

    void Awake(){
        style = new GUIStyle();
        style.fontSize = FontSize;
        style.normal.textColor = TextColor;
    }

    void Start()
    {
        frameCount = 0;
        timeTotal = 0;
        textDisplay = "";
    }

    void Update(){
        ++frameCount;
        timeTotal += Time.unscaledDeltaTime;
        if(timeTotal >= SampleTime){
            float fps = frameCount / timeTotal;
            textDisplay = $"FPS:{fps.ToString("F2")}";
            frameCount = 0;
            timeTotal = 0;
        }
    }

    void OnGUI(){
        GUI.Label(new Rect(10, 10, 200, 100), textDisplay, style);
    }
}
