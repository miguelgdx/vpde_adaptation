using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthControlUI : MonoBehaviour
{
    public float maxHp;
    public float currentHp;
    public GameObject healthBar;
    private RectTransform rt;
    // Start is called before the first frame update
    void Start()
    {
        rt = healthBar.GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
    }
    // Move the health bar sprite according to the current value.
    private void updateHealthUI()
    {
        rt.localPosition = new Vector3(mapHpToUI(), rt.localPosition.y, rt.localPosition.z);
    }
    // Mapping the current MP value to the fill amount.
    float mapHpToUI()
    {
        // Fill amount goes from 0 - 1.0f
        // Get current mp percentage (decimal) out of maximum.
        float percentage = currentHp / maxHp;
        float currentFill = percentage * rt.rect.width;
        Debug.Log("Current fill: " + currentFill);
        return -(rt.rect.width - currentFill);
    }
    public void consumeHp(float substraction)
    {
        currentHp -= substraction;
        updateHealthUI();
    }
}
