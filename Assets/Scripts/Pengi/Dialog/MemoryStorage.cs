using System.Linq;
using Pengi.GameSystem.Save;
using Yarn.Unity;

namespace Pengi.Dialog
{
    /// <summary>
    /// Modified version of InMemoryVariableStorage
    /// </summary>
    public class MemoryStorage : InMemoryVariableStorage
    {
        public void Write(SaveData saveData)
        {
            saveData.savedVariables.Clear();
            foreach (var pair in this.AsEnumerable())
            {
                saveData.savedVariables.Add(
                    new DefaultVariable
                    {
                        name = pair.Key.Remove(0, 1),
                        value = pair.Value.AsString,
                        type = pair.Value.type
                    }
                );
            }
        }
    }
}