using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    [SerializeField]
    private Transform target; // player

    [SerializeField]
    private float height = 50f;

    [SerializeField]
    private bool rotateWithPlayer = true;

    private void LateUpdate()
    {
        if (!target)
            return;

        Vector3 pos = target.position;
        pos.y += height;
        transform.position = pos;

        if (rotateWithPlayer)
            transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
        else
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
