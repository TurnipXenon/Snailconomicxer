using Pengi.Dialog;
using Pengi.GameSystem;
using Pengi.GameSystem.Save;
using Pengi.Others;
using Pengi.UI;
using SnailDate;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

namespace Pengi.Manager
{
    /// <summary>
    /// <c>DialogSceneManager</c> manages DialogScene
    /// </summary>
    public class DialogSceneManager : MonoBehaviour, ISaveClientCallback
    {
        public GameConfiguration gameConfiguration;
        public DialogueRunner runner;
        public MemoryStorage memory;
        public DialogueUIManager dialogueUiManager;
        public AutoSaveText autoSaveText;
        
        private SaveClient _saveClient;
        private GameInstance _gameInstance;
        private bool _isSaveDirty = false;
        private bool _isSaveDone = false;

        public bool IsSaveDone => _isSaveDone;

        private void OnEnable()
        {
            if (_saveClient == null)
            {
                _saveClient = gameConfiguration.RequestSaveAccess(this);
            }
        }

        private void OnDisable()
        {
            gameConfiguration.ReleaseSaveAccess(_saveClient);
            _saveClient = null;
        }

        private void Awake()
        {
            Debug.Assert(gameConfiguration != null);
            Debug.Assert(runner != null);
            Debug.Assert(memory != null);
            Debug.Assert(dialogueUiManager != null);
            
            // set up
            if (_saveClient == null)
            {
                _saveClient = gameConfiguration.RequestSaveAccess(this);
            }
            
            _gameInstance = gameConfiguration.gameInstance;
            
            // attach auto save node
            runner.onNodeStart.AddListener(AutoSaveNode);
            
            memory.defaultVariables = _saveClient.currentSave.savedVariables.ToArray();
            memory.ResetToDefaults();
        }

        private void AutoSaveNode(string currentNode)
        {
            if (_isSaveDirty)
            {
                // write on save client
                _gameInstance.WriteOnAutoSave();
                autoSaveText.AnimateText();
                _isSaveDone = true;
            }
            
            _isSaveDirty = true;
        }

        public void WriteAutoSave()
        {
            memory.Write(_saveClient.autoSave);
        }

        /// <summary>
        /// Used when back button is clicked
        /// </summary>
        /// I'm not sure but I don't want to delete this, just in case.
        public void GoBack()
        {
            SceneManager.LoadScene(PengiConstants.SceneMainMenu);
        }
    }
}