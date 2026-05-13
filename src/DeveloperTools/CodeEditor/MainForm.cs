using System.Diagnostics;
using System.Text;

namespace CodeEditor;

public partial class MainForm : Form
{
    const string FileExtension = ".chw";
    const string FileFilter = "Chow files (*.chw)|*.chw";
    const string UntitledName = "Untitled";

    enum EditorState
    {
        Idle,
        Running,
        Stopping
    }

    Process? _runProcess;
    string? _runTempFilePath;
    string? _currentFilePath;
    bool _isDirty;
    bool _isLoadingDocument;
    bool _isStoppingExecution;
    EditorState _state = EditorState.Idle;

    public MainForm()
    {
        InitializeComponent();
        WireEvents();
        ConfigureDialogs();
        SetState(EditorState.Idle);
        UpdateTitle();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
    }

    void WireEvents()
    {
        openToolStripMenuItem.Click += openToolStripMenuItem_Click;
        saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
        saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
        stopToolStripMenuItem.Click += stopToolStripMenuItem_Click;
        copyOutputToolStripMenuItem.Click += copyOutputToolStripMenuItem_Click;
        copyInputToolStripMenuItem.Click += copyInputToolStripMenuItem_Click;
        clearOutputToolStripMenuItem.Click += clearOutputToolStripMenuItem_Click;
        clearInputToolStripMenuItem.Click += clearInputToolStripMenuItem_Click;
        inputTextArea.TextChanged += inputTextArea_TextChanged;
        FormClosing += MainForm_FormClosing;
    }

    void ConfigureDialogs()
    {
        openFileDialog.Filter = FileFilter;
        openFileDialog.DefaultExt = FileExtension.TrimStart('.');
        openFileDialog.AddExtension = true;
        openFileDialog.CheckFileExists = true;
        openFileDialog.Multiselect = false;
        openFileDialog.FileName = string.Empty;
        openFileDialog.Title = "Open Chow File";

        saveFileDialog.Filter = FileFilter;
        saveFileDialog.DefaultExt = FileExtension.TrimStart('.');
        saveFileDialog.AddExtension = true;
        saveFileDialog.OverwritePrompt = true;
        saveFileDialog.FileName = string.Empty;
        saveFileDialog.Title = "Save Chow File";
    }

    private void newToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (!ConfirmSaveIfNeeded())
        {
            return;
        }

