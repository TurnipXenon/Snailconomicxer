﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pengi.Dialog;
using Pengi.Gameplay;
using Pengi.GameSystem;
using Pengi.GameSystem.Save;
using UnityEngine;
using UnityEngine.Events;
using Yarn;
using Yarn.Unity;

namespace SnailDate
{
    /// <summary>
    /// Modified version of YarnSpinner's sample DialogueUI
    /// </summary>
    /// <seealso cref="DialogueUI"/>
    public class DialogueUIManager : Yarn.Unity.DialogueUIBehaviour, ISaveClientCallback
    {
        public InputManager inputManager;
        public CustomCommands customCommands;
        public DialogBox dialogBox;

        [Header("Critical")] public GameConfiguration gameConfiguration;

        public String[] markupWholeWhitelist;
        public String[] markupPhraseWhitelist;
        
        private bool userRequestedNextLine = false;

        private System.Action<int> currentOptionSelectionHandler;

        private bool waitingForOptionSelection = false;

        public UnityEvent onDialogueStart;

        public UnityEvent onDialogueEnd;

        public UnityEvent onLinesStart;

        public UnityEvent onLineFinishDisplaying;

        public DialogueRunner.StringUnityEvent onLineUpdate;

        public UnityEvent onLineEnd;

        public UnityEvent onOptionsStart;

        public UnityEvent onOptionsEnd;

        public DialogueRunner.StringUnityEvent onCommand;

        public DialogueBlocker dialogueBlocker = new DialogueBlocker();

        private string _lastSpeaker = "";
        private SaveClient saveClient;
        private string _lastDialog;

        private float TextRate => gameConfiguration.TextRate;

        public bool IsBlocking
        {
            get => dialogueBlocker.IsBlocking;
            set
            {
                if (value)
                {
                    dialogueBlocker.Block();
                }
                else
                {
                    dialogueBlocker.Unblock();
                }
            }
        }

        private void OnEnable()
        {
            saveClient = gameConfiguration.RequestSaveAccess(this);
        }

        private void OnDisable()
        {
            gameConfiguration.ReleaseSaveAccess(saveClient);
            saveClient = null;
        }

        private void Awake()
        {
            Debug.Assert(gameConfiguration != null);
        }

