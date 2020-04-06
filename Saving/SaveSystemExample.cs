using Spellplague.Characters;
using Spellplague.Saving;
using System;
using UnityEngine;

public class SaveSystemExample : Character, ISaveable
{
    public string SaveFileName { get; set; } = "test";

    public void Awake()
    {
        SaveSystem.AddSaveable(this);
    }

    public void Load()
    {
        Debug.Log($"Loaded {SaveFileName}");
        SaveSystemExampleData saveData = SaveSystem.Load<SaveSystemExampleData>(FileType.XML, SaveFileName);
        if (saveData != null)
        {
            Health.Value = saveData.health;
            transform.position = new Vector3(saveData.xPos, saveData.yPos, saveData.zPos);
        }
    }

    public void Save()
    {
        Debug.Log($"Saved {SaveFileName}");
        SaveSystem.Save(FileType.XML, SaveFileName,
            new SaveSystemExampleData
            {
                health = Health.Value,
                xPos = transform.position.x,
                yPos = transform.position.y,
                zPos = transform.position.z
            });
    }
}

[Serializable]
public class SaveSystemExampleData
{
    public float health;
    public float xPos;
    public float yPos;
    public float zPos;

    public SaveSystemExampleData() { }
}