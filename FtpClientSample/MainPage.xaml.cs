using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FtpClientSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DoDownloadOrUpload(true);
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            DoDownloadOrUpload(false);
        }

        private async void DoDownloadOrUpload(bool isDownload)
        {
            Uri uri = new Uri(HostTextBox.Text);

            StatusTextBlock.Text = "Connecting.";

            FtpClient client = new FtpClient();
            await client.ConnectAsync(
                new HostName(uri.Host),
                uri.Port.ToString(),
                UserTextBox.Text,
                PassTextBox.Text);

            if (isDownload)
            {
                StatusTextBlock.Text = "Downloading.";

                byte[] data = await client.DownloadAsync(uri.AbsolutePath);

                ContentTextBox.Text = Encoding.UTF8.GetString(data, 0, data.Length);
            }
            else
            {
                StatusTextBlock.Text = "Uploading.";

                byte[] data = Encoding.UTF8.GetBytes(ContentTextBox.Text);

                await client.UploadAsync(uri.AbsolutePath, data);
            }

            StatusTextBlock.Text = "Done.";
        }
    }
}
