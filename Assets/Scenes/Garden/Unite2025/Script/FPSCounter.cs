using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    float updateInterval = 0.5f;
    float accum = 0f;
    int frames = 0;
    float timeleft;
    float fps;
    GUIStyle textStyle = new GUIStyle();

    void Start()
    {
        timeleft = updateInterval;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = Color.white;
    }

    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        if (timeleft <= 0f)
        {
            fps = accum / frames;
            timeleft = updateInterval;
            accum = 0f;
            frames = 0;
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(50, 50, 1000, 250), fps.ToString("F2") + " FPS", textStyle);
    }
}