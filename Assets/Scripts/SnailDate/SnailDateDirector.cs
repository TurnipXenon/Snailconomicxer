using System;
using Pengi.Dialog;
using Pengi.Gameplay;
using Pengi.Manager;
using UnityEditor;
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
        public float distanceSqrTolerance = 1f;

        [Header("Snails")]
        public SnailNPC aaronSnail;
        public SnailNPC leoSnail;
        public SnailNPC carlosSnail;

        [Header("Seats")] 
        public Transform leftTableLeft;
        public Transform leftTableRight;
        public Transform rightTableLeft;
        public Transform rightTableRight;

        private bool _isPhase3Triggered = false;
        private bool _isSeatedWithAaron = false;
        private bool _isSeatedWithLeo = false;
        private bool _isSeatedWithCarlos = false;
        
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
            var player = gameConfiguration.GetPlayer();
            
            switch (gameConfiguration.GameState)
            {
                case SnailGameState.Beginning:
                    break;
                case SnailGameState.OverworldStart:
                    if (!_isPhase3Triggered && player.transform.position.x
                        < phase3Trigger.transform.position.x)
                    {
                        gameConfiguration.StartDialogue("Start_Intro");
                        _isPhase3Triggered = true;
                    }
                    break;
                case SnailGameState.AaronPhase:
                    // if (!_isSeatedWithAaron &&
                    //     (player.transform.position - rightTableRight.position).sqrMagnitude < distanceSqrTolerance)
                    // {
                    //     gameConfiguration.StartDialogue("Aaron_Start");
                    //     var agent = player.GetComponent<SnailNPC>();
                    //     agent.SetTarget(rightTableRight.position);
                    //     _isSeatedWithAaron = true;
                    // }
                    PrepareSeat(player, rightTableRight, "Aaron_Start", ref _isSeatedWithAaron);
                    break;
                case SnailGameState.CarlosPhase:
                    PrepareSeat(player, leftTableRight, "Carlos_Start", ref _isSeatedWithCarlos);
                    break;
                case SnailGameState.LeoPhase:
                    PrepareSeat(player, rightTableLeft, "Leo_Start", ref _isSeatedWithLeo);
                    break;
                case SnailGameState.GameEnd:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void PrepareSeat(SnailNPC player, Transform table, string nodeName, ref bool isSeatedWith)
        {
            if (!isSeatedWith &&
                (player.transform.position - table.position).sqrMagnitude < distanceSqrTolerance)
            {
                gameConfiguration.StartDialogue(nodeName);
                player.SetTarget(table.position);
                isSeatedWith = true;
            }
        }

        public void OnStateStart()
        {
            switch (gameConfiguration.GameState)
            {
                case SnailGameState.Beginning:
                    break;
                case SnailGameState.OverworldStart:
                    break;
                
                case SnailGameState.AaronPhase:
                    aaronSnail.SetTarget(rightTableLeft.position);
                    carlosSnail.SetTarget(leftTableLeft.position);
                    leoSnail.SetTarget(leftTableRight.position);
                    // rightTableRight = you
                    break;
                
                case SnailGameState.CarlosPhase:
                    aaronSnail.SetTarget(rightTableLeft.position);
                    leoSnail.SetTarget(rightTableRight.position);
                    // leftTableRight = you
                    break;
                
                case SnailGameState.LeoPhase:
                    aaronSnail.SetTarget(rightTableRight.position);
                    leoSnail.SetTarget(leftTableRight.position);
                    // rightTableLeft = you
                    break;
                
                case SnailGameState.GameEnd:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}