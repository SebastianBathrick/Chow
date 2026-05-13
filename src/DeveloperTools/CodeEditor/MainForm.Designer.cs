namespace CodeEditor;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        menuStrip = new MenuStrip();
        fileToolStripMenuItem = new ToolStripMenuItem();
        newToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator1 = new ToolStripSeparator();
        openToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator2 = new ToolStripSeparator();
        saveToolStripMenuItem = new ToolStripMenuItem();
        saveAsToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator3 = new ToolStripSeparator();
        exitToolStripMenuItem = new ToolStripMenuItem();
        editToolStripMenuItem = new ToolStripMenuItem();
        copyOutputToolStripMenuItem = new ToolStripMenuItem();
        copyInputToolStripMenuItem = new ToolStripMenuItem();
        editToolStripSeparator = new ToolStripSeparator();
        clearOutputToolStripMenuItem = new ToolStripMenuItem();
        clearInputToolStripMenuItem = new ToolStripMenuItem();
        codeToolStripMenuItem = new ToolStripMenuItem();
        executeToolStripMenuItem = new ToolStripMenuItem();
        stopToolStripMenuItem = new ToolStripMenuItem();
        IoSplitContainer = new SplitContainer();
        inputTextArea = new RichTextBox();
        outputTextArea = new RichTextBox();
        openFileDialog = new OpenFileDialog();
        saveFileDialog = new SaveFileDialog();
        menuStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)IoSplitContainer).BeginInit();
        IoSplitContainer.Panel1.SuspendLayout();
        IoSplitContainer.Panel2.SuspendLayout();
        IoSplitContainer.SuspendLayout();
        SuspendLayout();
        // 
        // menuStrip
        // 
        menuStrip.ImageScalingSize = new Size(20, 20);
        menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, codeToolStripMenuItem });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(782, 28);
        menuStrip.TabIndex = 1;
        menuStrip.Text = "menuStrip1";
        // 
        // fileToolStripMenuItem
        // 
        fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newToolStripMenuItem, toolStripSeparator1, openToolStripMenuItem, toolStripSeparator2, saveToolStripMenuItem, saveAsToolStripMenuItem, toolStripSeparator3, exitToolStripMenuItem });
        fileToolStripMenuItem.Name = "fileToolStripMenuItem";
        fileToolStripMenuItem.Size = new Size(46, 24);
        fileToolStripMenuItem.Text = "File";
        // 
        // newToolStripMenuItem
        // 
        newToolStripMenuItem.Name = "newToolStripMenuItem";
        newToolStripMenuItem.Size = new Size(224, 26);
        newToolStripMenuItem.Text = "New";
        newToolStripMenuItem.Click += newToolStripMenuItem_Click;
        // 
        // toolStripSeparator1
        // 
        toolStripSeparator1.Name = "toolStripSeparator1";
        toolStripSeparator1.Size = new Size(221, 6);
        // 
        // openToolStripMenuItem
        // 
        openToolStripMenuItem.Name = "openToolStripMenuItem";
        openToolStripMenuItem.Size = new Size(224, 26);
        openToolStripMenuItem.Text = "Open";
        // 
        // toolStripSeparator2
        // 
        toolStripSeparator2.Name = "toolStripSeparator2";
        toolStripSeparator2.Size = new Size(221, 6);
        // 
        // saveToolStripMenuItem
        // 
        saveToolStripMenuItem.Name = "saveToolStripMenuItem";
        saveToolStripMenuItem.Size = new Size(224, 26);
        saveToolStripMenuItem.Text = "Save";
        // 
        // saveAsToolStripMenuItem
        // 
        saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
        saveAsToolStripMenuItem.Size = new Size(224, 26);
        saveAsToolStripMenuItem.Text = "Save As";
        // 
        // toolStripSeparator3
        // 
        toolStripSeparator3.Name = "toolStripSeparator3";
        toolStripSeparator3.Size = new Size(221, 6);
        // 
        // exitToolStripMenuItem
        // 
        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.Size = new Size(224, 26);
        exitToolStripMenuItem.Text = "Exit";
        // 
        // editToolStripMenuItem
        // 
        editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { copyOutputToolStripMenuItem, copyInputToolStripMenuItem, editToolStripSeparator, clearOutputToolStripMenuItem, clearInputToolStripMenuItem });
        editToolStripMenuItem.Name = "editToolStripMenuItem";
        editToolStripMenuItem.Size = new Size(49, 24);
        editToolStripMenuItem.Text = "Edit";
        // 
        // copyOutputToolStripMenuItem
        // 
        copyOutputToolStripMenuItem.Name = "copyOutputToolStripMenuItem";
        copyOutputToolStripMenuItem.Size = new Size(176, 26);
        copyOutputToolStripMenuItem.Text = "Copy Output";
        // 
        // copyInputToolStripMenuItem
        // 
        copyInputToolStripMenuItem.Name = "copyInputToolStripMenuItem";
        copyInputToolStripMenuItem.Size = new Size(176, 26);
        copyInputToolStripMenuItem.Text = "Copy Input";
        // 
        // editToolStripSeparator
        // 
        editToolStripSeparator.Name = "editToolStripSeparator";
        editToolStripSeparator.Size = new Size(173, 6);
        // 
        // clearOutputToolStripMenuItem
        // 
        clearOutputToolStripMenuItem.Name = "clearOutputToolStripMenuItem";
        clearOutputToolStripMenuItem.Size = new Size(176, 26);
        clearOutputToolStripMenuItem.Text = "Clear Output";
        // 
        // clearInputToolStripMenuItem
        // 
        clearInputToolStripMenuItem.Name = "clearInputToolStripMenuItem";
        clearInputToolStripMenuItem.Size = new Size(176, 26);
        clearInputToolStripMenuItem.Text = "Clear Input";
        // 
        // codeToolStripMenuItem
        // 
        codeToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { executeToolStripMenuItem, stopToolStripMenuItem });
        codeToolStripMenuItem.Name = "codeToolStripMenuItem";
        codeToolStripMenuItem.Size = new Size(80, 24);
        codeToolStripMenuItem.Text = "Interpret";
        codeToolStripMenuItem.Click += toolStripMenuItem1_Click;
        // 
        // executeToolStripMenuItem
        // 
        executeToolStripMenuItem.Name = "executeToolStripMenuItem";
        executeToolStripMenuItem.Size = new Size(160, 26);
        executeToolStripMenuItem.Text = "▶ Execute";
        executeToolStripMenuItem.Click += executeToolStripMenuItem_Click;
        // 
        // stopToolStripMenuItem
        // 
        stopToolStripMenuItem.Name = "stopToolStripMenuItem";
        stopToolStripMenuItem.Size = new Size(160, 26);
        stopToolStripMenuItem.Text = "■ Stop";
        // 
        // IoSplitContainer
        // 
        IoSplitContainer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        IoSplitContainer.Location = new Point(12, 28);
        IoSplitContainer.Name = "IoSplitContainer";
        // 
        // IoSplitContainer.Panel1
        // 
        IoSplitContainer.Panel1.Controls.Add(inputTextArea);
        // 
        // IoSplitContainer.Panel2
        // 
        IoSplitContainer.Panel2.Controls.Add(outputTextArea);
        IoSplitContainer.Size = new Size(758, 413);
        IoSplitContainer.SplitterDistance = 379;
        IoSplitContainer.TabIndex = 2;
        // 
        // inputTextArea
        // 
        inputTextArea.Dock = DockStyle.Fill;
        inputTextArea.Location = new Point(0, 0);
        inputTextArea.Name = "inputTextArea";
        inputTextArea.Size = new Size(379, 413);
        inputTextArea.TabIndex = 0;
        inputTextArea.Text = "";
        // 
        // outputTextArea
        // 
        outputTextArea.Dock = DockStyle.Fill;
        outputTextArea.Location = new Point(0, 0);
        outputTextArea.Name = "outputTextArea";
        outputTextArea.ReadOnly = true;
        outputTextArea.Size = new Size(375, 413);
        outputTextArea.TabIndex = 0;
        outputTextArea.Text = "";
        // 
        // openFileDialog
        // 
        openFileDialog.FileName = "openFileDialog1";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(782, 453);
        Controls.Add(IoSplitContainer);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Name = "MainForm";
        Text = "t5";
        Load += MainForm_Load;
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        IoSplitContainer.Panel1.ResumeLayout(false);
        IoSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)IoSplitContainer).EndInit();
        IoSplitContainer.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
    private MenuStrip menuStrip;
    private ToolStripMenuItem fileToolStripMenuItem;
    private ToolStripMenuItem newToolStripMenuItem;
    private ToolStripMenuItem openToolStripMenuItem;
    private ToolStripMenuItem saveToolStripMenuItem;
    private ToolStripMenuItem saveAsToolStripMenuItem;
    private ToolStripMenuItem codeToolStripMenuItem;
    private ToolStripMenuItem executeToolStripMenuItem;
    private ToolStripMenuItem stopToolStripMenuItem;
    private SplitContainer IoSplitContainer;
    private RichTextBox inputTextArea;
    private RichTextBox outputTextArea;
    private ToolStripMenuItem editToolStripMenuItem;
    private ToolStripMenuItem copyOutputToolStripMenuItem;
    private ToolStripMenuItem copyInputToolStripMenuItem;
    private ToolStripSeparator editToolStripSeparator;
    private ToolStripMenuItem clearOutputToolStripMenuItem;
    private ToolStripMenuItem clearInputToolStripMenuItem;
    private OpenFileDialog openFileDialog;
    private SaveFileDialog saveFileDialog;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripSeparator toolStripSeparator2;
    private ToolStripSeparator toolStripSeparator3;
    private ToolStripMenuItem exitToolStripMenuItem;
}
