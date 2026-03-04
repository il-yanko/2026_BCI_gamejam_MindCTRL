using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    InputAction escapeAction;
    void Start()
    {
        escapeAction = InputSystem.actions.FindAction("UI/Cancel");
    }

    void Update()
    {
        if (escapeAction.IsPressed())
        {
            print("Escape");
            SceneManager.LoadScene("SettingsMenu");
        }
    }
}
