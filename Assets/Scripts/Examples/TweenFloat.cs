using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MinorAlchemy;

public class TweenFloat : MonoBehaviour 
{
    public Text textComponent;

    MaTween<float> ft = null;

    public void DoTween()
    {
        if(ft!=null) ft.Stop();
        ft = new MaTween<float>(0, 100, 1, EaseType.CubeInOut);
        ft.Update =  (float val) => {
            Debug.Log(val);
            textComponent.text = val.ToString(); 
        };
        ft.Complete = 
            (float val) => {
                ft.Complete = (float n) => { };
                ft.from = 100;
                ft.to = 0;
                ft.Play(); 
            };
        ft.Play();
        
    }
}
