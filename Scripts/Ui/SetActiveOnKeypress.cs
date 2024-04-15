using UnityEngine;
using UnityEngine.EventSystems;

public class SetActiveOnKeypress : MonoBehaviour {
    public KeyCode setActiveKey = KeyCode.F1;
    public GameObject objectToSetActive;
    //public GameObject objectToDeactivate;

    private void Start()
    {
        pauseGame();
    }

    private void Update() {
        if (Input.GetKeyDown(setActiveKey)) {
            objectToSetActive.SetActive(!objectToSetActive.activeInHierarchy);
            pauseGame();
        }
    }

    private void pauseGame()
    {
        var lockState = Cursor.lockState;
        var cursorVisible = Cursor.visible;
        if (objectToSetActive.activeInHierarchy)
        {
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0f;
            Cursor.visible = true;
            EventSystem.current.SetSelectedGameObject(null);
        }
        else
        {
            Cursor.lockState = lockState;
            Time.timeScale = 1f;
            Cursor.visible = cursorVisible;
        }
    }
}