// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class LevelManager : MonoBehaviour
    {
        private const string PlayerScene = "Player";
        private const string NatureScene = "nature";
        private const string ManagersScene = "Managers";
        private static readonly string[] ScenesToEnsureIsLoaded = { NatureScene, PlayerScene};
        private static readonly string[] ScenesToPreserveOnLoadLevel = { ManagersScene, NatureScene, PlayerScene };
        public static LevelManager Instance { get; private set; }
        private MenuManager _menuManager;
        private string _currentLevelName;
        private Slider _loadingProgressSlider;
        private GameObject _loadingCanvas;

        void Awake()
        {
            Instance = this;
//            foreach (GameObject managerObject in SceneManager.GetSceneAt(0).GetRootGameObjects())
//            {
//                DontDestroyOnLoad(managerObject);
//            }

            _menuManager = GameObject.Find("MenuManager").GetComponent<MenuManager>();
            if (_menuManager == null) throw new Exception("MenuManager not found.");

            _loadingCanvas = Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.name == "LoadingCanvas");
            GameObject sliderObj = Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.name == "LoadingProgressSlider");

            _loadingProgressSlider = sliderObj.GetComponent<Slider>();

//            StartCoroutine(LoadInitialScenes());
        }

//        private static IEnumerator LoadInitialScenes()
//        {
//            // Then add base scenes on top
//            foreach (string baseScene in ScenesToEnsureIsLoaded)
//            {
//                yield return SceneManager.LoadSceneAsync(baseScene);
//            }
//        }
//
        void Start()
        {
            LoadLevelAsync("FreeFlight");
        }

        void Update()
        {
            if (Input.GetButtonDown(InputNames.Reset))
            {
                ResetLevelAsync();
            }
        }

        private void ResetLevelAsync()
        {
            Debug.Log("Reset level.");
            LoadLevelAsync(_currentLevelName);
        }

        public Coroutine LoadLevelAsync(string levelName)
        {
            return StartCoroutine(LoadLevel(levelName));
        }

        private IEnumerator LoadLevel(string levelName)
        {
            Debug.Log("Load level BEGIN: " + levelName);
            _loadingProgressSlider.value = 0;
            _loadingCanvas.SetActive(true);

            // Ensure environment is loaded
//            foreach (string sceneName in ScenesToEnsureIsLoaded)
//            {
//                yield return LoadSceneIfNotLoaded(sceneName);
//            }

            // Unload all except environment and managers
            IEnumerable<Scene> scenesToUnload = Enumerable.Range(0, SceneManager.sceneCount)
                .Reverse()
                .Select(SceneManager.GetSceneAt)
                .Where(x => !ScenesToPreserveOnLoadLevel.Contains(x.name))
                .ToList();

            foreach (Scene scene in scenesToUnload)
                yield return SceneManager.UnloadSceneAsync(scene);

            _loadingProgressSlider.value = .2f;

            // Reload player scene
            yield return SceneManager.UnloadSceneAsync(PlayerScene);
            yield return SceneManager.LoadSceneAsync(PlayerScene, LoadSceneMode.Additive);

            _loadingProgressSlider.value = .3f;

            // Load level scene
            yield return SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);

            _loadingProgressSlider.value = .9f;

            _currentLevelName = levelName;

            _menuManager.HideMenu();

            // Reset player and move to level spawn point
            var player = GameObject.FindGameObjectsWithTag(Tags.Player).Single().GetComponent<Parrot>();
            player.OnLevelLoad();
            GameObject spawnPoint = GameObject.FindGameObjectsWithTag(Tags.Respawn).FirstOrDefault();
            if (spawnPoint != null)
            {
                player.transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
            }
            else
            {
                // Default to scene's position if no spawn point is set
                Debug.LogWarning("No spawn point found.");
            }

            Time.timeScale = 1;

            _loadingProgressSlider.value = 1;
            _loadingCanvas.SetActive(false);
            Debug.Log("Load level DONE: " + levelName);

            yield return new WaitForEndOfFrame();
        }

        [CanBeNull]
        private static AsyncOperation LoadSceneIfNotLoaded(string sceneName)
        {
            Scene playerScene = SceneManager.GetSceneByName(sceneName);
            return playerScene.isLoaded ? null : SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
    }
}