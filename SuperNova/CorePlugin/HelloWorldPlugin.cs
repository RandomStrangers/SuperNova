using System;
using SuperNova.Tasks;

namespace SuperNova
{
    public class HelloWorld : Plugin
    {
        public override string name { get { return "Saying hello"; } } // to unload, /punload hello
        public override string creator { get { return Server.SoftwareName + " team"; } }
        public override string SuperNova_Version { get { return Server.Version; } }
        public override void Load(bool startup)
        {
            Server.Background.QueueOnce(SayHello, null, TimeSpan.FromSeconds(10));
        }

        void SayHello(SchedulerTask task)
        {
#if DEV_BUILD_NOVA
            Command.Find("say").Use(Player.Nova, "Hello World!");
#else
            Command.Find("say").Use(Player.Console, "Hello World!");
#endif
            Logger.Log(LogType.SystemActivity, "&fHello World!");
        }
        public override void Unload(bool shutdown)
        {
        }

        public override void Help(Player p)
        {
            p.Message("");
        }
    }
}