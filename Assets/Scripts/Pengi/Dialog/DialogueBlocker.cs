namespace Pengi.Dialog
{
    public class DialogueBlocker
    {
        private bool _isBlocking = false;
        public bool IsBlocking => _isBlocking;

        public void Unblock()
        {
            _isBlocking = false;
        }

        public void Block()
        {
            _isBlocking = true;
        }
    }
}