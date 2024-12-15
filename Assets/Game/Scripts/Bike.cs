using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Bike : MonoBehaviour
{
    public AudioSource PedalAudio;
    public AudioSource SwerveAudio;
    public AudioSource Jump1Audio;
    public AudioSource Jump2Audio;
    public AudioSource Jump3Audio;
    public AudioSource JumpFallAudio;
    public AudioSource ScreechAudio;
    public AudioSource LandThudAudio;
    public AudioSource WheelThud0Audio;
    public AudioSource WheelThud1Audio;
    public AudioSource WheelThud2Audio;
    public AudioSource WheelThud3Audio;
    public AudioSource WindAudio;
 
    public float BikeCyclePitchScale = 0.2f;
    public float BikeCyclePitchMinSpeed = 0.4f;

    public ParticleSystem CrashParticlesPrefab; // world
    public ParticleSystem CrashWallParticlesPrefab; // world
    public ParticleSystem SwerveParticlesPrefab; // world
    public ParticleSystem CollectBronzeParticlesPrefab; // world
    public ParticleSystem CollectSilverParticlesPrefab; // world
    public ParticleSystem CollectGoldParticlesPrefab; // world
    public ParticleSystem CollectFlagYellowParticlesPrefab; // world
    public ParticleSystem CollectFlagRedParticlesPrefab; // world
    public ParticleSystem CollectFlagBlueParticlesPrefab; // world
    public ParticleSystem CollectFlagCoinParticlesPrefab; // world
    public ParticleSystem WindParticlesPrefab;  // local to bike

    public GameObject FinishShine;


    // Object Refs
    public GameObject GameBikeBase;
    public GameObject FrontWheel;
    public GameObject BackWheel;
    public GameObject HandleBars;
    public GameObject FrontWheelProj;
    public GameObject BackWheelProj;

    // Gui Params
    public RectTransform SpeedGaugeAdjuster;

    // Bike Params
    public float BikeAccel = 20;
    public float BikeDecel = 5;
    public float BikeSwerveDecel = .05f; // Should be greater than BikeAccel, since we are overcompensating.
    public float BikeMaxSpeed = 14;
    public float BikeMaxSpeed2 = 32;
    public float BikeMaxSpeedGaugePercent = 70;
    public float BikeTurn = 1.5f;
    public float BikeTurnTranslation = .25f;
    public float BikeHandleTurn = 3;
    public float BikeSteerScreenWidth = 50;
    public float BikeSteerSwipeScale = .1f;
    public float BikeSteerPercentEasyJump = .7f;
    public int BikeSteerSwipeScaleLimit = 20;

    public float BikeJumpStartSpeed = 12;
    public float BikeJumpDecelRise = 40;
    public float BikeJumpDecelFall = 20;
    public float BikeJumpDecelForward = 4;
    public float BikeJumpLandingSlowdown = 2;

    public float BikeJumpTilt = -10;
    public float BikeJumpTiltThreshold = .8f;
    public float BikeJumpSailTimeout = 1.2f;
    public float BikeJump2Wait = .25f;
    public float BikeJump2Timeout = .5f; // should be more than wait
    public float BikeJump2SpeedBoost = 8.4f;
    public float BikeJump2DecelRise = 20;
    public float BikeJump2DecelFall = 10;
    public float BikeJump3Wait = .4f;
    public float BikeJump3Timeout = 1; // should be more than wait
    public float BikeJump3SpeedBoost = 12;
    public float BikeJump3DecelRise = 20;
    public float BikeJump3DecelFall = 11;
    public float BikeJumpUntiltSpeed = 44;

    public float BikeJumpFlipSpeed = -100;
    public float BikeJumpFlipAmount = 90;
    public float BikeJumpFlipSpeedBoost = 12;
    public float BikeJumpFlipDecelRise = 0.01f;
    public float BikeJumpFlipDecelFall = 0.01f;

    public float BikeWheelSpeedVisualFactor = 80;


    public int JumpCount { get; set; }

    public bool IsReset { get; set; }
    public bool IsRestarting { get; set; }
    public bool IsFinished { get; set; }
    public bool IsCrashed { get; set; }



    // Bike Controls
    float originalBikeEulerY;
    float originalHandleBarEulerY;
    float bikeSpeed = 0f;
    float bikeJumpSpeed = 0;
    bool isJumping = false;
    bool isSailing = false;
    bool isOkToScreechAgain = true;
    float sailElapsed = 0f;
    float jumpElapsed = 0f;
    float savedBikeTilt = 0f;
    bool swerveApplied = false;
    int wheelLevel = 0; // 0 = same, 1 = front down, 2 = back down too, 3 = front up 

    public void Jumped1()
    {
        Jump1Audio.Play();
    }

    public void Jumped2()
    {
        Jump2Audio.Play();
    }

    public void Jumped3()
    {
        Jump3Audio.Play();
    }

    public void JumpedFall()
    {
        JumpFallAudio.Play();
    }

    public void Screech()
    {
        if(!ScreechAudio.isPlaying)
            ScreechAudio.Play();
    }

    public void LandThud()
    {
        LandThudAudio.Play();
    }
    public void WheelThud(int wheelLevel)
    {
        switch(wheelLevel)
        {
            case 0:
                WheelThud0Audio.Play();
                break;
            case 1:
                WheelThud1Audio.Play();
                break;
            case 2:
                WheelThud2Audio.Play();
                break;
            case 3:
                WheelThud3Audio.Play();
                break;
        }
   
    }

    public void Pedaled(float bikeSpeed)
    {
        if (!PedalAudio.isPlaying)
        {
            if (bikeSpeed > BikeCyclePitchMinSpeed)
            {
                PedalAudio.pitch = BikeCyclePitchScale * bikeSpeed;
                PedalAudio.Play();
            }
        }
    }

    

    public void StartWind()
    {
        WindParticlesPrefab.Play();
    }

    public void StopWind()
    {
        WindParticlesPrefab.Stop();
    }

    public void StartShine()
    {
        FinishShine.SetActive(true);
    }

    public void StopShine()
    {
        FinishShine.SetActive(false);
    }

    public void StartDance()
    {
        
    }

    public void StopDance()
    {

    }

    public void ResetRotation()
    {
        Vector3 currentHandleEulers = HandleBars.transform.localEulerAngles;
        currentHandleEulers.y = originalHandleBarEulerY;
        HandleBars.transform.localEulerAngles = currentHandleEulers;

        transform.localEulerAngles = Vector3.zero;
    }

    public void ResetPosition()
    {
        transform.localPosition = Vector3.zero;
    }

    public void Stop()
    {
        bikeSpeed = 0f;
        jumpElapsed = 0f;
        sailElapsed = 0f;
        swerveApplied = false;
    }

    public void Crash()
    {
        IsCrashed = true;
        swerveApplied = false;
        Stop();
        Game.Inst.BikeCrashed();
    }

    public void FinishStart()
    {
        Stop();
        IsFinished = true;
        StartShine();
        StartDance();
    }

    public void FinishEnd()
    {
        StartCoroutine(ResetEverything());
    }

    public void Swerve()
    {
        swerveApplied = true;
        //Debug.LogError("Swerve Applied... bikeSpeed is: " + bikeSpeed);
        bikeSpeed = BikeMaxSpeed2;
    }

    void Start()
    {
        originalBikeEulerY = transform.localEulerAngles.y;
        originalHandleBarEulerY = HandleBars.transform.localEulerAngles.y;
    }

    //////////////// BIKE UPDATE

    void Update()
    {
        // Cache some values.
        float dt = Time.deltaTime;
        //Vector3 mousePos = Input.mousePosition;
        float pixelsAwayFromMiddle = 0;
        float fromMiddle = 0;
        Vector3 oldPos = transform.localPosition;

        if (IsCrashed)
            return;

        else if (IsRestarting)
            return;

        else if (IsFinished)
            return;

        float newGaugeHeight;
        if (bikeSpeed < BikeMaxSpeed)
        {
            // partial up to BikeMaxSpeed
            newGaugeHeight = SpeedGaugeAdjuster.rect.height * ((BikeMaxSpeedGaugePercent / 100f) * (bikeSpeed / BikeMaxSpeed));
        }
        else
        {
            newGaugeHeight = SpeedGaugeAdjuster.rect.height * (BikeMaxSpeedGaugePercent / 100f) + SpeedGaugeAdjuster.rect.height * ((1f - BikeMaxSpeedGaugePercent / 100f) * ((bikeSpeed - BikeMaxSpeed) / (BikeMaxSpeed2 - BikeMaxSpeed)));
        }

        SpeedGaugeAdjuster.localPosition = new Vector3(0, newGaugeHeight, 0);

        // Bike chug sound mechanism.
        if (!isJumping && bikeSpeed > 2f)
        {
            Pedaled(bikeSpeed);
        }

#if UNITY_TOUCH_SUPPORTED
        if (Input.touchCount > 0 && Input.touches[0].phase != TouchPhase.Canceled)
#else
        if (Input.GetMouseButton(0)) // button is being held down now
#endif
        {
            // Accel & Steering (pressed while not in air).
            if (!isJumping)// && mousePos.x > screenMiddleBorderLeft && mousePos.x < screenMiddleBorderRight)
            {
                if (bikeSpeed > 0 && isOkToScreechAgain)
                {
                    Screech();
                    isOkToScreechAgain = false;
                }
                if (bikeSpeed < 3.5f)
                {
                    isOkToScreechAgain = true;
                }


#if UNITY_TOUCH_SUPPORTED
                Touch firstTouch = Input.GetTouch(0);
                Vector2 contactPos = firstTouch.position;
                Vector2 contactDelta = firstTouch.deltaPosition;
#else
                Vector2 contactPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
#endif
                pixelsAwayFromMiddle = contactPos.x - Screen.width / 2;

                //Debug.Log($"Accel contact from mid: {pixelsAwayFromMiddle}             X: {contactPos.x}");
                fromMiddle = Mathf.Sign(pixelsAwayFromMiddle) * Mathf.Min(BikeSteerSwipeScaleLimit, BikeSteerSwipeScale * Mathf.Abs(pixelsAwayFromMiddle));

                Vector3 currentHandleEulers = HandleBars.transform.localEulerAngles;
                currentHandleEulers.y = originalHandleBarEulerY - BikeHandleTurn * fromMiddle; // rot y is opposite of bike direction so need minus sign
                HandleBars.transform.localEulerAngles = currentHandleEulers;

                Vector3 currentBikeEulers = transform.localEulerAngles;
                currentBikeEulers.y = originalBikeEulerY + BikeTurn * fromMiddle;
                transform.localEulerAngles = currentBikeEulers;

                if (swerveApplied)
                {
                    bikeSpeed -= BikeSwerveDecel * dt;

                    if (bikeSpeed < BikeMaxSpeed)
                    {
                        bikeSpeed = BikeMaxSpeed;
                        swerveApplied = false;
                    }
                }
                else
                {
                    bikeSpeed += BikeAccel * dt;

                    if (bikeSpeed > BikeMaxSpeed)
                        bikeSpeed = BikeMaxSpeed;
                }

            }
        }

#if UNITY_TOUCH_SUPPORTED
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended)
#else
        if (Input.GetMouseButtonUp(0)) // button is let go now
#endif
        {
            if (!isJumping)
            {
                Jumped1();
                isJumping = true;
                JumpCount++;
                bikeJumpSpeed = BikeJumpStartSpeed;
            }
        }

        // Jumping sail.
        if (isJumping)
        {
            jumpElapsed += dt;
            //Debug.Log("jumpElapsed: " + jumpElapsed);
            if (JumpCount == 1 && jumpElapsed > BikeJump2Wait && Input.GetMouseButtonDown(0) && jumpElapsed < BikeJump2Timeout)
            {
                Jumped2();
                bikeJumpSpeed += BikeJump2SpeedBoost;
                JumpCount++;
                jumpElapsed = 0;
            }
            else if (JumpCount == 2 && jumpElapsed > BikeJump3Wait && Input.GetMouseButtonDown(0) && jumpElapsed < BikeJump3Timeout)
            {
                Jumped3();
                bikeJumpSpeed += BikeJump3SpeedBoost;
                JumpCount++;
                jumpElapsed = 0;
                savedBikeTilt = BikeJumpTilt * transform.localPosition.y;

                // Special FX since this is the 3RD jump after all...
                StartWind();
            }
            else if (sailElapsed > BikeJump2Timeout && sailElapsed < BikeJumpSailTimeout &&
#if UNITY_TOUCH_SUPPORTED
                Input.touchCount > 0 && Input.touches[0].phase != TouchPhase.Canceled)
#else
                Input.GetMouseButton(0)) // button is being held down now
#endif
            {
                if (!isSailing)
                {
                    //JumpedFall();
                    //StartWind();
                }

                isSailing = true;
            }
            else
            {
                if (isSailing)
                {
                    StopWind();
                }

                isSailing = false;
            }
        }
        else
        {
            // Grounded and slowing down.
            bikeSpeed -= BikeDecel * dt;
            if (bikeSpeed < 0)
                bikeSpeed = 0;

            // Tilt bike if wheels are on different levels.
            int layerMask = LayerMask.GetMask("Terrain");
            Ray frontRay = new Ray(FrontWheel.transform.position, Vector3.down);
            if (Physics.Raycast(frontRay, out RaycastHit frontHit, Mathf.Infinity, layerMask))
            {
                FrontWheelProj.transform.position = frontHit.point;
            }
            Ray backRay = new Ray(BackWheel.transform.position, Vector3.down);
            if (Physics.Raycast(backRay, out RaycastHit backHit, Mathf.Infinity, layerMask))
            {
                BackWheelProj.transform.position = backHit.point;
            }

            Vector3 eulers = transform.localRotation.eulerAngles;
            eulers.x = -(90f - (180f / Mathf.PI) * (Mathf.Atan2(frontHit.point.z - backHit.point.z, frontHit.point.y - backHit.point.y)));
            transform.localRotation = Quaternion.Euler(eulers);

            // TODO: Fix hard code 0.25
            GameBikeBase.transform.localPosition = new Vector3(0,
                0.25f + (BackWheelProj.transform.position.y + FrontWheelProj.transform.position.y) / 2f, 0);

            int oldWheelLevel = wheelLevel;

            if (BackWheelProj.transform.position.y - FrontWheelProj.transform.position.y > 0.01f)
                wheelLevel = 1;
            else if (FrontWheelProj.transform.position.y - BackWheelProj.transform.position.y > 0.01f)
                wheelLevel = 2;
            else
                wheelLevel = 0;

            if (wheelLevel != oldWheelLevel)
            {
                // first tire down
                if (wheelLevel == 1)
                    WheelThud(0);
                // back tire down too
                else if (wheelLevel == 0 && oldWheelLevel == 1)
                    WheelThud(1);
                // first tire up
                else if (wheelLevel == 2)
                    WheelThud(2);
                // back tire up too
                else if (wheelLevel == 0 && oldWheelLevel == 2)
                    WheelThud(3);

            }
        }

        // Jumping Vertical Accel/Decel.
        if (isJumping)
        {
            transform.localPosition += new Vector3(0, dt * bikeJumpSpeed, 0);

            if (transform.localPosition.y > oldPos.y) // rising
            {
                if (JumpCount == 1)
                    bikeJumpSpeed += dt * -BikeJumpDecelRise;
                else if (JumpCount == 2)
                    bikeJumpSpeed += dt * -BikeJump2DecelRise;
                else if (JumpCount == 3)
                    bikeJumpSpeed += dt * -BikeJump3DecelRise;
            }
            else // falling
            {
                if (isSailing)
                {
                    sailElapsed += dt;
                    //if (sailElapsed > BikeJumpSailTimeout)
                    //    isSailing = false;
                }
                else
                {
                    if (JumpCount == 1)
                        bikeJumpSpeed += dt * -BikeJumpDecelFall;
                    else if (JumpCount == 2)
                        bikeJumpSpeed += dt * -BikeJump2DecelFall;
                    else if (JumpCount == 3)
                        bikeJumpSpeed += dt * -BikeJump3DecelFall;

                    bikeSpeed -= BikeJumpDecelForward * dt;
                    if (bikeSpeed < 0)
                        bikeSpeed = 0;
                }
            }

            // Tilting back.
            float bikeTilt = 0;
            if (transform.localPosition.y > BikeJumpTiltThreshold || JumpCount == 3)
            {
                if (JumpCount == 3)
                {
                    bikeTilt = savedBikeTilt; // Tilt down more for the 3rd one.
                    savedBikeTilt += BikeJumpUntiltSpeed * dt; // Will continue to keep tilting forward.
                }
                else
                    bikeTilt = BikeJumpTilt * transform.localPosition.y;
            }

            // Hitting the ground.            
            if (transform.localPosition.y < 0)
            {
                transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
                bikeJumpSpeed = 0f;
                isJumping = false;
                JumpCount = 0;
                bikeTilt = 0f;
                savedBikeTilt = 0f;
                jumpElapsed = 0f;
                sailElapsed = 0f;
                bikeSpeed -= BikeJumpLandingSlowdown; // Skitter slow as you hit the ground.
                if (bikeSpeed < 0f)
                    bikeSpeed = 0f;

                StopWind();

                if (!IsReset)
                {
                    LandThud();
                }
            }

            // Apply jump tilt (if any).
            Vector3 currentBikeEulers = transform.localEulerAngles;
            currentBikeEulers.x = bikeTilt;
            transform.localEulerAngles = currentBikeEulers;
        }

        
        // Moving.
        if (bikeSpeed > 0)
        {
            IsReset = false;

            transform.localPosition += new Vector3(dt * BikeTurnTranslation * fromMiddle, 0, dt * bikeSpeed);// + transform.position.z);

            FrontWheel.transform.localRotation *= Quaternion.Euler(dt * BikeWheelSpeedVisualFactor * bikeSpeed, 0, 0);
            BackWheel.transform.localRotation *= Quaternion.Euler(dt * BikeWheelSpeedVisualFactor * bikeSpeed, 0, 0);
        }
        

    }


    //////////////// BIKE COLLISION ENTER
 
    void OnCollisionEnter(Collision collision)
    {
        // If already crashed, don't be adding other things.
        if (IsCrashed)
            return;

        GameObject otherOb = collision.collider.gameObject;
        GameObject parentOfOtherOb = otherOb.gameObject.transform.parent.gameObject;
        string obName = otherOb.name;
        string parentName = parentOfOtherOb.name;

        // CAPSULE
        if (collision.collider is CapsuleCollider)
        {
            // HIT!!!!!!!!!

            // Looks better for a flagpole if the crash explosion is sitting directly in line with the pole.
            float xOffset = 0;
            /* TODO address this without using tags.
            if (collision.gameObject.tag.Contains("Flag"))
                if (transform.position.x < collision.transform.position.x)
                    xOffset = -1f;
                else
                    xOffset = 1f;
            */

            // Object sounds from being hit!
            if (parentOfOtherOb.name.Contains("animal"))
                parentOfOtherOb.GetComponent<AudioSource>().PlayDelayed(0.3f);
                
            CrashParticlesPrefab.transform.position = collision.transform.position + new Vector3(xOffset, 1f, -0.5f);
            CrashParticlesPrefab.Play();
            CrashParticlesPrefab.transform.GetChild(1).gameObject.SetActive(true); // Child particle too.
            CrashParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
            Crash();
            StartCoroutine(StopEverything());
        }

        // BOX
        else if(collision.collider is BoxCollider)
        {
            // HIT WALL
            if(obName.Contains("race_track"))
            {
                float xOffset = 0;
                if (transform.position.x < collision.transform.position.x)
                    xOffset = -0.7f;
                else
                    xOffset = 0.7f;

                CrashWallParticlesPrefab.transform.position = transform.position + new Vector3(xOffset, 0, 0);
                CrashWallParticlesPrefab.Play();
                CrashWallParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                Crash();
                StartCoroutine(StopEverything());
            }
            // FLAG COLLECT
            else if (obName.Contains("Flag"))
            {
                Vector3 flagFxPosition = transform.localPosition + new Vector3(0, 1.7f, 0); // add a little height

                if (obName.Contains("Yellow"))
                {
                    CollectFlagYellowParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagYellowParticlesPrefab.Play();
                    CollectFlagYellowParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();

                    // Disable flag and hit box now that we've collected.
                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.CollectFlag(0);
                }
                else if (obName.Contains("Red"))
                {
                    CollectFlagRedParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagRedParticlesPrefab.Play();
                    CollectFlagRedParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();

                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.CollectFlag(1);
                }
                else if (obName.Contains("Blue"))
                {
                    CollectFlagBlueParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagBlueParticlesPrefab.Play();
                    CollectFlagBlueParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();

                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.CollectFlag(2);
                }
                else if (obName.Contains("Coin"))
                {
                    CollectFlagCoinParticlesPrefab.transform.localPosition = flagFxPosition;
                    CollectFlagCoinParticlesPrefab.Play();
                    CollectFlagCoinParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();

                    otherOb.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                    otherOb.GetComponent<BoxCollider>().enabled = false;

                    Game.Inst.StartAtm();
                }
            }
            // OTHER OBJECTS
            else if(obName.Contains("carDanger"))
            {
                GameObject carOb = otherOb.transform.parent.gameObject;
                Game.Inst.CarDanger(carOb);
            }
            
        }    
    
        // MESH COLLIDER - Since BikeCenter (hitter) is subobject of rigidbody in question, the collision
        // shape will make it into this method against the collider (hittee).
        else if(collision.collider is MeshCollider)
        {
            // 0 - hexring - collide
            // 1 - hexringN
            // 2 - hexringY
            // 3 - hexpanel - collide
            // 4 - hexpanelN

            // Hitting the hexring means we have errored on this one, so turn red.
            if (otherOb.name.Contains("hexring"))
            {
                // If the hexpanel is still turned on and we hit the border, then it is a fail.
                if (parentOfOtherOb.transform.GetChild(3).gameObject.activeSelf)
                {
                    //otherOb.GetComponent<AudioSource>().PlayDelayed(0.2f);

                    // Turn off hex ring and turn on error (N) hex ring.
                    parentOfOtherOb.transform.GetChild(0).gameObject.SetActive(false);
                    parentOfOtherOb.transform.GetChild(1).gameObject.SetActive(true);
                    parentOfOtherOb.transform.GetChild(1).GetComponent<AudioSource>().Play();

                    // Turn off hex panel and turn on error (N) hex panel.
                    parentOfOtherOb.transform.GetChild(3).gameObject.SetActive(false);
                    parentOfOtherOb.transform.GetChild(4).gameObject.SetActive(true);
                    
                }

            }
            else if (otherOb.name.Contains("hexpanel"))
            {
                // If the error hexpanel is still turned off and we hit the panel, then it is a success.
                if (!parentOfOtherOb.transform.GetChild(1).gameObject.activeSelf) {

                    //parentOfOtherOb.GetComponent<AudioSource>().PlayDelayed(0.2f);

                    // Turn off hex ring and turn on success (Y) hex ring.
                    parentOfOtherOb.transform.GetChild(0).gameObject.SetActive(false);
                    parentOfOtherOb.transform.GetChild(2).gameObject.SetActive(true);
                    parentOfOtherOb.transform.GetChild(2).GetComponent<AudioSource>().Play();

                    // Turn off the hex panel, it has been "collected".
                    otherOb.SetActive(false);

                    // Hacky: Copied from below.
                    Vector3 starFxPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height
                    ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 1, 1, 1, 0.01f);

                    if (parentName.Contains("1"))
                    {
                        CollectBronzeParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectBronzeParticlesPrefab.Play();
                        CollectBronzeParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        CollectBronzeParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                        Game.Inst.CollectStar(0);
                    }
                    else if (parentName.Contains("2"))
                    {
                        CollectSilverParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectSilverParticlesPrefab.Play();
                        CollectSilverParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        CollectSilverParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                        Game.Inst.CollectStar(1);
                    }
                    else if (parentName.Contains("3"))
                    {
                        CollectGoldParticlesPrefab.transform.localPosition = starFxPosition;
                        CollectGoldParticlesPrefab.Play();
                        CollectGoldParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                        CollectGoldParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                        Game.Inst.CollectStar(2);
                    }
                }
            }
        }
    }

    IEnumerator StopEverything()
    {
        yield return Game.Inst.WaitParticlesStop;

        IsCrashed = false;
        IsRestarting = true;
        Game.Inst.LevelRestart(false);
        IsReset = true;
        ResetRotation();
        ResetPosition();
        IsRestarting = false;
    }

    IEnumerator ResetEverything()
    {
        //yield return Game.Inst.WaitParticlesStop;

        IsFinished = false;
        IsRestarting = true;
        Game.Inst.LevelRestart(true);

        // Wait for signal that level restart all done.
        while (Game.Inst.IsLevelRestarting)
        {
            yield return Game.Inst.WaitCheck;
        }
        IsReset = true;

        StopShine();
        StopDance();

        

        IsRestarting = false;

        Game.Inst.ActivateCamera(false);

        //yield return Game.Inst.WaitCheck2;

        ResetRotation();
        ResetPosition();
        // Now camera can reset to where bike is, since we have just stopped.
        Game.Inst.ResetCamera();

        yield return null;// Game.Inst.WaitCheck;

        Game.Inst.ActivateCamera(true);


        Game.Inst.StartGuiLevelText();

        
    }

    //////////////// BIKE COLLISION EXIT

    void OnCollisionExit(Collision collision)
    {
        if (IsCrashed)
            return;

        if (collision.collider is BoxCollider)
        {
            // Object that has the box component you are colliding into.
            GameObject boxOb = collision.collider.gameObject;

            if (!IsRestarting)
            {
                string boxObName = boxOb.name;

                // SWERVE BOXES
                if (boxObName.Contains("swerve"))
                {
                    if (transform.position.z > boxOb.transform.position.z) // Ignores the reset case where the bike ends up at the start and so is BEHIND the swerved object.
                    {
                        string parentName = boxOb.transform.parent.gameObject.name;

                        // Barriers get squishy when passed.
                        if (parentName.Contains("barrier"))
                        {
                            // Turn on crazy move.
                            //boxOb.transform.parent.GetChild(0).GetComponent<MMPathMovement>().enabled = true;
                            boxOb.transform.parent.GetChild(0).GetChild(0).GetComponent<MMSquashAndStretch>().enabled = true;

                            // Turn off all collisions too.
                            boxOb.transform.parent.GetChild(0).GetChild(0).GetChild(0).GetComponent<BoxCollider>().enabled = false; // model
                            boxOb.transform.parent.GetChild(0).GetChild(0).GetChild(0).GetComponent<CapsuleCollider>().enabled = false; // model
                            boxOb.transform.parent.GetChild(1).GetComponent<BoxCollider>().enabled = false; // left swerve
                            boxOb.transform.parent.GetChild(2).GetComponent<BoxCollider>().enabled = false; // right swerve
                        }

                        // When in swerve mode, these continue it.
                        SwerveAudio.Play();
                        SwerveParticlesPrefab.Play();
                        Swerve();

                        if (parentName.Contains("animal"))
                        {
                            // Play swerve sound.
                            boxOb.GetComponent<AudioSource>().PlayDelayed(0.3f);
                        }
                    }
                    
                }

                // NORMAL BOXES (NOT PRIZE OR DANGER BOXES)
                else if (!boxObName.Contains("Flag") && !boxObName.Contains("carDanger"))
                {
                    Vector3 starFxPosition = transform.localPosition + new Vector3(0, 0.5f, 0); // add a little height

                    // In this approach, the type of object jumped gets the corresponding reward.
                    if (boxObName.Contains("action"))
                    {
                        // DO ACTION

                        // Spin away barriers.
                        if (boxObName.Contains("barrier"))
                        {
                            // Spin it outta here.

                            // Move up and out (and left/right random).
                            MMPathMovement mpath = boxOb.transform.GetComponent<MMPathMovement>();
                            mpath.enabled = true;

                            if (boxObName.Contains("1")) {
                                mpath.PathElements[1].PathElementPosition.x = Random.Range(-1f, 1f);
                            } else if(boxObName.Contains("2")) {
                                mpath.PathElements[1].PathElementPosition.x = Random.Range(-2f, 2f);
                            }
                            else //if (boxObName.Contains("3"))
                            {
                                mpath.PathElements[1].PathElementPosition.x = Random.Range(-4f, 4f);
                            }

                            // Twirl around.
                            boxOb.transform.GetComponent<MMAutoRotate>().enabled = true;

                            // This locks rotation so make sure we clear it.
                            boxOb.transform.parent.GetComponent<MMSquashAndStretch>().enabled = false;

                            // Turn off all collisions too.
                            boxOb.transform.GetComponent<BoxCollider>().enabled = false; // model
                            boxOb.transform.GetComponent<CapsuleCollider>().enabled = false; // model
                            boxOb.transform.parent.parent.parent.GetChild(1).GetComponent<BoxCollider>().enabled = false; // left swerve
                            boxOb.transform.parent.parent.parent.GetChild(2).GetComponent<BoxCollider>().enabled = false; // right swerve
                        }
                        // Jump small animals.
                        else if(boxObName.Contains("1animal"))
                        {
                            // This enables wiggling which needs to be turned off if the level is reset.
                            MMPathMovement mpath = boxOb.transform.GetComponent<MMPathMovement>();
                            mpath.enabled = true;
                            mpath.PathElements[0].PathElementPosition.y = 0.2f;
                            mpath.PathElements[1].PathElementPosition.y = -0.1f;
                        }

                        if(boxObName.Contains("animal")) {
                            // Play the squeak sound or whatever.
                            boxOb.GetComponent<AudioSource>().Play();
                        }

                        // AWARD STAR

                        ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 1, 1, 1, 0.01f);
                       
                        if (boxObName.Contains("1"))
                        {
                            CollectBronzeParticlesPrefab.transform.localPosition = starFxPosition;
                            CollectBronzeParticlesPrefab.Play();
                            CollectBronzeParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                            CollectBronzeParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                            Game.Inst.CollectStar(0);
                        }
                        else if(boxObName.Contains("2"))
                        {
                            CollectSilverParticlesPrefab.transform.localPosition = starFxPosition;
                            CollectSilverParticlesPrefab.Play();
                            CollectSilverParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                            CollectSilverParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                            Game.Inst.CollectStar(1);
                        }
                        else if (boxObName.Contains("3"))
                        {
                            CollectGoldParticlesPrefab.transform.localPosition = starFxPosition;
                            CollectGoldParticlesPrefab.Play();
                            CollectGoldParticlesPrefab.gameObject.GetComponent<AudioSource>().Play();
                            CollectGoldParticlesPrefab.emission.SetBursts(new ParticleSystem.Burst[] { burst });
                            Game.Inst.CollectStar(2);
                        }

                    }
                    
                }

            }
        }
    }

}
