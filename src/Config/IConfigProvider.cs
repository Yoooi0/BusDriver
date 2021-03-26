using SimpleJSON;

namespace BusDriver.Config
{
    public interface IConfigProvider
    {
        void StoreConfig(JSONNode config);
        void RestoreConfig(JSONNode config);
    }
}
