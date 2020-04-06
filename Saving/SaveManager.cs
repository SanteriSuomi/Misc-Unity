using UnityEngine;

namespace Spellplague.Saving
{
    public class SaveManager : MonoBehaviour
    {
        private void Start()
        {
            SaveSystem.LoadSaveables();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                SaveSystem.SaveSaveables();
            }
            else if (Input.GetKeyDown(KeyCode.PageUp))
            {
                SaveSystem.Clear();
            }
        }
    }
}