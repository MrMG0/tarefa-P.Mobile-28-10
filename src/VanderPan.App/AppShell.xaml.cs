namespace VanderPan.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.RoteiroPage), typeof(Views.RoteiroPage));
	}
}
