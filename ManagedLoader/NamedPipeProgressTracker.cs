using System.Collections.Concurrent;
using System.IO.Pipes;
using ModContract;

namespace ManagedLoader;

public class NamedPipeProgressTracker : IProgressTracker, IDisposable
{
    private NamedPipeClientStream _pipe;
    private ConcurrentQueue<string> _updates;
    
    public NamedPipeProgressTracker(string pipeName)
    {
        _pipe = new NamedPipeClientStream(pipeName);
        _updates = [];
        _pipe.Connect();
        Task.Run(CommunicationThread);
    }
    
    public void SetCurrentStep(string name)
    {
        _updates.Enqueue(name);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _pipe.Dispose();
    }

    private void CommunicationThread()
    {
        using var writer = new StreamWriter(_pipe);
        while (_pipe.IsConnected)
        {
            if (_updates.TryDequeue(out var update))
            {
                writer.WriteLine(update);
                writer.Flush();
            }
        }   
    }
}