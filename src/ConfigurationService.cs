using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Tabavoco;

public class ConfigurationService
{
    private readonly string _configFilePath;
    private readonly Dictionary<string, object> _values = new();

    public ConfigurationService()
    {
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        LoadConfiguration();
    }

    public double WindowLeft
    {
        get => GetValue<double>("WindowLeft");
        set => SetValue("WindowLeft", value);
    }

    public double WindowTop
    {
        get => GetValue<double>("WindowTop");
        set => SetValue("WindowTop", value);
    }

    public bool ShowMediaControls
    {
        get => GetValue<bool>("ShowMediaControls");
        set => SetValue("ShowMediaControls", value);
    }

    public void Save()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            WindowSettings = new
            {
                WindowLeft = WindowLeft,
                WindowTop = WindowTop,
                ShowMediaControls = ShowMediaControls
            }
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        
        File.WriteAllText(_configFilePath, json);
    }

    private void LoadConfiguration()
    {
        if (!File.Exists(_configFilePath))
            return;

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        
        var configuration = builder.Build();
        
        _values["WindowLeft"] = configuration.GetValue<double>("WindowSettings:WindowLeft");
        _values["WindowTop"] = configuration.GetValue<double>("WindowSettings:WindowTop");
        _values["ShowMediaControls"] = configuration.GetValue<bool>("WindowSettings:ShowMediaControls");
    }

    private T GetValue<T>(string key)
    {
        if (_values.TryGetValue(key, out var value))
            return (T)value;
        
        return default(T)!;
    }

    private void SetValue(string key, object value)
    {
        _values[key] = value;
    }
}