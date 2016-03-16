
namespace PI.Plugin.Interface
{
    public interface PIPlugin
    {
        string BlockType();
        void Process(string jsonBlock, MainParameters mainParameters);
    }
}
