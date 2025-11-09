using UnityEngine;
using UnityEngine.InputSystem;   // new system

[RequireComponent(typeof(CharacterController))]
public class ControlScript: MonoBehaviour
{
    public float playerSpeed = 1.9f;
    public CharacterController cC;

    void Awake()
    {
        if (!cC) cC = GetComponent<CharacterController>();
    }

    void Update()
    {
        PlayerMove();
    }

    void PlayerMove()
    {
        if (Keyboard.current == null) return;

        float h = (Keyboard.current.aKey.isPressed ? -1f : 0f) +
                  (Keyboard.current.dKey.isPressed ?  1f : 0f);
        float v = (Keyboard.current.sKey.isPressed ? -1f : 0f) +
                  (Keyboard.current.wKey.isPressed ?  1f : 0f);

        Vector3 direction = new Vector3(h, 0f, v);
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        if (direction.sqrMagnitude >= 0.01f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
            cC.Move(direction.normalized * playerSpeed * Time.deltaTime);
        }
    }
}
