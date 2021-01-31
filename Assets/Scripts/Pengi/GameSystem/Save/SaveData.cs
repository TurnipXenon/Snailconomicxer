using System;
using System.Collections.Generic;
using Yarn.Unity;

namespace Pengi.GameSystem.Save
{
    /// <summary>
    /// This class holds all serializable variables for one save slot.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public List<InMemoryVariableStorage.DefaultVariable> savedVariables = 
            new List<InMemoryVariableStorage.DefaultVariable>();

        public SaveData(SaveData rhs)
        {
            Overwrite(rhs);
        }
    
        public SaveData()
        {
        }

        protected internal static SaveData AsNull()
        {
            return new SaveData();
        }

        public static SaveData AsNewGame()
        {
            return new SaveData();
        }

        public void Overwrite(SaveData rhs)
        {
            savedVariables.Clear();
            foreach (var variable in rhs.savedVariables)
            {
                savedVariables.Add(new InMemoryVariableStorage.DefaultVariable()
                {
                    name = variable.name,
                    type = variable.type,
                    value = variable.value
                });
            }
        }
    }
}