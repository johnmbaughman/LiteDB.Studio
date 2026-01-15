Add/port BsonValueToStringConverter and a BsonDataGridHelper that implements UIExtensions.BindBsonData behavior; wire into MainWindow.xaml DataGrid columns (see MainWindow.xaml).
Implement SqlCompletionService in WPF by porting SqlCodeCompletion.UpdateCodeCompletion logic and reusing SQL-Mode.xshd (add file from WinForms resources if not already present). Hook it into AvalonEditorAdapter (use ShowCompletion).
Copy any missing icons from Resources.resx into Resources and ensure toolbar Image sources match WinForms ordering/size.
Port ConnectionForm layout into a WPF ConnectionDialog.xaml and wire button/OK behavior to MainViewModel.ConnectCommand.

Wire double-click/context menu actions on tree nodes to open collections (e.g. run SELECT).
Use different icon choices or overlay icons for "table_gear" if you want a closer visual match.
Run the manual integration test (open a real .db and confirm tree contents). Which do you want me to do next?