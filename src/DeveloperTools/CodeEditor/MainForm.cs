using System.Diagnostics;
using System.Text;

namespace CodeEditor;

public partial class MainForm : Form
{
    const string FileExtension = ".chw";
    const string FileFilter = "Chow files (*.chw)|*.chw";
    const string UntitledName = "Untitled";
    const string DefaultZoomText = "Zoom 100%";

    static readonly string[] ZoomOptions =
    {
        "Zoom 25%",
        "Zoom 50%",
        "Zoom 75%",
        "Zoom 90%",
        "Zoom 100%",
        "Zoom 110%",
        "Zoom 125%",
        "Zoom 150%",
        "Zoom 175%"
    };

    enum EditorState
    {
        Idle,
        Running,
        Stopping
    }

    enum EditorLogLevel
    {
        None,
        Information,
        Debug
    }

    Process? _runProcess;
    string? _runTempFilePath;
    long? _lastExecutionDurationMs;
    ToolStripComboBox? _zoomComboBox;
    string? _currentFilePath;
    bool _isDirty;
    bool _isLoadingDocument;
    bool _isStoppingExecution;
    EditorState _state = EditorState.Idle;

    public MainForm()
    {
        InitializeComponent();
        outputTextArea.Clear();
        _zoomComboBox = FindZoomComboBox();
        WireEvents();
        ConfigureDialogs();
        ConfigureZoomComboBox();
        ConfigureKeyboardShortcuts();
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
        exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
        stopToolStripMenuItem.Click += stopToolStripMenuItem_Click;
        copyOutputToolStripMenuItem.Click += copyOutputToolStripMenuItem_Click;
        copyInputToolStripMenuItem.Click += copyInputToolStripMenuItem_Click;
        clearOutputToolStripMenuItem.Click += clearOutputToolStripMenuItem_Click;
        clearInputToolStripMenuItem.Click += clearInputToolStripMenuItem_Click;
        logLevelComboBox.SelectedIndexChanged += logLevelComboBox_SelectedIndexChanged;
        if (_zoomComboBox != null)
        {
            _zoomComboBox.SelectedIndexChanged += zoomComboBox_SelectedIndexChanged;
        }

        inputTextArea.TextChanged += inputTextArea_TextChanged;
        FormClosing += MainForm_FormClosing;
    }

    void ConfigureZoomComboBox()
    {
        if (_zoomComboBox == null)
        {
            ApplyZoom(DefaultZoomText);
            return;
        }

        _zoomComboBox.Items.Clear();
        _zoomComboBox.Items.AddRange(ZoomOptions);
        _zoomComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _zoomComboBox.MaxDropDownItems = ZoomOptions.Length;
        _zoomComboBox.SelectedItem = DefaultZoomText;
        ApplyZoom(DefaultZoomText);
    }

    void ConfigureKeyboardShortcuts()
    {
        newToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;
        openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
        saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
        executeToolStripMenuItem.ShortcutKeys = Keys.F5;
        clearOutputToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.L;
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

        logLevelComboBox.Items.Clear();
        logLevelComboBox.Items.Add("Logs Disabled");
        logLevelComboBox.Items.Add("Information");
        logLevelComboBox.Items.Add("Debug");
        logLevelComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        logLevelComboBox.SelectedIndex = 0;
    }

    private void newToolStripMenuItem_Click(object sender, EventArgs e)
    {
        CreateNewDocument();
    }

    void CreateNewDocument()
    {
        if (!ConfirmSaveIfNeeded())
        {
            return;
        }

        LoadDocumentText(string.Empty, null);
        outputTextArea.Clear();
        UpdateGuiState();
    }

