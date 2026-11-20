using Microsoft.Extensions.Configuration;

namespace InvestmentViewer;

public class ConfigFactory
{
    public static IConfigurationRoot Create()
    {
        string pathConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"config.ini");
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddIniFile(pathConfig, optional: false, reloadOnChange: false)
            .Build();
        
        return config;
    }
}