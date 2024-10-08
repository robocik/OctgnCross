using Octgn.Communication.Tcp;
using Octgn.Library.Communication;
using Octgn.Online;

namespace OctgnMaui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            var handshaker = new DefaultHandshaker();
            var connectionCreator = new TcpConnectionCreator(handshaker);
            var lobbyClientConfig = new LibraryCommunicationClientConfig(connectionCreator);

            MauiProgram.LobbyClient = new Client(
                lobbyClientConfig,
                typeof(App).Assembly.GetName().Version
            );
        }
    }
}
