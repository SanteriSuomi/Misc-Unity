using System;
using UnityEngine;

namespace Saving
{
    public class SaveSystemExample : MonoBehaviour, ISaveable
    {
        public string GetFileName()
        {
            return GetType().Name;
        }

        private void Awake()
        {
            SaveSystem.AddSaveable(this);
        }

        public void Load()
        {
            Debug.Log($"Loaded {GetFileName()}");

            SaveSystemExampleData save = SaveSystem.Load<SaveSystemExampleData>(FileType.JSON, GetFileName());
            if (save != default)
            {
                transform.position = new Vector3(save.xPos, save.yPos, save.zPos);
                transform.rotation = Quaternion.Euler(save.xRot, save.yRot, save.zRot);
            }
        }

        public void Save()
        {
            Debug.Log($"Saved {GetFileName()}");

            SaveSystem.Save(FileType.JSON, GetFileName(),
                new SaveSystemExampleData
                {
                    xPos = transform.position.x,
                    yPos = transform.position.y,
                    zPos = transform.position.z,

                    xRot = transform.rotation.eulerAngles.x,
                    yRot = transform.rotation.eulerAngles.y,
                    zRot = transform.rotation.eulerAngles.z
                });
        }

        [Serializable]
        private class SaveSystemExampleData : SaveData
        {
            public float xPos;
            public float yPos;
            public float zPos;

            public float xRot;
            public float yRot;
            public float zRot;
        }
    }
}