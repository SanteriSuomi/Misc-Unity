using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Saving
{
    public class SaveManagerExample : MonoBehaviour
    {
        private void Awake()
        {
            SaveSystem.Decompress();
        }

        private void OnEnable()
        {
            SaveSystem.LoadSaveables();
        }

        //private void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.Insert))
        //    {
        //        SaveSystem.SaveSaveables();
        //    }
        //    else if (Input.GetKeyDown(KeyCode.Home))
        //    {
        //        SaveSystem.LoadSaveables();
        //    }
        //}

        private void OnDisable()
        {
            SaveSystem.SaveSaveables();
        }

        private void OnDestroy()
        {
            SaveSystem.Compress(CompressionLevel.Fastest);
        }
    }
}