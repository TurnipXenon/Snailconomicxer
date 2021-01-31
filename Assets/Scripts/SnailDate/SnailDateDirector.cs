using System;
using Pengi.Dialog;
using Pengi.Gameplay;
using Pengi.Manager;
using UnityEngine;
using Yarn.Unity;

namespace SnailDate
{
    public class SnailDateDirector : DialogSceneManager
    {
        public InputManager inputManager;
        public DialogueRunner dialogueRunner;
        public SnailDateCommands customCommands;
        public Transform phase3Trigger;

        protected new void OnEnable()
        {
            base.OnEnable();
            gameConfiguration.SetInputManager(inputManager);
            gameConfiguration.SetDialogueRunner(dialogueRunner);
        }

        protected new void OnDisable()
        {
            base.OnDisable();
            gameConfiguration.SetInputManager(null);
            gameConfiguration.SetDialogueRunner(null);
        }

        private void Update()
        {
            switch (gameConfiguration.GameState)
            {
                case SnailGameState.Beginning:
                    break;
                case SnailGameState.OverworldStart:
                    if (gameConfiguration.GetPlayer().transform.position.x
                        < phase3Trigger.transform.position.x)
                    {
                        customCommands.SetGameState(new string[]{"aaronphase"});
                        gameConfiguration.StartDialogue("Start_Intro");
                    }
                    break;
                case SnailGameState.AaronPhase:
                    break;
                case SnailGameState.CarlosPhase:
                    break;
                case SnailGameState.LeoPhase:
                    break;
                case SnailGameState.GameEnd:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}