        public override Dialogue.HandlerExecutionType RunLine(Line line, ILineLocalisationProvider localisationProvider,
            Action onLineComplete)
        {
            StartCoroutine(DoRunLine(line, localisationProvider, onLineComplete));
            return Dialogue.HandlerExecutionType.PauseExecution;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// For changing up the text. You have three places to watch out for:
        /// (1) When text is fed to the string slowly
        /// (2) When text was never fed to the string slowly
        /// (3) User decided to show the entire text immediately
        /// </remarks>
        /// <param name="line"></param>
        /// <param name="localisationProvider"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        private IEnumerator DoRunLine(Yarn.Line line, ILineLocalisationProvider localisationProvider,
            System.Action onComplete)
        {
            onLinesStart?.Invoke();

            userRequestedNextLine = false;

            string text = localisationProvider.GetLocalisedTextForLine(line);
            text = text.Trim();

            // for empty //, we ignore them
            if (text.Trim().Equals("//"))
            {
                userRequestedNextLine = true;
                text = "";
            }

            // identifies speaker
            var argSplit = text.Split(':');
            var speakerText = argSplit[0];
            DialogBoxReturn info = dialogBox.InformSpeaker(speakerText);
            
            // InformSpeakerReturn speakerInfo = iconManager.InformSpeaker(argSplit.Length != 1 ? argSplit[0] : "");
            dialogueBlocker = info.dialogueBlocker;
            // var character = speakerInfo.character;
            _lastSpeaker = info.realName;
            
            if (info.realName.Length != 0)
            {
                text = text.Replace($"{argSplit[0]}:", $"{info.realName}:");
            } 
            // todo: understand relevance of this
            // else if (info.character.CharacterType == CharacterType.Narrator)
            // {
            //     text = text.Replace($"{argSplit[0]}:", "");
            // }

            text = text.Trim();
            // from http://digitalnativestudios.com/forum/index.php?topic=1199.0
            text = text.Replace("\\n", "\n");

            // onLineUpdate.AddListener(textItem.UpdateLine);

            while (dialogueBlocker.IsBlocking && !userRequestedNextLine)
            {
                yield return new WaitForSeconds(0.03f);
            }
            
            // todo: show dialog box immediately if skipping

            if (TextRate <= 0f)
            {
                gameConfiguration.TextRate = 0f;
            }

            var formattedStringBuilder = new StringBuilder();
            var cleanStringBuilder = new StringBuilder();
            var markupBuilder = new StringBuilder();
            var textMarks = new Queue<SpecialTextMark>();

            if (text.Contains("<forceNext>"))
            {
                text = "";
            }

            foreach (var c in text)
            {
                #region for hiding markup

                if (markupBuilder.Length != 0)
                {
                    markupBuilder.Append(c);

                    if (c.Equals('>'))
                    {
                        AdjustMarkups(markupBuilder);

                        var markupText = markupBuilder.ToString().ToLower();
                        if (!gameConfiguration.EnableTextFormatting  || !IsWhiteListed(markupText))
                        {
                            // don't do text formatting
                        }
                        else if (markupText.Contains("textspeed"))
                        {
                            var parseArgs = markupText.Split('=');
                            if (parseArgs.Length == 1)
                            {
                                textMarks.Enqueue(new SpecialTextMark
                                {
                                    argument = "1",
                                    effect = SpecialTextMark.Effect.TextSpeed,
                                    index = cleanStringBuilder.Length
                                });
                            }
                            else if (parseArgs.Length == 2 &&
                                     float.TryParse(parseArgs[1].Replace(">", "")
                                         .Replace("/", ""), out var tmpFloat))
                            {
                                textMarks.Enqueue(new SpecialTextMark
                                {
                                    argument = $"{1f / tmpFloat}",
                                    effect = SpecialTextMark.Effect.TextSpeed,
                                    index = cleanStringBuilder.Length
                                });
                            }
                            else
                            {
                                Debug.LogWarning($"Failed to translate markup: {markupText}");
                            }
                        }
                        else if (markupText.Contains("showitem"))
                        {
                            var parseArgs = markupText.Trim('>').Split(' ');
                            if (parseArgs.Length != 0)
                            {
                                textMarks.Enqueue(new SpecialTextMark
                                {
                                    argument = string.Join(" ", parseArgs.Skip(1)),
                                    effect = SpecialTextMark.Effect.ShowItem,
                                    index = cleanStringBuilder.Length
                                });
                            }
                            else
                            {
                                Debug.LogWarning($"Failed to translate markup: {markupText}");
                            }
                        }
                        else if (markupText.Contains("hideitem"))
                        {
                            var parseArgs = markupText.Trim('>').Split(' ');
                            if (parseArgs.Length != 0)
                            {
                                textMarks.Enqueue(new SpecialTextMark
                                {
                                    argument = string.Join(" ", parseArgs.Skip(1)),
                                    effect = SpecialTextMark.Effect.HideItem,
                                    index = cleanStringBuilder.Length
                                });
                            }
                            else
                            {
                                Debug.LogWarning($"Failed to translate markup: {markupText}");
                            }
                        }
                        else
                        {
                            formattedStringBuilder.Append(markupBuilder);
                        }

                        markupBuilder.Clear();
                    }

                    continue;
                }

                if (c.Equals('<'))
                {
                    markupBuilder.Append(c);
                    continue;
                }

                #endregion for hiding markup

                formattedStringBuilder.Append(c);
                cleanStringBuilder.Append(c);
            }

            var formattedString = gameConfiguration.EnableTextFormatting ? 
                formattedStringBuilder.ToString() : cleanStringBuilder.ToString();
            var textLength = cleanStringBuilder.Length;
            dialogBox.SetInitialText(formattedString);
            var actualText = cleanStringBuilder.ToString();
            if (actualText.Trim().Length == 0)
            {
                userRequestedNextLine = true;
                textLength = 0;
            }
            else
            {
                _lastDialog = cleanStringBuilder.ToString();
            }

            var textSpeedMultiplier = 1f;
            if (Math.Abs(TextRate) > 0.00001f)
            {
                for (int i = 0; i < textLength; i++)
                {
                    if (textMarks.Count != 0 && textMarks.Peek().index <= i)
                    {
                        var effect = textMarks.Dequeue();
                        ExecuteEffect(effect, ref textSpeedMultiplier);
                    }

                    if (userRequestedNextLine)
                    {
                        userRequestedNextLine = false;
                        break;
                    }
                
                    dialogBox.ShowCharacters(i);
                    while (inputManager.inputState == InputState.Pause)
                    {
                        yield return new WaitForSeconds(1f/60f);
                    }
                    yield return new WaitForSeconds(TextRate * textSpeedMultiplier);
                }
            }
            
            // i don't know why the last character's not shown sometimes
            dialogBox.ShowCharacters(textLength + 10);

            while (textMarks.Count != 0)
            {
                ExecuteEffect(textMarks.Dequeue(), ref textSpeedMultiplier);
            }

            // Indicate to the rest of the game that the line has finished being delivered
            onLineFinishDisplaying?.Invoke();

            while (userRequestedNextLine == false)
            {
                yield return null;
            }

            userRequestedNextLine = false;

            // Avoid skipping lines if textSpeed == 0
            yield return new WaitForEndOfFrame();

            // Hide the text and prompt
            onLineEnd?.Invoke();

            // onLineUpdate.RemoveListener(textItem.UpdateLine);

            onComplete();
        }

