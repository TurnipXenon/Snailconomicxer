﻿using Pengi.Dialog;
using Pengi.Gameplay;
using UnityEngine;

namespace SnailDate
{
    public class SnailDateCommands : CustomCommands
    {
        public SnailDateDirector director;
        
        protected void Awake()
        {
            base.Awake();
            
            dialogueRunner.AddCommandHandler("setGameState", SetGameState);
        }

        public void SetGameState(string[] parameters)
        {
            if (parameters.Length != 1)
            {
                Debug.LogError("SetGameState: Game state not equal to 1!");
                return;
            }

            var smallArg = parameters[0].ToLower();
            const string gameState = "$gameState";

            if (smallArg.Equals("beginning"))
            {
                // todo: game set game state
                gameConfiguration.SetGameState(SnailGameState.Beginning);
                memoryStorage.SetValue(gameState, 0);
                inputManager.SetInputState(InputState.MainDialogue);
            }
            else if (smallArg.Equals("overworldstart"))
            {
                // todo: game set game state
                gameConfiguration.SetGameState(SnailGameState.OverworldStart);
                memoryStorage.SetValue(gameState, 1);
                inputManager.SetInputState(InputState.Overworld);
            }
            else if (smallArg.Equals("aaronphase"))
            {
                // todo: game set game state
                gameConfiguration.SetGameState(SnailGameState.AaronPhase);
                memoryStorage.SetValue(gameState, 2);
            }
            else if (smallArg.Equals("carlosphase"))
            {
                // todo: game set game state
                gameConfiguration.SetGameState(SnailGameState.CarlosPhase);
                memoryStorage.SetValue(gameState, 3);
            }
            else if (smallArg.Equals("leophase"))
            {
                // todo: game set game state
                gameConfiguration.SetGameState(SnailGameState.LeoPhase);
                memoryStorage.SetValue(gameState, 4);
            }
            else if (smallArg.Equals("gameend"))
            {
                // todo: game set game state
                gameConfiguration.SetGameState(SnailGameState.GameEnd);
                memoryStorage.SetValue(gameState, 5);
            }
            else
            {
                Debug.LogError($"SetGameState: Unknown game state: {parameters[0]}");
                return;
            }
            
            
            director.OnStateStart();
        }
    }
}
