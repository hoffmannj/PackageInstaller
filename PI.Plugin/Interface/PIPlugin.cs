
namespace PI.Plugin.Interface
{
    public interface PIPlugin
    {
        string BlockName();
        void Process(string jsonBlock, MainParameters mainParameters);
    }
}
