namespace Pengi.GameSystem.Save
{
    /// <summary>
    /// A component which allows a class to listen whenever they need to update data
    /// into autosave.
    /// </summary>
    public class SaveClient
    {
        public SaveData currentSave;
        public SaveData autoSave;
        public ISaveClientCallback saveClientCallback;

        public void TryAutoSaveWrite()
        {
            saveClientCallback.WriteAutoSave();
        }
    }

    public interface ISaveClientCallback
    {
        void WriteAutoSave();
    }
}