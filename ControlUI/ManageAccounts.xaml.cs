using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Xml.XPath;
using DropNet;
using DropboxFS;

namespace ControlUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly string _dropboxfsAppConfig = "DropboxFS.exe.config";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ApplicationExit_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void ManageAccounts_OnClick(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateAccountList();
        }

        private void UpdateAccountList()
        {
            var serviceConfig = XDocument.Load(_dropboxfsAppConfig);
            if (serviceConfig.Root == null) return;
            AccountsList.Items.Clear();
            foreach (var a in serviceConfig.Root.XPathSelectElements("//clientAccounts/add").Select(account => new ClientAccount
                {
                    Name = account.Attribute("name").Value,
                    Key = account.Attribute("key").Value,
                    Secret = account.Attribute("secret").Value,
                }))
            {
                AccountsList.Items.Add(a);
            }
        }

        private void AccountsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;
            var account = listBox.SelectedItem as ClientAccount;            
            AccountDetailsStackPanel.Visibility = (account == null) ? Visibility.Hidden : Visibility.Visible;
            if (account == null) return;
            KeyTextBox.Text = account.Key;
            SecretTextBox.Text = account.Secret;
        }

        private void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var serviceConfig = XDocument.Load(_dropboxfsAppConfig);
            if (serviceConfig.Root == null) return;
            var appKey = serviceConfig.Root.XPathSelectElement("//appSettings/add[@key='ApplicationKey']").Attribute("value").Value;
            var appSecret = serviceConfig.Root.XPathSelectElement("//appSettings/add[@key='ApplicationSecret']").Attribute("value").Value;
            if (string.IsNullOrWhiteSpace(appKey) || string.IsNullOrWhiteSpace(appSecret))
                return;
            var dropNetclient = new DropNetClient(appKey, appSecret);
            dropNetclient.GetToken();
            var url = dropNetclient.BuildAuthorizeUrl("http://127.0.0.1:64646/callback/");
            var callbackResult = new DropboxCallbackListener(dropNetclient);
            ThreadPool.QueueUserWorkItem(DropboxCallbackListener.ProcessDropboxCallback, callbackResult);
            Process.Start(url);
            var count = 0; // we'll count how long the process can wait. 4 mins seems more than enough
            do
            {
                count++;
            } while (!WaitHandle.WaitAll(new WaitHandle[]{callbackResult.Done}, 5000) || count < 48);

            if (callbackResult.UserLogin == null)
                throw new Exception("Timed out waiting for Dropbox oAuth callback. Try again");
            // if everything is alright - update the config with a new authorized account
            dropNetclient.UserLogin = callbackResult.UserLogin;
            var info = dropNetclient.AccountInfo();
            var name = info.display_name ?? info.email;
            var accountsRoot = serviceConfig.Root.XPathSelectElement("//clientAccounts");
            var account = new XElement("add");
            account.SetAttributeValue("name", name);
            account.SetAttributeValue("key", callbackResult.UserLogin.Token);
            account.SetAttributeValue("secret", callbackResult.UserLogin.Secret);
            accountsRoot.Add(account);
            serviceConfig.Save(_dropboxfsAppConfig);
            UpdateAccountList();
        }

        private void RemoveAccountButton_Click(object sender, RoutedEventArgs e)
        {            
            var account = AccountsList.SelectedItem as ClientAccount;
            if (account == null) return;
            var serviceConfig = XDocument.Load(_dropboxfsAppConfig);
            if (serviceConfig.Root == null) return;
            var elementToDelete = serviceConfig.Root.XPathSelectElement(string.Format("//clientAccounts/add[@name='{0}']",account.Name));
            elementToDelete.Remove();
            serviceConfig.Save(_dropboxfsAppConfig);
            UpdateAccountList();
        }

        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
}