    private void openToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        OpenDocument();
    }

    void OpenDocument()
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
            UpdateGuiState();
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
        _lastExecutionDurationMs = null;
        SetState(EditorState.Running);
        AppendEditorLog(EditorLogLevel.Information, "Starting module...");

        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{FileExtension}");
        _runTempFilePath = tempFilePath;

        try
        {
            await File.WriteAllTextAsync(tempFilePath, inputTextArea.Text, Encoding.UTF8);

            using var process = CreateRunProcess(tempFilePath);
            _runProcess = process;

            process.OutputDataReceived += (_, args) => AppendProcessOutput(args.Data);
            process.ErrorDataReceived += (_, args) => AppendProcessOutput(args.Data);

            AppendEditorLog(EditorLogLevel.Information, "Running module...\n");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
            process.WaitForExit();

            if (_isStoppingExecution)
            {
                AppendEditorLog(EditorLogLevel.Information, "\nExecution stopped.");
            }
            else if (process.ExitCode == 0)
            {
                AppendEditorLog(EditorLogLevel.Information, "\nExecution complete.");
                AppendDebugExecutionDuration();
            }
            else
            {
                AppendEditorLog(EditorLogLevel.Information, $"\nExecution failed with exit code {process.ExitCode}.");
                AppendDebugExecutionDuration();
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

    private void exitToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void stopToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        StopExecution();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.S:
                if (_state == EditorState.Idle && (_currentFilePath != null || inputTextArea.TextLength > 0))
                {
                    SaveDocument();
                }
                return true;

            case Keys.Control | Keys.N:
                if (newToolStripMenuItem.Enabled)
                {
                    CreateNewDocument();
                }
                return true;

            case Keys.Control | Keys.O:
                if (_state == EditorState.Idle)
                {
                    OpenDocument();
                }
                return true;

            case Keys.Control | Keys.Enter:
            case Keys.F5:
                ExecuteOrStop();
                return true;

            case Keys.Control | Keys.L:
                ClearOutput();
                return true;

            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    void ExecuteOrStop()
    {
        switch (_state)
        {
            case EditorState.Idle:
                executeToolStripMenuItem_Click(this, EventArgs.Empty);
                break;
            case EditorState.Running:
                StopExecution();
                break;
        }
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
        UpdateGuiState();
    }

    private void logLevelComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateGuiState();
    }

    private void zoomComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_zoomComboBox?.SelectedItem is string selectedZoom)
        {
            ApplyZoom(selectedZoom);
        }
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
        ClearOutput();
    }

    void ClearOutput()
    {
        outputTextArea.Clear();
        UpdateGuiState();
    }

    private void clearInputToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        inputTextArea.Clear();
        UpdateGuiState();
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
            UpdateGuiState();
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
        UpdateGuiState();
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
        if (CurrentLogLevel == EditorLogLevel.Debug)
        {
            process.StartInfo.ArgumentList.Add(Program.DebugDurationArgument);
        }

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
        var hasInput = inputTextArea.TextLength > 0;
        var hasFile = _currentFilePath != null;

        inputTextArea.Enabled = isIdle;
        executeToolStripMenuItem.Enabled = isIdle;
        stopToolStripMenuItem.Enabled = isRunning;
        newToolStripMenuItem.Enabled = isIdle && (hasFile || hasInput);
        openToolStripMenuItem.Enabled = isIdle;
        saveToolStripMenuItem.Enabled = isIdle && hasFile;
        saveAsToolStripMenuItem.Enabled = isIdle && (hasFile || hasInput);
        exitToolStripMenuItem.Enabled = true;
        clearInputToolStripMenuItem.Enabled = isIdle && hasInput;
        copyInputToolStripMenuItem.Enabled = hasInput;
        clearOutputToolStripMenuItem.Enabled = outputTextArea.TextLength > 0;
        copyOutputToolStripMenuItem.Enabled = outputTextArea.TextLength > 0;
        logLevelComboBox.Enabled = isIdle;
        if (_zoomComboBox != null)
        {
            _zoomComboBox.Enabled = true;
        }
    }

    void AppendProcessOutput(string? text)
    {
        if (text == null)
        {
            return;
        }

        if (TryCaptureExecutionDuration(text))
        {
            return;
        }

        AppendOutputLine(text);
    }

    void AppendEditorLog(EditorLogLevel minimumLevel, string text)
    {
        if (CurrentLogLevel >= minimumLevel)
        {
            AppendOutputLine(text, FontStyle.Italic);
        }
    }

    void AppendDebugExecutionDuration()
    {
        if (CurrentLogLevel != EditorLogLevel.Debug || !_lastExecutionDurationMs.HasValue)
        {
            return;
        }

        AppendOutputLine($"Execution time: {_lastExecutionDurationMs.Value} ms");
    }

    bool TryCaptureExecutionDuration(string text)
    {
        if (!text.StartsWith(Program.ExecutionDurationPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rawDuration = text.Substring(Program.ExecutionDurationPrefix.Length);
        if (long.TryParse(rawDuration, out var durationMs))
        {
            _lastExecutionDurationMs = durationMs;
        }

        return true;
    }

    void AppendOutputLine(string text)
    {
        AppendOutputLine(text, FontStyle.Regular);
    }

    void AppendOutputLine(string text, FontStyle fontStyle)
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
                BeginInvoke(new Action(() => AppendOutputLine(text, fontStyle)));
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

        using var outputFont = new Font(outputTextArea.Font, fontStyle);
        outputTextArea.SelectionLength = 0;
        outputTextArea.SelectionFont = outputFont;
        outputTextArea.AppendText(text);
        outputTextArea.SelectionFont = outputTextArea.Font;
        outputTextArea.SelectionStart = outputTextArea.TextLength;
        outputTextArea.ScrollToCaret();
        UpdateGuiState();
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

    void ApplyZoom(string zoomText)
    {
        var zoomFactor = ParseZoomFactor(zoomText);
        inputTextArea.ZoomFactor = zoomFactor;
        outputTextArea.ZoomFactor = zoomFactor;
    }

    static float ParseZoomFactor(string zoomText)
    {
        var percentText = zoomText
            .Replace("Zoom", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("%", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (!float.TryParse(percentText, out var percent))
        {
            return 1.0F;
        }

        return percent / 100.0F;
    }

    ToolStripComboBox? FindZoomComboBox()
    {
        var comboBoxes = menuStrip.Items.OfType<ToolStripComboBox>().ToArray();
        var namedZoomComboBox = comboBoxes.FirstOrDefault(comboBox =>
            comboBox.Name?.Contains("zoom", StringComparison.OrdinalIgnoreCase) == true);

        if (namedZoomComboBox != null)
        {
            return namedZoomComboBox;
        }

        var nonLogComboBoxes = comboBoxes
            .Where(comboBox => !ReferenceEquals(comboBox, logLevelComboBox))
            .ToArray();

        return nonLogComboBoxes.Length == 1 ? nonLogComboBoxes[0] : null;
    }

    private void zoomComboBox_Click(object sender, EventArgs e)
    {

    }

    EditorLogLevel CurrentLogLevel
    {
        get
        {
            switch (logLevelComboBox.SelectedItem as string)
            {
                case "Information":
                    return EditorLogLevel.Information;
                case "Debug":
                    return EditorLogLevel.Debug;
                default:
                    return EditorLogLevel.None;
            }
        }
    }
}
