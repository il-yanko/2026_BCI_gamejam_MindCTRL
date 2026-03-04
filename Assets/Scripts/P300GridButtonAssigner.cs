using UnityEngine;
using BCIEssentials.LSLFramework;

public class P300GridButtonAssigner : MonoBehaviour
{
    public P300GridController gridController;

    void Awake()
    {
        // Try to auto-assign the 9 buttons if not set
        if (gridController != null && (gridController.gridButtons == null || gridController.gridButtons.Length == 0))
        {
            GameObject[] foundButtons = new GameObject[9];
            for (int i = 0; i < 9; i++)
            {
                var btn = GameObject.Find($"Button{i}");
                if (btn != null)
                    foundButtons[i] = btn;
            }
            gridController.gridButtons = foundButtons;
        }
    }
}
