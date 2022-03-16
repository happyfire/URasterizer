using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    public float SampleTime = 1f;    

    public Text FPSText;

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
        
        if(FPSText != null){
            FPSText.fontSize = FontSize;
            FPSText.color = TextColor;
        }
    }

    void Update(){
        ++frameCount;
        timeTotal += Time.unscaledDeltaTime;
        if(timeTotal >= SampleTime){
            float fps = frameCount / timeTotal;
            textDisplay = $"FPS:{fps.ToString("F2")}";
            frameCount = 0;
            timeTotal = 0;
            if(FPSText != null){
                if(FPSText.fontSize != FontSize){
                    FPSText.fontSize = FontSize;
                }
                if(FPSText.color != TextColor){
                    FPSText.color = TextColor;
                }
                FPSText.text = textDisplay;
            }
        }
    }

    void OnGUI(){
        if(FPSText == null){
            GUI.Label(new Rect(10, 10, 200, 100), textDisplay, style);
        }
    }
}
