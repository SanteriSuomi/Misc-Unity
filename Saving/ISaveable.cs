namespace Essentials.Saving
{
    public interface ISaveable
    {
        void Load(SaveData saveData);
        SaveData GetSave();
    }
}