using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    //Game Time
    public float startTemp = -100f;
    public float endTemp = 0f;
    public float duration = 60f;
    public float currentTemp;
    private float timer;

    //UI
    public TextMeshProUGUI valueText;

    //Ending Screens
    public GameObject timeOverScreen;

    
    void Start()
    {
        currentTemp = startTemp;
        timeOverScreen.SetActive(false);
    }
    void Update()
    {
        if (currentTemp < endTemp)
        {
            timer += Time.deltaTime;
            currentTemp = Mathf.Lerp(startTemp, endTemp, timer / duration);

            if (currentTemp >= endTemp)
            {
                // Enable Time Over Ending Screen
                timeOverScreen.SetActive(true);
                Debug.Log("Time Over Rescue Failed");
            }
        }
        valueText.text = currentTemp.ToString("F0");
    }
}
