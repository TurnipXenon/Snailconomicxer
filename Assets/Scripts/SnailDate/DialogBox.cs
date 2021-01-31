using Pengi.Dialog;
using TMPro;
using UnityEngine;

namespace SnailDate
{
    public class DialogBox : MonoBehaviour
    {
        public TextMeshProUGUI dialogText;
        
        public DialogBoxReturn InformSpeaker(string speakerText)
        {
            // todo: load icon
            var ret = new DialogBoxReturn();
            // todo: block dialog if still needs to load dialog
            // todo: set real name to ret
            return ret;
        }

        public void SetInitialText(string formattedString)
        {
            dialogText.text = formattedString;
            dialogText.maxVisibleCharacters = 0;
        }

        public void ShowCharacters(int i)
        {
            dialogText.maxVisibleCharacters = i;
        }
    }

    public class DialogBoxReturn
    {
        public DialogueBlocker dialogueBlocker = new DialogueBlocker();
        public string realName = "";
    }
}