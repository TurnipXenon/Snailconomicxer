using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Pengi.Gameplay;
using Pengi.GameSystem;
using Pengi.GameSystem.Save;
using Pengi.Others;
using Pengi.UI;
using SnailDate;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

namespace Pengi.Dialog
{
    /// <summary>
    /// CustomCommand has all the functions that can be used as a command in Yarn.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CustomCommands : MonoBehaviour, ISaveClientCallback, PoolableInstantAudio.IPooler
    {
        public GameConfiguration gameConfiguration;

        [Header("Variables")] public float delayTime = 3f;
        public float fadeRate = 0.05f;

        [Header("Assets")] 
        public AudioItem[] audioList;
        public GameObject sfxEffectPrefab;
        public GameObject prefabFadedAudio;

        [Header("Scene objects")]
        // Drag and drop your Dialogue Runner into this variable.
        public DialogueRunner dialogueRunner;

        public MemoryStorage memoryStorage;
        public InputManager inputManager;
        public SpriteRenderer blackScreen;
        public DialogueUIManager dialogueUiManager;

        private State _state = State.None;
        private float _alpha = 0f;
        private SaveClient _saveClient;
        private string _lastAudioName;
        private BackgroundScript _currentBg;
        private string _lastLocation;
        private float _screenTransitionDuration = 1f;
        private float _targetAlpha = 1f;
        private float _transitionStartTime = 0f;
        private float _startAlpha = 0f;
        private float _diffAlpha = 1f;

        private FadedAudio _lastAudio = null;
        private Action _onComplete;
        private CinemachineImpulseSource _impulseSignal;
        private BackgroundScript[] _backgroundScriptList;
        private readonly Stack<PoolableInstantAudio> _instantAudioPool = new Stack<PoolableInstantAudio>();

        private readonly Stack<FadedAudio> Pool = new Stack<FadedAudio>();

        private const string PuzzleShelfArg = "shelf";

        private enum State
        {
            None,
            GameEnding,
            ScreenFadeTransition
        }

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

        protected void Awake()
        {
            _impulseSignal = GetComponent<CinemachineImpulseSource>();

            Debug.Assert(gameConfiguration != null);
            Debug.Assert(dialogueRunner != null);
            Debug.Assert(dialogueUiManager != null);
            Debug.Assert(prefabFadedAudio != null);
            Debug.Assert(memoryStorage != null);
            Debug.Assert(inputManager != null);
            // Debug.Assert(iconManager != null);
            Debug.Assert(blackScreen != null);

            if (_saveClient == null)
            {
                _saveClient = gameConfiguration.RequestSaveAccess(this);
            }

            // PlayAudio(new[] {_saveClient.currentSave.lastAudioName});

            dialogueRunner.AddCommandHandler(
                "playAudio", // the name of the command
                PlayAudio // the method to run
            );

            dialogueRunner.AddCommandHandler("shake", Shake);
            dialogueRunner.AddCommandHandler("debugLog", DebugLog);
            dialogueRunner.AddCommandHandler("resetSpeaker", ResetSpeaker);
            dialogueRunner.AddCommandHandler("removeSpeaker", ResetSpeaker);
            dialogueRunner.AddCommandHandler("showDialogue", ShowDialogue);
            dialogueRunner.AddCommandHandler("hideDialogue", HideDialogue);
            dialogueRunner.AddCommandHandler("gameEnd", GameEnd);
            dialogueRunner.AddCommandHandler("enterStage", EnterStage);
            dialogueRunner.AddCommandHandler("exitStage", ExitStage);
            dialogueRunner.AddCommandHandler("fakeLastDialog", FakeLastDialog);
            dialogueRunner.AddCommandHandler("playSFX", PlaySfx);
            dialogueRunner.AddCommandHandler("playSfx", PlaySfx);
            dialogueRunner.AddCommandHandler("playsfx", PlaySfx);
            dialogueRunner.AddCommandHandler("fadePlainBackground", FadePlainBackground);
        }

        private void Start()
        {
            blackScreen.gameObject.SetActive(false);
        }

        private void Update()
        {
            switch (_state)
            {
                case State.None:
                    break;
                case State.GameEnding:
                    _alpha += fadeRate * Time.deltaTime;
                    var blackScreenColor = blackScreen.color;
                    blackScreenColor.a = _alpha;
                    blackScreen.color = blackScreenColor;

                    if (_alpha >= _targetAlpha)
                    {
                        SceneManager.LoadScene(PengiConstants.SceneEndGame);
                    }

                    break;
                case State.ScreenFadeTransition:
                    _alpha = _startAlpha +
                             ((Time.time - _transitionStartTime) / _screenTransitionDuration) * _diffAlpha;
                    var screenColor = blackScreen.color;
                    screenColor.a = _alpha;
                    blackScreen.color = screenColor;

                    if (_startAlpha < _targetAlpha && _alpha > _targetAlpha)
                    {
                        _state = State.None;
                        _onComplete?.Invoke();
                        _onComplete = null;
                    }
                    else if (_startAlpha >= _targetAlpha && _alpha < _targetAlpha)
                    {
                        _state = State.None;
                        blackScreen.gameObject.SetActive(false);
                        _onComplete?.Invoke();
                        _onComplete = null;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Plays a sound effect once. Sound effects can stack on each other.
        /// </summary>
        /// <example><<playSfx sfxName>></example>
        /// <param name="parameters">The name of the audio item containing the AudioClip in audioList</param>
        private void PlaySfx(string[] parameters)
        {
            if (parameters.Length == 0)
            {
                Debug.Log("PlaySFX: no parameter");
                return;
            }

            string searchTerm = parameters[0].ToUpper();
            foreach (var audioItem in audioList)
            {
                if (!audioItem.name.ToUpper().Equals(searchTerm)) continue;

                AudioClip audioClip = audioItem.audioClip;

                PoolableInstantAudio sfx;
                if (_instantAudioPool.Count == 0)
                {
                    sfx = Instantiate(sfxEffectPrefab).GetComponent<PoolableInstantAudio>();
                    sfx.Initialize();
                }
                else
                {
                    sfx = _instantAudioPool.Pop();
                }

                Debug.Assert(sfx != null);
                sfx.Play(this, audioClip);

                return;
            }

            Debug.Log($"Audio clip not found for: {searchTerm}");
        }

        /// <summary>
        /// EnterStage will allow a specified character to appear in the scene.
        /// </summary>
        /// <param name="parameter">The name or alias of the character</param>
        /// <remarks>You can call multiple characters in one command</remarks>
        /// <example><<enterStage B oldMole>></example>
        private void EnterStage(string[] parameter)
        {
            foreach (var characterName in parameter)
            {
                // iconManager.EnterStage(characterName);
            }
        }

        /// <summary>
        /// ExitStage forces a character to leave the scene. After a character leaves a scene and
        /// if their name appears in the script, they will not appear and the narrator will
        /// take place of them speaking.
        /// </summary>
        /// <param name="parameter">The name or alias of the character</param>
        /// <remarks>You can call multiple characters in one command</remarks>
        /// <example><<exitStage B oldMole>></example>
        private void ExitStage(string[] parameter)
        {
            foreach (var characterName in parameter)
            {
                // iconManager.ExitStage(characterName);
            }
        }

        /// <summary>
        /// Calling GameEnd will make the screen fade to black and open the credit scene.
        /// </summary>
        /// <param name="parameters"></param>
        /// <example><<gameEnd>></example>
        private void GameEnd(string[] parameters)
        {
            if (_lastAudio != null)
            {
                _lastAudio.FadeOut();
            }

            if (blackScreen != null)
            {
                blackScreen.gameObject.SetActive(true);
            }

            blackScreen.color = Color.black;
            _targetAlpha = 1f;
            _state = State.GameEnding;
        }

        /// <summary>
        /// Forces all characters to leave without the side effect of them not reappearing when
        /// they need to speak.
        /// </summary>
        /// <param name="parameters"></param>
        /// <example><<hideDialogue>></example>
        /// Note: the game had more distinct elements back then. Now, it's just a matter of making
        /// the characters leave the scene without the side effect of not making them not appear
        /// when it's their turn to speak.
        private void HideDialogue(string[] parameters)
        {
            ShowElements(false);
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <param name="parameters"></param>
        /// <example><<hideDialogue>></example>
        /// Note: the game had more distinct elements back then. Now, it's just a matter of making
        /// the characters leave the scene without the side effect of not making them not appear
        /// when it's their turn to speak.
        private void ShowDialogue(string[] parameters)
        {
            ShowElements(true);
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        /// <param name="parameters"></param>
        /// <example></example>
        private void ResetSpeaker(string[] parameters)
        {
            Debug.LogWarning("ResetSpeaker does not do anything.");
        }
        /// <summary>

        public void Shake(string[] parameter)
        {
            if (gameConfiguration.ShowVisualEffects)
            {
                _impulseSignal.GenerateImpulse(gameConfiguration.ShakeStrength);
            }
        }

        /// <summary>
        /// Prints a message into Debug.LogWarning()
        /// </summary>
        /// <param name="parameter">The message to be displayed in Debug.LogWarning()</param>
        private void DebugLog(string[] parameter)
        {
            Debug.LogWarning("Incoming warning from Yarn");
            Debug.LogWarning(string.Join(" ", parameter));
        }

        #region PlayAudio

        /// <summary>
        /// Plays the looping background music. In the game, only one background music can be played at a time.
        /// When invoked, it will attempt to fade out the previous audio, and fade in the current
        /// audio requested.
        /// </summary>
        /// <example>
        /// <<playAudio audioName>>
        /// </example>
        /// <param name="parameters"></param>
        private void PlayAudio(string[] parameters)
        {
            if (parameters.Length != 1)
            {
                return;
            }

            string searchTerm = parameters[0].ToUpper();
            foreach (var audioItem in audioList)
            {
                if (!audioItem.name.ToUpper().Equals(searchTerm))
                {
                    continue;
                }

                if (audioItem.audioClip == null)
                {
                    Debug.LogWarning($"playAudio: {audioItem.name} has no clip");
                    return;
                }

                if (_lastAudio != null)
                {
                    _lastAudio.FadeOut();
                }

                _lastAudio = GetNewAudio();
                _lastAudio.FadeIn(audioItem.audioClip, this);
                _lastAudioName = audioItem.name;

                return;
            }

            Debug.LogWarning($"Audio name not found: {searchTerm}");
        }

        /// <summary>
        /// Gets an unused audio from the pool or a newly instantiated one.
        /// </summary>
        /// <returns></returns>
        private FadedAudio GetNewAudio()
        {
            if (Pool.Count == 0)
            {
                var fadedAudio = Instantiate(prefabFadedAudio).GetComponent<FadedAudio>();
                Debug.Assert(fadedAudio != null);
                return fadedAudio;
            }
            else
            {
                return Pool.Pop();
            }
        }

        /// <summary>
        /// Receives an unused audio to be put back into the pool
        /// </summary>
        /// <param name="fadedAudio"></param>
        public void ReturnAudio(FadedAudio fadedAudio)
        {
            Pool.Push(fadedAudio);
        }

        #endregion PlayAudio

        /// <summary>
        /// ShowElements can only make characters leave.
        /// </summary>
        /// <param name="shouldShow">
        /// This function will only work if you pass shouldShow as true
        /// </param>
        public void ShowElements(bool shouldShow)
        {
            dialogueUiManager.ShowElements(shouldShow);
        }

        /// <summary>
        /// Sets the last dialog to the given parameter without making the dialog
        /// appear on-screen.
        /// </summary>
        /// <param name="parameters">Fake dialogue separated by a space/param>
        /// <remarks>
        /// This is very useful for parts without dialog, and we still want
        /// to give context about the last dialog when saving.
        /// </remarks>
        /// <example><<fakeLastDialog I just finished cleaning up the shelf...>></example>
        private void FakeLastDialog(String[] parameters)
        {
            string message = "";
            string speaker = "";

            if (parameters.Length == 1)
            {
                message = parameters[0];
            }
            else if (parameters.Length > 1)
            {
                speaker = parameters[0];
                message = string.Join(" ", parameters.Skip(1));
            }

            dialogueUiManager.SetFakeLastDialog(speaker, message);
        }

        /// <summary>
        /// Fades in or fades out a plain foreground.
        /// </summary>
        /// <param name="parameters">
        /// 1st element: on or off
        /// - on will make the foreground appear
        /// - off will make the foreground disappear
        /// 2nd element: float as transition duration
        /// 3rd element: block?
        /// - if the 3rd element is block, the function will block yarn until it finishes its transition
        /// 4th element: the color of the background
        /// - Only white or black is supported
        ///
        /// If there are 7 elements:
        /// - 4th, 5th, 6th, 7th elements would be float values that correspond to r, g, b, a in Color
        /// </param>
        /// <param name="onComplete"></param>
        private void FadePlainBackground(string[] parameters, System.Action onComplete)
        {
            if (!gameConfiguration.ShowVisualEffects)
            {
                onComplete.Invoke();
                return;
            }

            if (parameters.Length == 0)
            {
                Debug.LogWarning("fadePlainBackground needs one argument");
                return;
            }

            bool shouldAppear = parameters[0].ToLower().Equals("on");
            _screenTransitionDuration = 1f;
            var color = UnityEngine.Color.white;
            _onComplete = null;

            // duration
            if (parameters.Length > 1)
            {
                _screenTransitionDuration = float.Parse(parameters[1]);
            }

            // should block?
            if (parameters.Length > 2 && parameters[2].ToLower().Equals("block"))
            {
                _onComplete = onComplete;
            }
            else
            {
                onComplete.Invoke();
            }

            // color: accept by word or by value
            if (parameters.Length == 4)
            {
                switch (parameters[3].ToLower())
                {
                    case "white":
                        color = Color.white;
                        break;
                    case "black":
                        color = Color.white;
                        break;
                    default:
                        Debug.LogWarning($"Unknown color: {parameters[3].ToLower()} in fadePlainBackground");
                        break;
                }
            }
            else if (parameters.Length == 7)
            {
                color = new Color(float.Parse(parameters[3]), float.Parse(parameters[4]),
                    float.Parse(parameters[5]), float.Parse(parameters[6]));
            }

            if (shouldAppear)
            {
                blackScreen.gameObject.SetActive(true);
                _targetAlpha = color.a;
                color.a = 0f;
                blackScreen.color = color;
                _state = State.ScreenFadeTransition;
                _startAlpha = 0f;
            }
            else if (blackScreen.gameObject.activeSelf)
            {
                _state = State.ScreenFadeTransition;
                _startAlpha = blackScreen.color.a;
                _targetAlpha = 0f;
            }

            _diffAlpha = _targetAlpha - _startAlpha;
            _transitionStartTime = Time.time;
        }

        /// <summary>
        /// Writes relevant savable information to auto save when requested.
        /// </summary>
        public void WriteAutoSave()
        {
            // todo: auto save here
            // _saveClient.autoSave.lastHeader = _lastLocation;
        }

        /// <summary>
        /// Returns unused audio into the audio pool.
        /// </summary>
        /// <param name="finished"></param>
        public void ReturnInstantAudio(PoolableInstantAudio finished)
        {
            _instantAudioPool.Push(finished);
        }
    }


    [Serializable]
    public class AudioItem : DataItem
    {
        public AudioClip audioClip;
    }
}