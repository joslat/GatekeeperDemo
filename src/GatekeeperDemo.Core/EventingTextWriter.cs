using AgentEval.PartnerDeskDemo.Demo;
using System.Text;

namespace GatekeeperDemo.Core;

/// <summary>Projects the imported demo's original narration into the Debug stream one complete line at a time.</summary>
internal sealed class EventingTextWriter(IPartnerDeskRuntimeEventSink events) : TextWriter
{
    private readonly object _sync = new();
    private readonly StringBuilder _line = new();

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        lock (_sync)
        {
            if (value == '\n')
            {
                FlushLine();
                return;
            }

            if (value != '\r')
            {
                _line.Append(value);
            }
        }
    }

    public override void Write(string? value)
    {
        if (value is null)
        {
            return;
        }

        foreach (var character in value)
        {
            Write(character);
        }
    }

    public override void WriteLine(string? value)
    {
        Write(value);
        Write('\n');
    }

    private void FlushLine()
    {
        var text = _line.ToString();
        _line.Clear();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        events.Emit(new(
            PartnerDeskRuntimeEventKind.Diagnostic,
            "runtime",
            "debug",
            "Runtime narration",
            text.TrimEnd(),
            PartnerDeskRuntimeDisposition.Neutral));
    }
}
