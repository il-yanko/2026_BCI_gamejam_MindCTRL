using UnityEngine;
using BCIEssentials.LSLFramework;


public class P300GridController : MonoBehaviour
{
    public ResponseProvider responseProvider; // Assign in Inspector or via code
    public GameObject[] gridButtons; // Assign your 9 buttons in Inspector

    void Start()
    {
        responseProvider.SubscribePredictions(OnPredictionReceived);
    }

    void OnPredictionReceived(Prediction prediction)
    {
        int selectedIndex = prediction.Index;
        if (selectedIndex >= 0 && selectedIndex < gridButtons.Length)
        {
            // Simulate button press or highlight
            gridButtons[selectedIndex].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }
        else
        {
            Debug.LogWarning($"Prediction index out of range: {selectedIndex}");
        }
    }
}