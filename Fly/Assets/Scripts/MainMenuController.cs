using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject mapScreen;
    [SerializeField] private GameObject birdScreen;
    [SerializeField] private GameObject settingsScreen;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowTitle();
    }

    public void ShowTitle() => Show(titleScreen);
    public void ShowMaps() => Show(mapScreen);
    public void ShowBirds() => Show(birdScreen);
    public void ShowSettings() => Show(settingsScreen);

    public void StartForest()
    {
        SceneManager.LoadScene("Forest");
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void Show(GameObject screen)
    {
        titleScreen.SetActive(screen == titleScreen);
        mapScreen.SetActive(screen == mapScreen);
        birdScreen.SetActive(screen == birdScreen);
        settingsScreen.SetActive(screen == settingsScreen);
    }
}
