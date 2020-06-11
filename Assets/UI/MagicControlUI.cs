using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MagicControlUI : MonoBehaviour
{
    public Sprite fireballSprite;
    public Sprite healSprite;
    public float maxMp;
    public float currentMp;
    public GameObject MpCircle;
    public float MPRecoveryPerSecond;
    public float fireballCost = 20;
    public float healCost = 20;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (currentMp < maxMp)
        {
            currentMp += MPRecoveryPerSecond * Time.deltaTime;
        }
        updateMagicCircle();
    }
    private void updateMagicCircle()
    {
        // Get the image from the circle.
        Image mpImage = MpCircle.GetComponent<Image>();
        mpImage.fillAmount = currentMpToFillAmount();
    }
    // Mapping the current MP value to the fill amount.
    float currentMpToFillAmount()
    {
        // Fill amount goes from 0 - 1.0f
        // Get current mp percentage (decimal) out of maximum.
        float percentage = currentMp / maxMp;
        float currentFill = percentage;
        return currentFill;
    }
    public bool hasEnoughMP(float requestedMp)
    {
        if (currentMp >= requestedMp)
        {
            return true;
        }
        else
            return false;
    }
    public void consumeMp(float substraction)
    {
        currentMp -= substraction;
    }
}
