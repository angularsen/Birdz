// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    private bool _isMenuButtonPressed;
    private bool _isMenuVisible;
    private float _timeScaleBeforePause;

    void Awake()
    {
    }

    void Start()
    {
    }

    void Update()
    {
        if (Input.GetButtonDown("Menu"))
        {
            if (!_isMenuButtonPressed)
            {
                _isMenuButtonPressed = true;
                ToggleMenu();
            }
        }
        else if (Input.GetButtonUp("Menu"))
        {
            _isMenuButtonPressed = false;
        }
    }

    public void ToggleMenu()
    {
        if (!_isMenuVisible)
            ShowMenu();
        else
            HideMenu();
    }

    public void HideMenu()
    {
        // Resume time
        Time.timeScale = _timeScaleBeforePause;

        Scene mainMenuScene = SceneManager.GetSceneByName("MainMenu");
        if (mainMenuScene.IsValid() && mainMenuScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync("MainMenu");
            _isMenuVisible = false;
            AudioListener.pause = false;
        }
    }

    public void ShowMenu()
    {
        Scene mainMenuScene = SceneManager.GetSceneByName("MainMenu");
        if (mainMenuScene.IsValid() && mainMenuScene.isLoaded)
            return;

        // Pause time
        _timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0;
        AudioListener.pause = true;

        SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
        _isMenuVisible = true;
    }
}