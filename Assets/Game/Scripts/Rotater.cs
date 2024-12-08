using UnityEngine;

public class Rotater : MonoBehaviour
{
    [Tooltip("For 3D specifies the eulers to values rotate by. For 2D spin just adjust +/- Z.")]
    public Vector3 RotateSpeed = Vector3.zero;
    public enum SpaceEnum { Local, World };
    [Tooltip("Whether it rotates according to local frame or world frame.")]
    public SpaceEnum RotateSpace;

    void Update()
    {
        if (RotateSpace == SpaceEnum.Local)
            transform.Rotate(RotateSpeed * Time.deltaTime);
        else
            transform.Rotate(RotateSpeed * Time.deltaTime, Space.World);
    }
}
