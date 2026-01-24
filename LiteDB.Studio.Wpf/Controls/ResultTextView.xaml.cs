using LiteDB.Studio.Wpf.Services;
using System.Windows;
using System.Windows.Controls;

namespace LiteDB.Studio.Wpf.Controls;

public partial class ResultTextView : UserControl
{
    public static readonly DependencyProperty QueryResultProperty =
        DependencyProperty.Register(nameof(QueryResult), typeof(QueryResult), typeof(ResultTextView),
            new PropertyMetadata(null, OnQueryResultChanged));

    public QueryResult? QueryResult
    {
        get => (QueryResult?)GetValue(QueryResultProperty);
        set => SetValue(QueryResultProperty, value);
    }

    public ResultTextView()
    {
        InitializeComponent();
    }

    private static void OnQueryResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResultTextView control)
        {
            control.UpdateJsonDisplay();
        }
    }

    private void UpdateJsonDisplay()
    {
        if (QueryResult?.Rows == null)
        {
            JsonTextBox.Text = string.Empty;
            return;
        }

        var jsonLines = new List<string>();
        foreach (var row in QueryResult.Rows)
        {
            if (row is BsonDocument doc)
            {
                jsonLines.Add(doc.ToString());
            }
            else
            {
                jsonLines.Add(row.ToString() ?? "null");
            }
        }

        JsonTextBox.Text = string.Join(Environment.NewLine, jsonLines);
    }
}