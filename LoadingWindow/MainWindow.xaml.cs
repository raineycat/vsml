using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Windows;

namespace LoadingWindow;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private NamedPipeServerStream _pipe;

    public string CurrentStatus { get; set; } = "Setting up";
    
    public MainWindow()
    {
        var pipeName = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();
        if(pipeName == null)
            Environment.Exit(1);

        _pipe = new NamedPipeServerStream(pipeName);
        Task.Run(CommunicationThread);
        
        InitializeComponent();
        DataContext = this;
    }

    private void CommunicationThread()
    {
        _pipe.WaitForConnection();
        using var reader = new StreamReader(_pipe);
        
        while (_pipe.IsConnected)
        {
            var data = reader.ReadLine();
            if(data == null)
                continue;
            
            CurrentStatus = data;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentStatus)));
        }
        
        Environment.Exit(0);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}