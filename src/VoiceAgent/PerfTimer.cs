using System.Diagnostics;

namespace VoiceAgent;

public sealed class PerfTimer(ILogger _logger)
{
    private string? _name;
    private Stopwatch _stopwatch = new();
    public void Start(string name)
    {
        _name = name;
        _stopwatch.Restart();
    }
    public void Stop()
    {
        if (_name is not null)
        {
            _stopwatch.Stop();
            _logger.LogTrace("{Name} took {ElapsedMilliseconds}ms", _name, _stopwatch.ElapsedMilliseconds);
            _name = null;
        }
    }
    public void StopWith(string extra)
    {
        if (_name is not null)
        {
            _stopwatch.Stop();
            _logger.LogTrace("{Name} took {ElapsedMilliseconds}ms, {Extra}", _name, _stopwatch.ElapsedMilliseconds, extra);
            _name = null;
        }
    }
    public void StopStart(string name)
    {
        Stop();
        Start(name);
    }
}