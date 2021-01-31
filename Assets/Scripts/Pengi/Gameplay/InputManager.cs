using System;
using Pengi.Dialog;
using SnailDate;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pengi.Gameplay
{
    /// <summary>
    /// InputManager distributes the input to different systems, and it will block or modify some inputs depending on its state.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public DialogueUIManager dialogueUiManager;
        public float clickDelay = 0.25f;
        public float androidTouchDelay = 0.1f;
        public SnailNPC testSnail;
        public CustomCommands customCommands;

        [HideInInspector] public InputState inputState = InputState.MainDialogue;

        private Camera _camera;
        private float _toleratedTime;
        private bool _requestRaycast = false;

        private void Awake()
        {
            Debug.Assert(dialogueUiManager != null);

            _camera = Camera.main;
        }

        public void ContinueText(InputAction.CallbackContext context)
        {
            if (context.started && Time.time > _toleratedTime)
            {
                switch (inputState)
                {
                    case InputState.MainDialogue:
                        dialogueUiManager.MarkLineComplete();
                        break;
                    case InputState.Overworld:
                        break;
                    case InputState.Pause:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                Vector3 position;
                position = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

                var hit = Physics2D.Raycast(position, Vector2.zero);

                if (hit)
                {
                    var clickableItem = hit.transform.gameObject.GetComponent<IClickable>();

                    switch (inputState)
                    {
                        case InputState.MainDialogue:
                            break;
                        case InputState.Overworld:
                            // clickableItem?.OnClick();
                            break;
                        case InputState.Pause:
                            // if we can cast it to ClickableItem, go activate
                            (clickableItem as ClickableItem)?.OnClick();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            
            
            var ray3d = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hitInfo;
            if (Physics.Raycast(ray3d.origin, ray3d.direction, out hitInfo))
            {
                      
                switch (inputState)
                {
                    case InputState.MainDialogue:
                        break;
                    case InputState.Overworld:
                        testSnail.SetTarget(hitInfo.point);
                        break;
                    case InputState.Pause:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }          
            }

            // for mouse clicks:
            ContinueText(context);
        }

        public void SetInputState(InputState state)
        {
            _toleratedTime = Time.time + clickDelay;
            inputState = state;
        }
    }


    public enum InputState
    {
        MainDialogue,
        Overworld,
        Pause
    }
}