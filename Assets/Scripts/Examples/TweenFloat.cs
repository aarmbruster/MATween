using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MinorAlchemy;

public class TweenFloat : MonoBehaviour 
{
    public Text textComponent;
    public Transform cube;
    public MaTween<float> ft = null;

    void Start()
    {
        ft = new MaTween<float>(0, 100, 1, EaseType.CubeInOut);
        ft.Update = (float val) =>
        {
            textComponent.text = val.ToString();
            cube.position = new Vector3(0, val * 0.05f, 0);
        };
        ft.Complete = (float val) =>
        {
            var tmpFrom = ft.from;
            ft.from = ft.to;
            ft.to = tmpFrom;
            ft.Play();
        };
    }

    public void DoTween()
    {
        
        ft.Play(); 
    }

    public void ResumeTween()
    {
        ft.Resume();
    }

    public void PauseTween()
    {
        ft.Pause();
    }

    public void StopTween()
    {
        ft.Stop();
    }
}
