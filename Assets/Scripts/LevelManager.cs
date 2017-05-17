// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
    public class LevelManager : MonoBehaviour
    {
        private static readonly string[] ScenesToPreserveByName = {"nature", "MainMenu"};
        public static LevelManager Instance { get; private set; }
        private MenuManager _menuManager;
        private string _currentLevelName;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
            _menuManager = GameObject.Find("MenuManager").GetComponent<MenuManager>();
            if (_menuManager == null) throw new Exception("MenuManager not found.");
        }

        void Start()
        {
            LoadLevelAsync("FreeFlight");
        }

        void Update()
        {
            if (Input.GetButtonDown(InputNames.Reset))
            {
                ResetLevel();
            }
        }

        private void ResetLevel()
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
            IEnumerable<Scene> scenesToUnload = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .Where(x => !ScenesToPreserveByName.Contains(x.name))
                .ToList();

            foreach (Scene scene in scenesToUnload)
                yield return SceneManager.UnloadSceneAsync(scene);

            yield return SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
            _currentLevelName = levelName;

            _menuManager.HideMenu();

            GameObject spawnPoint = GameObject.FindGameObjectsWithTag(Tags.Respawn).FirstOrDefault();
            if (spawnPoint != null)
            {
                GameObject player = GameObject.FindGameObjectsWithTag(Tags.Player).Single();
                player.transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
            }
            else
            {
                // Default to scene's position if no spawn point is set
                Debug.LogWarning("No spawn point found.");
            }

            Time.timeScale = 1;

            yield return new WaitForEndOfFrame();
            Debug.Log("Load level DONE: " + levelName);
        }
    }
}