        /// <summary>
        /// Executes effects that are placed as html tags in the script instead of yarn commands
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="textSpeedMultiplier"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void ExecuteEffect(SpecialTextMark effect, ref float textSpeedMultiplier)
        {
            switch (effect.effect)
            {
                case SpecialTextMark.Effect.TextSpeed:
                    if (float.TryParse(effect.argument, out var value))
                    {
                        textSpeedMultiplier = value;
                    }
                    break;
                case SpecialTextMark.Effect.ShowItem:
                    // customCommands.ShowItem(effect.argument.Split(' '));
                    break;
                case SpecialTextMark.Effect.HideItem:
                    // customCommands.HideItem(effect.argument.Split(' '));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Checks if the tag is whitelisted
        /// </summary>
        /// <param name="markupText"></param>
        /// <returns></returns>
        private bool IsWhiteListed(string markupText)
        {
            string markupLower = markupText.ToLower();
            foreach (var wholeMarkup in markupWholeWhitelist)
            {
                if (wholeMarkup.ToLower().Equals(markupLower))
                {
                    return true;
                }
            }

            foreach (var phraseMarkup in markupPhraseWhitelist)
            {
                if (markupLower.Contains(phraseMarkup.ToLower()))
                {
                    return true;
                }
            }
            
            Debug.Log($"Delete: {markupText}");
            
            return false;
        }

        /// <summary>
        /// Adjust markups that may contain special characters, like color
        /// </summary>
        /// <param name="markupBuilder"></param>
        private void AdjustMarkups(StringBuilder markupBuilder)
        {
            markupBuilder.Replace("<color=", "<color=#");
        }

        public override void RunOptions(OptionSet optionSet, ILineLocalisationProvider localisationProvider,
            Action<int> onOptionSelected)
        {
            StartCoroutine(DoRunOptions(optionSet, localisationProvider, onOptionSelected));
        }


        /// Show a list of options, and wait for the player to make a
        /// selection.
        private IEnumerator DoRunOptions(Yarn.OptionSet optionsCollection,
            ILineLocalisationProvider localisationProvider,
            System.Action<int> selectOption)
        {
            // iconManager.CreateButtons(optionsCollection.Options.Length);

            // Display each option in a button, and make it visible
            int i = 0;

            waitingForOptionSelection = true;

            currentOptionSelectionHandler = selectOption;

            // iconManager.InformShowingOptions();
            foreach (var optionString in optionsCollection.Options)
            {
                // iconManager.ActivateButtons(i, () => SelectOption(optionString.ID));

                var optionText = localisationProvider.GetLocalisedTextForLine(optionString.Line);

                if (optionText == null)
                {
                    Debug.LogWarning($"Option {optionString.Line.ID} doesn't have any localised text");
                    optionText = optionString.Line.ID;
                }

                // iconManager.SetButtonText(i, optionText);

                i++;
            }

            onOptionsStart?.Invoke();

            // Wait until the chooser has been used and then removed 
            while (waitingForOptionSelection)
            {
                yield return null;
            }

            // iconManager.HideAllButtons();

            onOptionsEnd?.Invoke();
        }


        /// Runs a command.
        /// <inheritdoc/>
        public override Dialogue.HandlerExecutionType RunCommand(Yarn.Command command, System.Action onCommandComplete)
        {
            // Dispatch this command via the 'On Command' handler.
            onCommand?.Invoke(command.Text);

            // Signal to the DialogueRunner that it should continue
            // executing. (This implementation of RunCommand always signals
            // that execution should continue, and never calls
            // onCommandComplete.)
            return Dialogue.HandlerExecutionType.ContinueExecution;
        }


        /// Called when the dialogue system has started running.
        /// <inheritdoc/>
        public override void DialogueStarted()
        {
            dialogBox.SetVisible(true);
            onDialogueStart?.Invoke();
        }

        /// Called when the dialogue system has finished running.
        /// <inheritdoc/>
        public override void DialogueComplete()
        {
            dialogBox.SetVisible(false);
            onDialogueEnd?.Invoke();
        }

        /// <summary>
        /// Signals that the user has finished with a line, or wishes to
        /// skip to the end of the current line.
        /// </summary>
        /// <remarks>
        /// This method is generally called by a "continue" button, and
        /// causes the DialogueUI to signal the <see
        /// cref="DialogueRunner"/> to proceed to the next piece of
        /// content.
        ///
        /// If this method is called before the line has finished appearing
        /// (that is, before <see cref="onLineFinishDisplaying"/> is
        /// called), the DialogueUI immediately displays the entire line
        /// (via the <see cref="onLineUpdate"/> method), and then calls
        /// <see cref="onLineFinishDisplaying"/>.
        /// </remarks>
        public void MarkLineComplete()
        {
            userRequestedNextLine = true;
        }

        /// <summary>
        /// Signals that the user has selected an option.
        /// </summary>
        /// <remarks>
        /// This method is called by the <see cref="Button"/>s in the <see
        /// cref="optionButtons"/> list when clicked.
        ///
        /// If you prefer, you can also call this method directly.
        /// </remarks>
        /// <param name="optionID">The <see cref="OptionSet.Option.ID"/> of
        /// the <see cref="OptionSet.Option"/> that was selected.</param>
        public void SelectOption(int optionID)
        {
            if (waitingForOptionSelection == false)
            {
                Debug.LogWarning("An option was selected, but the dialogue UI was not expecting it.");
                return;
            }

            waitingForOptionSelection = false;
            currentOptionSelectionHandler?.Invoke(optionID);
        }

        public void ShowElements(bool shouldShow)
        {
            // iconManager.ShowElements(shouldShow);
        }

        public void WriteAutoSave()
        {
            // todo: auto save here
            // saveClient.autoSave.currentSpeaker = _lastSpeaker;
        }

        public void SetFakeLastDialog(string speaker, string message)
        {
            _lastSpeaker = speaker;
            _lastDialog = message;
        }
    }

    public class SpecialTextMark
    {
        public string argument = "";
        public int index = 0;
        public Effect effect;

        public enum Effect
        {
            TextSpeed,
            ShowItem,
            HideItem
        }
    }
}