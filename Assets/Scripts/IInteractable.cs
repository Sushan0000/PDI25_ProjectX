using UnityEngine;

// Anything the player can interact with (pickups, buttons, etc.)
public interface IInteractable
{
    // Player object that has ControlScript.
    void Interact(ControlScript player);
}