using UnityEngine;
using TMPro;
using UnityEngine.InputSystem.Controls;
public class Timer : MonoBehaviour
{

    public TextMeshProUGUI text;
    public float time = 100f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        text.text = "" + time;
    }

    // Update is called once per frame
    void Update()
    {
        time -= Time.deltaTime;
        text.text = "" + time;
        if (time < 50)
        {
            if((int)time % 10 % 2 == 0)
            {
                text.color = Color.red;
            }
            else if((int)time % 10 % 3 == 0)
            {
                text.color = Color.antiqueWhite;
            }
            else if( (int)time % 10 == 7)
            {
                text.color = Color.purple;
            }
            else
            {
                text.color = Color.lavenderBlush;
            }
        }
            
    }
}
