using LiteDB.Studio.Wpf.ViewModels;
using Microsoft.Win32;
using System;
using System.Windows;

namespace LiteDB.Studio.Wpf.Views
{
    public partial class ConnectionManagerWindow : Window
    {
        public ConnectionManagerWindow()
        {
            InitializeComponent();
            this.Loaded += ConnectionManagerWindow_Loaded;
        }

        private void ConnectionManagerWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            if (this.DataContext is ConnectionManagerViewModel vm)
            {
                vm.RequestClose += Vm_RequestClose;
            }
        }

        private void Vm_RequestClose(object? sender, bool? e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.DialogResult = e;
                this.Close();
            });
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "LiteDB files (*.db)|*.db|All files (*.*)|*.*",
                Title = "Open LiteDB file"
            };

            var res = dlg.ShowDialog(this);
            if (res == true && this.DataContext is ConnectionManagerViewModel vm)
            {
                vm.Filename = dlg.FileName;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ConnectionManagerViewModel vm)
            {
                vm.Password = PasswordBox.Password;
            }
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ConnectionManagerViewModel vm)
            {
                if (vm.ConnectCommand.CanExecute(null)) vm.ConnectCommand.Execute(null);
            }
        }
    }
}
