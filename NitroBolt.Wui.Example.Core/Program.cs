using NitroBolt.Wui.Example.Core;
using Newtonsoft.Json;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
		.WriteTo.File(
			Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "", "Logs", "site.log"),
			fileSizeLimitBytes: 500000,
			encoding: Encoding.UTF8,
			outputTemplate: "[{Timestamp:dd.MM.yyyy HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}"
		)
		.CreateLogger();

try
{
	Log.Information("{0}{0}{0}{1}{0}", System.Environment.NewLine,
		"<===============================================================================>"
	);

	Log.Information("Приложение стартовало");

	try
	{
		Encoding encoding = Encoding.GetEncoding(1251);
	}
	catch (Exception e)
	{
		Log.Error(e, "Тестовая ошибка");
	}

	var builder = WebApplication.CreateBuilder(args);

	builder.Services.AddControllers().AddNewtonsoftJson();

	var app = builder.Build();

	app.UseRouting();
	app.UseStaticFiles();
	app.UseAuthorization();

	app.MapControllers();

	Log.Information("Запускаем приложение");

	app.Run();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
	Log.CloseAndFlush();
}
