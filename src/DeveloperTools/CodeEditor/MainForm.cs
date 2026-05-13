using System.Diagnostics;
using Chow.Interpreter;

namespace CodeEditor;

public partial class MainForm : Form
{
    private const int WAIT_STATE_POLL_MS = 100;
    private const long START_MODULE_TIMEOUT_MS = 10000; // 10 seconds

    enum EditorState { Idle, StartingModuleTask, ModuleExecuteFailure, ExecutingModule, ExitingModuleTask }

    private static readonly Lock _stateLock = new();
    private Task? _moduleTask = null;
    EditorState _state = EditorState.Idle;

    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {

    }

    private void newToolStripMenuItem_Click(object sender, EventArgs e)
    {

    }

    private void toolStripMenuItem1_Click(object sender, EventArgs e)
    {

    }

    void executeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        TransitionState(EditorState.Idle, EditorState.StartingModuleTask);
        executeToolStripMenuItem.Enabled = false;

        var module = new ChowModule();
        var srcCode = inputTextArea.Text;
        
        _moduleTask = Task.Run(() =>
        {
            TransitionState(EditorState.StartingModuleTask, EditorState.ExecutingModule);
            
            // This could cause a race condition, but it is for GUI and does not control app state
            stopToolStripMenuItem.Enabled = true;

            module.Execute(srcCode);
            TransitionState(EditorState.ExecutingModule, EditorState.ExitingModuleTask);
        });

        WaitForModuleExit();
    }

    async void WaitForModuleExit()
    {
        var sw = Stopwatch.StartNew();

        while (!IsState(EditorState.ExecutingModule))
        {
            await Task.Delay(WAIT_STATE_POLL_MS);

            if (sw.ElapsedMilliseconds > START_MODULE_TIMEOUT_MS)
            {
                TransitionState(EditorState.StartingModuleTask, EditorState.Idle);
                MessageBox.Show("Failed to start module within timeout.");
                _moduleTask?.Dispose();
                _moduleTask = null;
                TransitionState(EditorState.StartingModuleTask, EditorState.ModuleExecuteFailure);
                return;
            }
        }

        while (!IsState(EditorState.ExitingModuleTask))
        {
            await Task.Delay(WAIT_STATE_POLL_MS);
        }

        TransitionState(EditorState.ExitingModuleTask, EditorState.Idle);
    }

    private void textArea_TextChanged(object sender, EventArgs e)
    {
        
    }

    bool IsState(EditorState state)
    {
        lock (_stateLock)
        {
            return _state == state;
        }
    }

    void TransitionState(EditorState fromState, EditorState toState)
    {
        lock (_stateLock)
        {
            if (_state != fromState)
            {
                throw new InvalidOperationException($"Invalid state transition: expected {fromState}, but was {_state}");
            }

            _state = toState;
            outputTextArea.Text += $"\n{GetStateMessage()}";

            UpdateGuiState();
        }
    }

    string GetStateMessage()
    {
        lock (_stateLock)
        {

        }

        switch (_state)
        {
            case EditorState.Idle:
                return "";
            case EditorState.ExecutingModule:
                return "Starting execution...";
            case EditorState.ExitingModuleTask:
                return "Execution complete.";
            case EditorState.ModuleExecuteFailure:
                return "Module failed to execute.";
            case EditorState.StartingModuleTask:
                return "Starting module...";
            default:
                throw new InvalidOperationException();
        }
    }

    void UpdateGuiState()
    {
        // Caller is assumed to have already acquired the state lock
        switch (_state)
        {
            case EditorState.Idle:
                inputTextArea.Enabled = true;
                executeToolStripMenuItem.Enabled = true;
                stopToolStripMenuItem.Enabled = false;
                break;

            case EditorState.StartingModuleTask:
                inputTextArea.Enabled = false;
                executeToolStripMenuItem.Enabled = false;
                stopToolStripMenuItem.Enabled = false;
                break;

            case EditorState.ExecutingModule:
                inputTextArea.Enabled = false;
                executeToolStripMenuItem.Enabled = false;
                stopToolStripMenuItem.Enabled = true;
                break;

            case EditorState.ExitingModuleTask:
                inputTextArea.Enabled = false;
                executeToolStripMenuItem.Enabled = false;
                stopToolStripMenuItem.Enabled = false;
                break;

             default:
                throw new InvalidOperationException();
        }
    }
}
