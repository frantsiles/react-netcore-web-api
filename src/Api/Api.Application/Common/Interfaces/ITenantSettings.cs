namespace Api.Application.Common.Interfaces;

public interface ITenantSettings
{
    T Get<T>(string key, T defaultValue);
    bool GetBool(string key, bool defaultValue = false);
    int GetInt(string key, int defaultValue = 0);
    decimal GetDecimal(string key, decimal defaultValue = 0m);
    string GetString(string key, string defaultValue = "");
}
