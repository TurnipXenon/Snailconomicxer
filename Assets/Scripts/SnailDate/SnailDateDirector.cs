using System;
using Pengi.Manager;
using Yarn.Unity;

namespace SnailDate
{
    public class SnailDateDirector : DialogSceneManager
    {
        public DialogueRunner dialogueRunner;
    
        private void Start()
        {
            dialogueRunner.StartDialogue("Start_Start");
        }
    }
}