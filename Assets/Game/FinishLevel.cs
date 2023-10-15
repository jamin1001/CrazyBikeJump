using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishLevel : MonoBehaviour
{
    public float HeightY;
    public float VariationX;
    public float VariationY;

    // Start is called before the first frame update
    void OnCollisionEnter(Collision collision)
    {
        Game.Inst.BikeStopped();
        Game.Inst.IsFinished = true;
        Game.Inst.PlayOneShot(Game.Inst.FinishLineClip);
        StartCoroutine(PopPopPop()); 
    }

    IEnumerator PopPopPop()
    {
        Transform confettiParent = transform.GetChild(0);

        List<WaitForSecondsRealtime> waitTimes = Game.WaitTimes1;
        int waitCount = waitTimes.Count;
        int randomWait;
        float sameZ = confettiParent.GetChild(0).gameObject.transform.position.z;
        float offsetX = Camera.main.transform.position.x;
        float offsetY = HeightY;

        yield return Game.WaitConfettiStart;

        // 1
        Game.Inst.PlayOneShot(Game.Inst.ConfettiBangClip);
        confettiParent.GetChild(0).gameObject.SetActive(true);
        confettiParent.GetChild(0).position = new Vector3(offsetX + Random.Range(-VariationX, +VariationX), offsetY + Random.Range(-VariationY, +VariationY), sameZ);
        
        // 2
        randomWait = Random.Range(0, waitCount);
        yield return waitTimes[randomWait];
        Game.Inst.PlayOneShotRandom(Game.Inst.ConfettiPopClips);
        confettiParent.GetChild(1).gameObject.SetActive(true);
        confettiParent.GetChild(1).position = new Vector3(offsetX + Random.Range(-VariationX, +VariationX), offsetY + Random.Range(-VariationY, +VariationY), sameZ);
       
        // 3
        randomWait = Random.Range(0, waitCount);
        yield return waitTimes[randomWait];
        Game.Inst.PlayOneShotRandom(Game.Inst.ConfettiPopClips);
        confettiParent.GetChild(2).gameObject.SetActive(true);
        confettiParent.GetChild(2).position = new Vector3(offsetX + Random.Range(-VariationX, +VariationX), offsetY + Random.Range(-VariationY, +VariationY), sameZ);

        yield return Game.WaitConfettiStop;

        confettiParent.GetChild(0).gameObject.SetActive(false);
        confettiParent.GetChild(1).gameObject.SetActive(false);
        confettiParent.GetChild(2).gameObject.SetActive(false);

        // Check score and pick here.
        //Game.Inst.PlayOneShot(Game.Inst.FinishedBadClip);
        Game.Inst.PlayOneShot(Game.Inst.FinishedGoodClip);
        //Game.Inst.PlayOneShot(Game.Inst.FinishedWonderfulClip);

        Game.Inst.IsFinished = false;
        Game.Inst.Restart();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
