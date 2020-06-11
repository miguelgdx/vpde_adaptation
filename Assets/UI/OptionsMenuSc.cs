using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsMenuSc : MonoBehaviour
{
    public Slider QualitySlider;
    public TextMeshProUGUI QualityText;
    // Start is called before the first frame update
    
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnQualityChange() {
        switch (QualitySlider.value)
        {
            case 0:
                QualityText.text = "LOW";
                break;
            case 1:
                QualityText.text = "MEDIUM";
                break;
            case 2:
                QualityText.text = "HIGH";
                break;
        }
        Debug.Log("Quality set to: " + (int)QualitySlider.value);
        QualitySettings.SetQualityLevel((int)QualitySlider.value, true);

    }

}
