using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class UseMenu : MonoBehaviour
{
    public float levelSpinSpeed;
    public Transform target;
    public Camera camera;
    public Transform levels;
    public GameObject leftArrow;
    public GameObject rightArrow;
    public TextMeshProUGUI levelTitle; 

    private int levelIndex = 0;

    public float arrowXRate;
    public float arrowYRate;
    public float arrowXMaxScale;
    public float arrowYMaxScale;

    void Start()
    {
        
    }

    void Update()
    {
        int c = levels.childCount;

        // Hide arrow on first or last level.
        leftArrow.SetActive(true);
        rightArrow.SetActive(true);
        if (levelIndex == 0)
            leftArrow.SetActive(false);
        else if (levelIndex == c - 1)
            rightArrow.SetActive(false);

        // Rotate current level.
        for (int i = 0; i < c; i++)
        {
            if(i == levelIndex)
                levels.GetChild(i).Rotate(0, levelSpinSpeed * Time.deltaTime, 0, Space.Self);
            else
                levels.GetChild(i).rotation = Quaternion.identity;
        }

        // Pulsate arrows.
        float xScale = 1.0f + Mathf.Sin(Time.time * arrowXRate) * arrowXMaxScale;
        float yScale = 1.0f + Mathf.Sin(Time.time * arrowYRate) * arrowYMaxScale;

        leftArrow.transform.localScale = new Vector3(xScale, yScale, 1.0f);
        rightArrow.transform.localScale = new Vector3(xScale, yScale, 1.0f);

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
 
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
              
                if(hit.collider.gameObject == leftArrow)
                {
                    levelIndex = (levelIndex + c - 1) % c;
                    target.position = levels.GetChild(levelIndex).position;
                }
                else if(hit.collider.gameObject == rightArrow)
                {
                    levelIndex = (levelIndex + c + 1) % c;
                    target.position = levels.GetChild(levelIndex).position;
                }

                levelTitle.text = levels.GetChild(levelIndex).name;

            }
        }
    }
}
