using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class UseMenu : MonoBehaviour
{
    public float WorldSpinSpeed;
    public Transform Target;
    public Camera MenuCamera;
    public Transform WorldOptions;
    public GameObject LeftArrow;
    public GameObject RightArrow;
    public TextMeshProUGUI WorldTitle;

    public float ArrowXRate;
    public float ArrowYRate;
    public float ArrowXMaxScale;
    public float ArrowYMaxScale;

    int worldIndex = 0;
    
    void Start()
    {
        
    }

    void Update()
    {
        int c = WorldOptions.childCount;

        // Hide arrow on first or last level.
        LeftArrow.SetActive(true);
        RightArrow.SetActive(true);
        if (worldIndex == 0)
            LeftArrow.SetActive(false);
        else if (worldIndex == c - 1)
            RightArrow.SetActive(false);

        // Rotate current level.
        for (int i = 0; i < c; i++)
        {
            if(i == worldIndex)
                WorldOptions.GetChild(i).Rotate(0, WorldSpinSpeed * Time.deltaTime, 0, Space.Self);
            else
                WorldOptions.GetChild(i).rotation = Quaternion.identity;
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
                    worldIndex = (worldIndex + c - 1) % c;
                    Target.position = WorldOptions.GetChild(worldIndex).position;
                }
                else if(hit.collider.gameObject == RightArrow)
                {
                    worldIndex = (worldIndex + c + 1) % c;
                    Target.position = WorldOptions.GetChild(worldIndex).position;
                }

                WorldTitle.text = WorldOptions.GetChild(worldIndex).name;
            }
        }
    }
}
