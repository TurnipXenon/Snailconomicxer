using Pengi.GameSystem;
using Pengi.GameSystem.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pengi.Manager
{
    public class MainMenuManager : MonoBehaviour, ISaveClientCallback
    {
        public GameConfiguration gameConfiguration;
        public Button newGameButton;
        public Button continueButton;
        public Button optionsButton;
        public SaveClient saveClient;

        private void OnDestroy()
        {
            gameConfiguration.ReleaseSaveAccess(saveClient);
            saveClient = null;
        }

        private void Start()
        {
            saveClient = gameConfiguration.RequestSaveAccess(this);
            
            // disable continue when auto save does not exist
            continueButton.interactable = gameConfiguration.SaveIo.RequestSlotExecutor()
                .AtSlotIndex(GameConfiguration.AutoSaveIndex)
                .DoesExist();
            
            newGameButton.gameObject.SetActive(true);
            continueButton.gameObject.SetActive(true);
            optionsButton.gameObject.SetActive(true);
        }

        public void OnClickNewGame()
        {
            gameConfiguration.ResetSaveData();
            SceneManager.LoadScene("DialogScene");
        }

        public void OnClickContinue()
        {
            // gameConfiguration.LoadData(GameConfiguration.AutoSaveIndex);
            // SceneManager.LoadScene("DialogScene");
            SceneManager.LoadScene("SaveSelect");
        }

        public void OnClickOptions()
        {
            SceneManager.LoadScene("OptionScene");
        }

        public void WriteAutoSave()
        {
            // do nothing
        }
    }
}