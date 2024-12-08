using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class UseMenu : MonoBehaviour
{
    public float LevelSpinSpeed;
    public Transform Target;
    public Camera MenuCamera;
    public Transform Levels;
    public GameObject LeftArrow;
    public GameObject RightArrow;
    public TextMeshProUGUI LevelTitle; 

    public float ArrowXRate;
    public float ArrowYRate;
    public float ArrowXMaxScale;
    public float ArrowYMaxScale;

    int levelIndex = 0;

    void Start()
    {
        
    }

    void Update()
    {
        int c = Levels.childCount;

        // Hide arrow on first or last level.
        LeftArrow.SetActive(true);
        RightArrow.SetActive(true);
        if (levelIndex == 0)
            LeftArrow.SetActive(false);
        else if (levelIndex == c - 1)
            RightArrow.SetActive(false);

        // Rotate current level.
        for (int i = 0; i < c; i++)
        {
            if(i == levelIndex)
                Levels.GetChild(i).Rotate(0, LevelSpinSpeed * Time.deltaTime, 0, Space.Self);
            else
                Levels.GetChild(i).rotation = Quaternion.identity;
        }

        // Pulsate arrows.
        float xScale = 1.0f + Mathf.Sin(Time.time * ArrowXRate) * ArrowXMaxScale;
        float yScale = 1.0f + Mathf.Sin(Time.time * ArrowYRate) * ArrowYMaxScale;

        LeftArrow.transform.localScale = new Vector3(xScale, yScale, 1.0f);
        RightArrow.transform.localScale = new Vector3(xScale, yScale, 1.0f);

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = MenuCamera.ScreenPointToRay(Input.mousePosition);
 
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
              
                if(hit.collider.gameObject == LeftArrow)
                {
                    levelIndex = (levelIndex + c - 1) % c;
                    Target.position = Levels.GetChild(levelIndex).position;
                }
                else if(hit.collider.gameObject == RightArrow)
                {
                    levelIndex = (levelIndex + c + 1) % c;
                    Target.position = Levels.GetChild(levelIndex).position;
                }

                LevelTitle.text = Levels.GetChild(levelIndex).name;

            }
        }
    }
}
