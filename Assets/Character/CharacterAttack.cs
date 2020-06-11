using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.HeroEditor.Common.CharacterScripts;

public class CharacterAttack : MonoBehaviour
{
    Transform[] childrenTransforms;
    private Transform rightArm;
    private Transform rightForearm;
    private Transform rightHand;
    Vector2 targetPos;
    public GameObject objWithCastingEffect;
    private CastingEffect castingScript;
    private int castingLayerOrder = 0;
    public List<GameObject> availableMagics;
    private Vector2 lastStaffPoint;
    public float freezeDueCasting = 0f;
    private Vector2 castingTarget;
    private CharacterSounds soundScript;
    public float magicCooldown = 0f;
    private bool performingMagic = false;
    // The ui script which controls the MP interface.
    public MagicControlUI magicControlUIScript;
    private Character heroCharacterScript;
    private CharacterControl charControlScript;
    public RectTransform JoystickCover;
    public Touch castingTouch; // Casting touch for Android.
    public bool isCastingWithTouch = false;

    // Start is called before the first frame update
    void Start()
    {
        childrenTransforms = gameObject.GetComponentsInChildren<Transform>();
        foreach (Transform t in childrenTransforms)
        {
            if (t.name.Equals("ArmR[1]"))
                rightArm = t;
            if (t.name.Equals("HandR"))
                rightHand = t;
            if (t.name.Equals("ForearmR"))
                rightForearm = t;

        }
        castingScript = objWithCastingEffect.GetComponent<CastingEffect>();

        // In order to set the casting effect layer order, we get the weapon and sum it 1.
        int weaponLayerOrder = getWeaponLayerOrder();
        castingLayerOrder = weaponLayerOrder + 1;
        soundScript = GetComponent<CharacterSounds>();
        heroCharacterScript = GetComponent<Character>();
        charControlScript = GetComponent<CharacterControl>();

        // Enable multitouch for Android.
        Input.multiTouchEnabled = true;
    }

    private int getWeaponLayerOrder()
    {
        int result = 0;
        SpriteRenderer weaponSprite = objWithCastingEffect.GetComponentInParent<SpriteRenderer>();
        result = weaponSprite.sortingOrder;
        return result;
    }

    private void Update()
    {
        if (isAndroid())
        {
            targetPos = Camera.main.ScreenToWorldPoint(castingTouch.position);
        }
        else
        {
            targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
       
        if (magicCooldown > 0f)
        {
            magicCooldown -= Time.deltaTime;
        }
        else
        {
            // Swap from casting face to normal if needed.
            if (heroCharacterScript.Expression.Equals("MagicLaunchFace"))
            {
                heroCharacterScript.SetExpression("Default");
            }
        }
    }
    private void FixedUpdate()
    {
       
    }
    public bool isAndroid()
    {
        return (Application.platform == RuntimePlatform.Android);
    }

    public bool isCastingPositionFrozen()
    {
        return freezeDueCasting > 0f;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // If can't attack at all, return.
        if (!charControlScript.canAttack())
            return;

        if (freezeDueCasting > 0f)
        {
            freezeDueCasting -= Time.deltaTime;
            pointAt(castingTarget);
        }

        if (isAndroid())
        {
            isCastingWithTouch = false;
            foreach (Touch touch in Input.touches)
            {
                // Check we're not clicking in the jump button area or joystick covere (for mobile devices).
                if (RectTransformUtility.RectangleContainsScreenPoint(charControlScript.mobileJumpButton.GetComponent<RectTransform>(), touch.position) || RectTransformUtility.RectangleContainsScreenPoint(JoystickCover, touch.position))
                {
                    continue;
                }
                // If we get here, it means it's a touch for casting.
                isCastingWithTouch = true;
                castingTouch = touch;
            }
        }

        if (isAndroid() && !isCastingWithTouch && !performingMagic)
        {
            return;
        }

        // When clicking and can perform magic -> Aim.
        if ((!isAndroid() && Input.GetMouseButton(0) || castingTouch.phase == TouchPhase.Moved || castingTouch.phase == TouchPhase.Stationary) && !isCastingPositionFrozen())
        {
            pointAtCursor();
            if (!performingMagic && canPerformMagic() && magicControlUIScript.hasEnoughMP(magicControlUIScript.fireballCost))
            {
                castingScript.setLayerOrder(castingLayerOrder);
                castingScript.showCast(0);
                soundScript.playCasting();
                performingMagic = true;
            }
            
        }
        // When click is released and magic is to be produced.
        else if ((!isAndroid() && Input.GetMouseButtonUp(0) || castingTouch.phase == TouchPhase.Ended) && canPerformMagic() && magicControlUIScript.hasEnoughMP(magicControlUIScript.fireballCost))
        {
            soundScript.stopSounds();
            castingScript.hideAllCast();
            castMagic();
            performingMagic = false;
        }

    }

    private bool canPerformMagic()
    {
        bool result = true;
        if (magicCooldown > 0f)
            result = false;

        return result;
    }
    private void castMagic()
    {
        // Change character expression.
        AnimationEvents animScript = GetComponentInChildren<AnimationEvents>();
        animScript.SetExpression("MagicLaunchFace");

        // Substract MP from UI
        magicControlUIScript.consumeMp(magicControlUIScript.fireballCost);
        freezeDueCasting = 0.5f;
        // We instantiate a magic fireball!
        GameObject fireballPrefab = availableMagics[0];
        GameObject go = (GameObject)Instantiate(fireballPrefab);
        Fireball fireballScript = go.GetComponent<Fireball>();
        fireballScript.target = targetPos;
        fireballScript.speed = 10f;
        fireballScript.startPosition = lastStaffPoint;

        // Call character control onCastingFinished for a pushback.
        GetComponentInParent<CharacterControl>().castingPushback(targetPos);
        GetComponentInParent<CharacterControl>().onCastingFinished();

        // Assign cooldown to prevent new magic.
        magicCooldown = 1f;
    }
    // Where does the character is aiming at?
    void updateCastingTarget(Vector2 newTarget)
    {
        castingTarget = newTarget;
    }

    void pointAtCursor()
    {

        // Convert mouse position into world coordinates
        // get direction you want to point at
        Vector2 direction = (targetPos - (Vector2)rightArm.transform.position).normalized;
        Debug.DrawRay(rightArm.position, direction);

        // Find the right arm
        //rightArm.LookAt(targetPos);
        //transform.GetChild(0).LookAt(targetPos);
        rightArm.transform.up = direction;
        rightArm.transform.Rotate(0.0f, 0.0f, 180f, Space.Self);
        rightForearm.transform.Rotate(0.0f, 0.0f, -30f, Space.Self);
        rightHand.transform.Rotate(0.0f, 0.0f, -30f, Space.Self);
        //transform.GetChild(0).transform.up = direction;
        lastStaffPoint = objWithCastingEffect.transform.position;
        updateCastingTarget(targetPos);
    }

    void pointAt(Vector2 targ)
    {
        // get direction you want to point at
        Vector2 direction = (targ - (Vector2)rightArm.transform.position).normalized;
        rightArm.transform.up = direction;
        rightArm.transform.Rotate(0.0f, 0.0f, 180f, Space.Self);
        rightForearm.transform.Rotate(0.0f, 0.0f, -30f, Space.Self);
        rightHand.transform.Rotate(0.0f, 0.0f, -30f, Space.Self);
    }

    public bool isPerformingMagic()
    {
        return performingMagic;
    }

}
