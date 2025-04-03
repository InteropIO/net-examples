using System;
using System.Drawing;
using System.Text.Json;
using System.Windows;
using Tick42.Channels;

namespace net_channels_wpf
{
    public partial class MainWindow
    {
        private void UpdateControlsInitial()
        {
            SetStatusText("Not connected");
            SetWindowChannelText("Window not registered");
            EnableRegisterWindowButton(false);
            ComboICSelector.IsEnabled = false;
            ComboWCSelector.IsEnabled = false;
            BtnUpdateSimple.IsEnabled = false;
            BtnGetContact.IsEnabled = false;
            BtnUpdateContact.IsEnabled = false;
        }

        private void UpdateControlsGlueConnected()
        {
            SetStatusText("Connected");
            EnableRegisterWindowButton(true);
            UpdateAvailableChannels();
            Dispatcher.BeginInvoke((Action)(() =>
            {
                ComboICSelector.IsEnabled = true;
                ComboWCSelector.IsEnabled = true;
            }));
        }

        private void UpdateControlsWindowRegistered()
        {
            EnableRegisterWindowButton(false, "Window Registered");
            var wcContext = glueWindow.ChannelContext;
            SetWindowChannelText(GetChannelInfoText(wcContext, wcContext.GetCurrentChannel(), "Window Registered"));

            Dispatcher.BeginInvoke((Action)(() =>
            {
                BtnUpdateSimple.IsEnabled = true;
                BtnGetContact.IsEnabled = true;
                BtnUpdateContact.IsEnabled = true;
            }));
        }

        private void EnableRegisterWindowButton(bool bEnable, string text = null)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                BtnRegisterWindow.IsEnabled = bEnable;
                if(text is string)
                {
                    BtnRegisterWindow.Content = text;
                }
            }));
        }

        private void UpdateAvailableChannels()
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                ComboWCSelector.Items.Clear();
                ComboWCSelector.Items.Add("None");
                ComboICSelector.Items.Clear();
                ComboICSelector.Items.Add("None");
                foreach (var channel in availableChannels)
                {
                    ComboWCSelector.Items.Add(channel.Name);
                    ComboICSelector.Items.Add(channel.Name);
                }
                UpdateICChannelSelection();
                UpdateWCChannelSelection();
            }));
        }

        private void UpdateWCChannelSelection(string channelName = null)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                channelName = glueWindow?.ChannelContext?.GetCurrentChannel()?.Name ?? "None";
            }
            Dispatcher.BeginInvoke((Action)(() =>
            {
                ComboWCSelector.SelectedItem = channelName;
            }));
        }

        private void UpdateICChannelSelection(string channelName = null)
        {
            if(string.IsNullOrEmpty(channelName))
            {
                channelName = icContext?.GetCurrentChannel()?.Name ?? "None";
            }
            Dispatcher.BeginInvoke((Action)(() =>
            {
                ComboICSelector.SelectedItem = channelName;
            }));
        }

        private void SetStatusText(string text)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                TxtGlueStatus.Text = text;
            }));
        }

        private void SetWindowChannelText(string text)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                TxtWindowChannel.Text = text;
            }));
        }

        private void SetIndependentChannelText(string text)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                TxtIndependentChannel.Text = text;
            }));
        }

        private string GetChannelInfoText(IGlueChannelContext channelContext, IGlueChannel channel, string reason)
        {
            string text = $"{DateTime.Now}\n";
            text += (reason ?? "") + "\n";
            if(channel is object && channelContext is object)
            {
                try
                {
                    var colorText = channelContext.GetValue<string>("color", ChannelLevel.Meta);
                    var color = ColorTranslator.FromHtml(colorText);
                    text += $"displayColor: #{color.ToArgb():X8}\n";
                    text += "channelData:\n";
                    var dataValue = channelContext.GetValue(null, ChannelLevel.Data);
                    var jsOptions = new JsonSerializerOptions { WriteIndented = true };
                    var jsonString = JsonSerializer.Serialize(dataValue, jsOptions);
                    text += jsonString;
                }
                catch (Exception e)
                {
                    text += $"Cannot read from channel: {e.Message}\n";
                }
            }
            else
            {
                text += "No Channel Selected";
            }
            return text;
        }

        private IGlueChannelContext checkGetWindowChannelContext()
        {
            if (!(glueWindow is object))
            {
                MessageBox.Show("Glue window not registered");
                return null;
            }
            if(!(glueWindow.ChannelContext.GetCurrentChannel() is object))
            {
                MessageBox.Show("No channel selected but returning channel context.");
            }
            return glueWindow.ChannelContext;
        }

        #region Control Events
        private void BtnRegisterWindow_Click(object sender, RoutedEventArgs e)
        {
            _ = RegisterGlueWindow();
        }

        private void BtnUpdateSimple_Click(object sender, RoutedEventArgs e)
        {
            var context = checkGetWindowChannelContext();
            if (context is null) return;
            context.SetValue(new
            {
                g = Guid.NewGuid().ToString("N"),
                lastUpdated = DateTime.Now
            }, "aSimple");
        }

        private void BtnGetContact_Click(object sender, RoutedEventArgs e)
        {
            var context = checkGetWindowChannelContext();
            if (context is null) return;
            try
            {
                var contact = context.GetValue<CustomFdc3Contact>("contact");
                if(contact is null)
                {
                    MessageBox.Show("No contact present in channel context", "Info");
                    return;
                }
                if (!contact.type.Equals("fdc3.contact"))
                {
                    throw new Exception("Incompatible format");
                }
                var jsOptions = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(contact, jsOptions);
                MessageBox.Show(jsonString, "Contact Data");
            }
            catch (Exception)
            {
                MessageBox.Show("Contact data format is incompatible", "Error");
            }
        }

        private void BtnUpdateContact_Click(object sender, RoutedEventArgs e)
        {
            var context = checkGetWindowChannelContext();
            if (context is null) return;

            var contact = new CustomFdc3Contact()
            {
                name = "John Doe",
                lastUpdated = DateTime.Now,
                id = new CustomFdc3ContactId()
                {
                    displayName = "Mr. John Doe"
                }
            };
            context.SetValue(contact, "contact");
        }

        private void ComboWCSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedText = ComboWCSelector.SelectedItem as string;
            var newChannel = availableChannels.Find(c => c.Name.Equals(selectedText));
            if(glueWindow is object)
            {
                glueWindow.Channel = newChannel?.Name;
            }
        }

        private void ComboICSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedText = ComboICSelector.SelectedItem as string;
            var newChannel = availableChannels.Find(c => c.Name.Equals(selectedText));

            if (icContext is object)
            {
                if (newChannel is object)
                {
                    icContext.SwitchChannel(newChannel);
                }
                else
                {
                    icContext.SwitchChannel((string)null);
                }
            }
        }
        #endregion

    }
}
