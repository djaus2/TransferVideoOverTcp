using Microsoft.Extensions.Logging;
using SendVideoOverTCPLib.Services;
using SendVideoOverTCPLib.Platforms.Android;

namespace SendVideo;

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

#if DEBUG
		builder.Logging.AddDebug();
#endif
        builder.Services.AddTransient<SendVideoOverTCPLib.ViewModels.NetworkViewModel>();
        builder.Services.AddTransient<MainPage>();
        
        // Register the VideoMetadataService
#if ANDROID
        builder.Services.AddSingleton<IVideoMetadataService, VideoMetadataService>();
#else
        builder.Services.AddSingleton<IVideoMetadataService, DefaultVideoMetadataService>();
#endif
        return builder.Build();
	}
}
