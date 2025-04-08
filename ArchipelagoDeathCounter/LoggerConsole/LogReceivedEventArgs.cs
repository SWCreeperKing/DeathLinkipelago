namespace ArchipelagoDeathCounter.LoggerConsole;

public class LogReceivedEventArgs(Logger.Level logMessageLevel, string timeOfMessage, string logMessage, string sender)
    : EventArgs
{
    public readonly Logger.Level LogMessageLevel = logMessageLevel;
    public readonly string Sender = sender;
    public readonly string TimeOfMessage = timeOfMessage;
    public readonly string LogMessage = logMessage;
}