        LoadDocumentText(string.Empty, null);
        outputTextArea.Clear();
    }

    private void openToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        if (!ConfirmSaveIfNeeded())
        {
            return;
        }

        if (openFileDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var path = openFileDialog.FileName;
        if (!IsChowFile(path))
        {
            ShowError($"Only {FileExtension} files can be opened.");
            return;
        }

        try
        {
            var text = File.ReadAllText(path, Encoding.UTF8);
            LoadDocumentText(text, path);
            outputTextArea.Clear();
        }
        catch (Exception ex)
        {
            ShowError(ex.ToString());
        }
    }

    private void saveToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        SaveDocument();
    }

    private void saveAsToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        SaveDocumentAs();
    }

    private async void executeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (_state != EditorState.Idle)
        {
            return;
        }

        _isStoppingExecution = false;
        SetState(EditorState.Running);
        AppendOutputLine("Starting module...");

        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{FileExtension}");
        _runTempFilePath = tempFilePath;

        try
        {
            await File.WriteAllTextAsync(tempFilePath, inputTextArea.Text, Encoding.UTF8);

            using var process = CreateRunProcess(tempFilePath);
            _runProcess = process;

            process.OutputDataReceived += (_, args) => AppendProcessOutput(args.Data);
            process.ErrorDataReceived += (_, args) => AppendProcessOutput(args.Data);

            AppendOutputLine("Running module...");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
            process.WaitForExit();

            if (_isStoppingExecution)
            {
                AppendOutputLine("Execution stopped.");
            }
            else if (process.ExitCode == 0)
            {
                AppendOutputLine("Execution complete.");
            }
            else
            {
                AppendOutputLine($"Execution failed with exit code {process.ExitCode}.");
            }
        }
        catch (Exception ex)
        {
            AppendOutputLine(ex.ToString());
        }
        finally
        {
            _runProcess = null;
            DeleteTempRunFile();
            _isStoppingExecution = false;
            SetState(EditorState.Idle);
        }
    }

    private void stopToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        StopExecution();
    }

    private void toolStripMenuItem1_Click(object sender, EventArgs e)
    {
    }

    private void inputTextArea_TextChanged(object? sender, EventArgs e)
    {
        if (_isLoadingDocument)
        {
            return;
        }

        _isDirty = true;
        UpdateTitle();
    }

    private void copyOutputToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        if (outputTextArea.TextLength > 0)
        {
            Clipboard.SetText(outputTextArea.Text);
        }
    }

    private void copyInputToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        if (inputTextArea.TextLength > 0)
        {
            Clipboard.SetText(inputTextArea.Text);
        }
    }

    private void clearOutputToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        outputTextArea.Clear();
    }

    private void clearInputToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        inputTextArea.Clear();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_state != EditorState.Idle)
        {
            var closeResult = MessageBox.Show(
                this,
                "Code is still running. Stop execution and close?",
                "Stop Execution",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (closeResult != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            StopExecution();
        }

        if (!ConfirmSaveIfNeeded())
        {
            e.Cancel = true;
        }
    }

    bool ConfirmSaveIfNeeded()
    {
        if (!HasUnsavedChanges())
        {
            return true;
        }

        var result = MessageBox.Show(
            this,
            "Save changes before continuing?",
            "Unsaved Changes",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        switch (result)
        {
            case DialogResult.Yes:
                return SaveDocument();
            case DialogResult.No:
                return true;
            default:
                return false;
        }
    }

    bool SaveDocument()
    {
        return _currentFilePath == null
            ? SaveDocumentAs()
            : SaveDocumentTo(_currentFilePath);
    }

    bool SaveDocumentAs()
    {
        while (true)
        {
            if (_currentFilePath != null)
            {
                saveFileDialog.FileName = _currentFilePath;
            }

            if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            var path = saveFileDialog.FileName;
            if (IsChowFile(path))
            {
                return SaveDocumentTo(path);
            }

            ShowError($"Only {FileExtension} files can be saved.");
        }
    }

    bool SaveDocumentTo(string path)
    {
        try
        {
            File.WriteAllText(path, inputTextArea.Text, Encoding.UTF8);
            _currentFilePath = path;
            _isDirty = false;
            UpdateTitle();
            return true;
        }
        catch (Exception ex)
        {
            ShowError(ex.ToString());
            return false;
        }
    }

    void LoadDocumentText(string text, string? filePath)
    {
        _isLoadingDocument = true;
        try
        {
            inputTextArea.Text = text;
        }
        finally
        {
            _isLoadingDocument = false;
        }

        _currentFilePath = filePath;
        _isDirty = false;
        UpdateTitle();
    }

    void StopExecution()
    {
        if (_runProcess == null || _runProcess.HasExited)
        {
            return;
        }

        _isStoppingExecution = true;
        SetState(EditorState.Stopping);

        try
        {
            _runProcess.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception ex)
        {
            AppendOutputLine(ex.ToString());
        }
    }

    Process CreateRunProcess(string tempFilePath)
    {
        var process = new Process();
        process.StartInfo.FileName = Application.ExecutablePath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
        process.StartInfo.ArgumentList.Add(Program.RunWorkerArgument);
        process.StartInfo.ArgumentList.Add(tempFilePath);
        return process;
    }

    void DeleteTempRunFile()
    {
        if (_runTempFilePath == null)
        {
            return;
        }

        try
        {
            File.Delete(_runTempFilePath);
        }
        catch
        {
            // Temporary execution files are best-effort cleanup only.
        }
        finally
        {
            _runTempFilePath = null;
        }
    }

    void SetState(EditorState state)
    {
        _state = state;
        UpdateGuiState();
    }

    void UpdateGuiState()
    {
        var isIdle = _state == EditorState.Idle;
        var isRunning = _state == EditorState.Running;

        inputTextArea.Enabled = isIdle;
        executeToolStripMenuItem.Enabled = isIdle;
        stopToolStripMenuItem.Enabled = isRunning;
        newToolStripMenuItem.Enabled = isIdle;
        openToolStripMenuItem.Enabled = isIdle;
        saveToolStripMenuItem.Enabled = isIdle;
        saveAsToolStripMenuItem.Enabled = isIdle;
        clearInputToolStripMenuItem.Enabled = isIdle;
    }

    void AppendProcessOutput(string? text)
    {
        if (text == null)
        {
            return;
        }

        AppendOutputLine(text);
    }

    void AppendOutputLine(string text)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            if (!IsHandleCreated)
            {
                return;
            }

            try
            {
                BeginInvoke(new Action(() => AppendOutputLine(text)));
            }
            catch (InvalidOperationException)
            {
            }

            return;
        }

        if (outputTextArea.TextLength > 0)
        {
            outputTextArea.AppendText(Environment.NewLine);
        }

        outputTextArea.AppendText(text);
        outputTextArea.SelectionStart = outputTextArea.TextLength;
        outputTextArea.ScrollToCaret();
    }

    bool HasUnsavedChanges()
    {
        return _isDirty || (_currentFilePath == null && inputTextArea.TextLength > 0);
    }

    static bool IsChowFile(string path)
    {
        return string.Equals(Path.GetExtension(path), FileExtension, StringComparison.OrdinalIgnoreCase);
    }

    void UpdateTitle()
    {
        var name = _currentFilePath == null ? UntitledName : Path.GetFileName(_currentFilePath);
        Text = $"{(_isDirty ? "*" : string.Empty)}{name} - Chow Code Editor";
    }

    void ShowError(string message)
    {
        MessageBox.Show(this, message, "CodeEditor", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
