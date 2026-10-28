using Microsoft.Extensions.Logging;
using VanderPan.App.Services;

namespace VanderPan.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Serviços da Aplicação Mobile
		builder.Services.AddSingleton<SessionService>();
		builder.Services.AddSingleton<MobileApiService>();
		builder.Services.AddSingleton<CartService>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
