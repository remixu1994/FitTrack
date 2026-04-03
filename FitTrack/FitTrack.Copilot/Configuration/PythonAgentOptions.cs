namespace FitTrack.Copilot.Configuration;

public class PythonAgentOptions
{
    public const string SectionName = "PythonAgent";

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "http://127.0.0.1:8010";

    public int TimeoutSeconds { get; set; } = 15;
}
