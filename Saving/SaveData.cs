namespace Saving
{
    abstract class SaveData
    {
        public string objName;

        protected SaveData()
        {
            objName = GetType().Name;
        }
    }
}