using BFSSyncNetworkApp.Models;
using BFSSyncNetworkApp.Services;

namespace BFSSyncNetworkApp;

public sealed class MainForm : Form
{
    private readonly TextBox _jsonInput = new();
    private readonly NumericUpDown _startNodeInput = new();
    private readonly Button _runThreadTaskButton = new();
    private readonly Button _runParallelForButton = new();
    private readonly Button _runChannelButton = new();
    private readonly Button _runCompareButton = new();
    private readonly RichTextBox _outputBox = new();

    public MainForm()
    {
        Text = "Synchronous BFS Playground (.NET)";
        Width = 1200;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        WireEvents();

        _jsonInput.Text = DefaultGraphJson;
        _startNodeInput.Value = 0;
    }

    private static string DefaultGraphJson =>
"""
{
  "nodes": [0, 1, 2, 3, 4, 5, 6],
  "edges": [
    [0, 1], [0, 2], [1, 3], [2, 3], [2, 4], [4, 5], [5, 6]
  ]
}
""";

    private void BuildLayout()
    {
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));

        var inputGroup = new GroupBox
        {
            Text = "Graph JSON Input",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _jsonInput.Multiline = true;
        _jsonInput.ScrollBars = ScrollBars.Both;
        _jsonInput.AcceptsTab = true;
        _jsonInput.AcceptsReturn = true;
        _jsonInput.WordWrap = false;
        _jsonInput.Dock = DockStyle.Fill;
        _jsonInput.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);

        inputGroup.Controls.Add(_jsonInput);

        var controlsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 8)
        };

        var startLabel = new Label
        {
            Text = "Start node:",
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };

        _startNodeInput.Width = 80;
        _startNodeInput.Minimum = -100000;
        _startNodeInput.Maximum = 100000;

        _runThreadTaskButton.Text = "Run Thread/Task";
        _runParallelForButton.Text = "Run Parallel.For";
        _runChannelButton.Text = "Run Channel";
        _runCompareButton.Text = "Compare All";

        _runThreadTaskButton.AutoSize = true;
        _runParallelForButton.AutoSize = true;
        _runChannelButton.AutoSize = true;
        _runCompareButton.AutoSize = true;

        controlsPanel.Controls.Add(startLabel);
        controlsPanel.Controls.Add(_startNodeInput);
        controlsPanel.Controls.Add(_runThreadTaskButton);
        controlsPanel.Controls.Add(_runParallelForButton);
        controlsPanel.Controls.Add(_runChannelButton);
        controlsPanel.Controls.Add(_runCompareButton);

        var outputGroup = new GroupBox
        {
            Text = "Result",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _outputBox.Dock = DockStyle.Fill;
        _outputBox.ReadOnly = true;
        _outputBox.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);
        _outputBox.WordWrap = false;

        outputGroup.Controls.Add(_outputBox);

        root.Controls.Add(inputGroup, 0, 0);
        root.Controls.Add(controlsPanel, 0, 1);
        root.Controls.Add(outputGroup, 0, 2);

        Controls.Add(root);
    }

    private void WireEvents()
    {
        _runThreadTaskButton.Click += async (_, _) => await RunSingleAsync(BfsExecutionMode.ThreadTask);
        _runParallelForButton.Click += async (_, _) => await RunSingleAsync(BfsExecutionMode.ParallelFor);
        _runChannelButton.Click += async (_, _) => await RunSingleAsync(BfsExecutionMode.Channel);
        _runCompareButton.Click += async (_, _) => await RunComparisonAsync();
    }

    private async Task RunSingleAsync(BfsExecutionMode mode)
    {
        SetButtonsEnabled(false);

        try
        {
            Graph graph = GraphParser.Parse(_jsonInput.Text);
            int startNode = decimal.ToInt32(_startNodeInput.Value);

            BfsResult result = await Task.Run(() => SynchronousBfsRunner.Run(graph, startNode, mode));
            _outputBox.Text = ResultFormatter.FormatSingle(graph, result);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Run Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async Task RunComparisonAsync()
    {
        SetButtonsEnabled(false);

        try
        {
            Graph graph = GraphParser.Parse(_jsonInput.Text);
            int startNode = decimal.ToInt32(_startNodeInput.Value);

            IReadOnlyList<BfsResult> results = await Task.Run(() =>
            {
                return new[]
                {
                    SynchronousBfsRunner.Run(graph, startNode, BfsExecutionMode.ThreadTask),
                    SynchronousBfsRunner.Run(graph, startNode, BfsExecutionMode.ParallelFor),
                    SynchronousBfsRunner.Run(graph, startNode, BfsExecutionMode.Channel)
                };
            });

            _outputBox.Text = ResultFormatter.FormatComparison(graph, results);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Run Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _runThreadTaskButton.Enabled = enabled;
        _runParallelForButton.Enabled = enabled;
        _runChannelButton.Enabled = enabled;
        _runCompareButton.Enabled = enabled;
    }
}
