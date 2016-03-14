
namespace PI.Plugin.Exception
{
    public class PluginException : System.Exception
    {
        public bool Critical { get; private set; }

        public PluginException()
            : base()
        {
            Critical = false;
        }

        public PluginException(bool critical)
            : base()
        {
            Critical = critical;
        }

        public PluginException(string message)
            : base(message)
        {
            Critical = false;
        }

        public PluginException(bool critical, string message)
            : base(message)
        {
            Critical = critical;
        }
    }
}
