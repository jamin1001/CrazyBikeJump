using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishLevel : MonoBehaviour
{
    public float FireworksHeightY;
    public float FireworksVariationX;
    public float FireworksVariationY;

    public AudioSource FinishLineAudio;
    public AudioSource FinishBadAudio;
    public AudioSource FinishGoodAudio;
    public AudioSource FinishGreatAudio;

    // Start is called before the first frame update
    void OnCollisionEnter(Collision collision)
    {
        Game.Inst.IsFinished = true;
        Game.Inst.BikeStopped();
        FinishLineAudio.Play();
        StartCoroutine(PopPopPop(collision.gameObject)); 
    }

    IEnumerator PopPopPop(GameObject obThatEnteredFinishLine)
    {
        Transform confettiParent = transform.GetChild(3); // Careful: this is a specific child within the Finish scene node.

        List<WaitForSecondsRealtime> waitTimes = Game.WaitTimes1;
        int waitCount = waitTimes.Count;
        int randomWait;
        Vector3 obPos = obThatEnteredFinishLine.transform.position;

        bool bronzeStar = Game.Inst.StarsThisLevel[0] > 0;
        bool silverStar = Game.Inst.StarsThisLevel[1] > 0;
        bool goldStar = Game.Inst.StarsThisLevel[2] > 0;
        bool anyStars = bronzeStar || silverStar || goldStar;
        bool allStars = bronzeStar && silverStar && goldStar;

        if (anyStars)
            yield return Game.Inst.WaitConfettiStart;

        if (bronzeStar)
        {
            // 1
            confettiParent.GetChild(0).gameObject.SetActive(true);
            confettiParent.GetChild(0).position = obPos + new Vector3(Random.Range(-FireworksVariationX, +FireworksVariationX), FireworksHeightY + Random.Range(-FireworksVariationY, +FireworksVariationY), 0);
            confettiParent.GetChild(0).gameObject.GetComponent<AudioSource>().Play();
            Game.Inst.GameGui.JumpText.text = "Bronze Jump Bonus";
            yield return Game.Inst.JumpTextBonus;
            Game.Inst.GameGui.JumpText.text = "Bronze Jump Bonus\n+1";
            Game.Inst.GameGui.AddSpinCoins(1);

        }

        if (silverStar)
        {
            // 2
            randomWait = Random.Range(0, waitCount);
            yield return waitTimes[randomWait];
            confettiParent.GetChild(1).gameObject.SetActive(true);
            confettiParent.GetChild(1).position = obPos + new Vector3(Random.Range(-FireworksVariationX, +FireworksVariationX), FireworksHeightY + Random.Range(-FireworksVariationY, +FireworksVariationY), 0);
            confettiParent.GetChild(1).gameObject.GetComponent<AudioSource>().Play();
            Game.Inst.GameGui.JumpText.text = "Silver Jump Bonus";
            yield return Game.Inst.JumpTextBonus;
            Game.Inst.GameGui.JumpText.text = "Silver Jump Bonus\n+2";
            Game.Inst.GameGui.AddSpinCoins(2);
        }

        if (goldStar)
        {
            // 3
            randomWait = Random.Range(0, waitCount);
            yield return waitTimes[randomWait];
            confettiParent.GetChild(2).gameObject.SetActive(true);
            confettiParent.GetChild(2).position = obPos + new Vector3(Random.Range(-FireworksVariationX, +FireworksVariationX), FireworksHeightY + Random.Range(-FireworksVariationY, +FireworksVariationY), 0);
            confettiParent.GetChild(2).gameObject.GetComponent<AudioSource>().Play();
            Game.Inst.GameGui.JumpText.text = "Gold Jump Bonus";
            yield return Game.Inst.JumpTextBonus;
            Game.Inst.GameGui.JumpText.text = "Gold Jump Bonus\n+3";
            Game.Inst.GameGui.AddSpinCoins(3);
        }

        if (allStars)
        {
            // 1 + 2 + 3
            List<WaitForSecondsRealtime> waitTimesHalf = Game.WaitTimes1Half;


            randomWait = Random.Range(0, waitCount);
            yield return waitTimesHalf[randomWait];
            confettiParent.GetChild(0).gameObject.SetActive(false);
            confettiParent.GetChild(0).gameObject.SetActive(true);
            confettiParent.GetChild(0).position = obPos + new Vector3(Random.Range(-FireworksVariationX, +FireworksVariationX), FireworksHeightY + Random.Range(-FireworksVariationY, +FireworksVariationY), 0);
            confettiParent.GetChild(0).gameObject.GetComponent<AudioSource>().Play();

            randomWait = Random.Range(0, waitCount);
            yield return waitTimesHalf[randomWait];
            confettiParent.GetChild(1).gameObject.SetActive(false);
            confettiParent.GetChild(1).gameObject.SetActive(true);
            confettiParent.GetChild(1).position = obPos + new Vector3(Random.Range(-FireworksVariationX, +FireworksVariationX), FireworksHeightY + Random.Range(-FireworksVariationY, +FireworksVariationY), 0);
            confettiParent.GetChild(1).gameObject.GetComponent<AudioSource>().Play();

            randomWait = Random.Range(0, waitCount);
            yield return waitTimesHalf[randomWait];
            confettiParent.GetChild(2).gameObject.SetActive(false);
            confettiParent.GetChild(2).gameObject.SetActive(true);
            confettiParent.GetChild(2).position = obPos + new Vector3(Random.Range(-FireworksVariationX, +FireworksVariationX), FireworksHeightY + Random.Range(-FireworksVariationY, +FireworksVariationY), 0);
            confettiParent.GetChild(2).gameObject.GetComponent<AudioSource>().Play();

            Game.Inst.GameGui.JumpText.text = "All Jump Bonus";
            yield return Game.Inst.JumpTextBonus;
            Game.Inst.GameGui.JumpText.text = "All Jump Bonus\n+5";
            Game.Inst.GameGui.AddSpinCoins(5);
        }

        if (anyStars)
        {
            confettiParent.gameObject.GetComponent<AudioSource>().Play();
            yield return Game.Inst.WaitConfettiStop;
        }

        // Check score and pick here.
        if (!anyStars)
        {
            FinishBadAudio.Play();
            Game.Inst.GameGui.JumpText.text = "No Jumps!";
        }
        // Got a gold star.
        else if (Game.Inst.StarsThisLevel[2] > 0)
            FinishGreatAudio.Play();
        // Got something.
        else
            FinishGoodAudio.Play();

        Game.Inst.IsFinished = false;
        Game.Inst.Restart(true);

        confettiParent.GetChild(0).gameObject.SetActive(false);
        confettiParent.GetChild(1).gameObject.SetActive(false);
        confettiParent.GetChild(2).gameObject.SetActive(false);

        yield return Game.Inst.JumpTextDisappear;
        Game.Inst.GameGui.JumpText.text = "";
    }

}
