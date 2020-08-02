using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using The_Long_Dark_Save_Editor_2.Helpers;


namespace The_Long_Dark_Save_Editor_2.Tabs
{
    public partial class JSONToolsTab : UserControl
    {
        private static JsonLoadSettings jsonLoadSettings = new JsonLoadSettings() { LineInfoHandling = LineInfoHandling.Ignore };
        public JSONToolsTab()
        {
            InitializeComponent();
            BadData.Visibility = Visibility.Hidden;
        }

        private void JSONLoadClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.Instance.CurrentSave != null)
                    if (DeserializeGlobalCheckBox.IsChecked == true)
                        MainWindow.Instance.CurrentSave.JSONLoadSave(true);
                    else
                        MainWindow.Instance.CurrentSave.JSONLoadSave();
                }
            catch (Exception ex)
            {
                ErrorDialog.Show("JSONLoad failed to load a save", ex != null ? (ex.Message + "\n" + ex.ToString()) : null);
            }
        }

        private void JSONSaveClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.Instance.CurrentSave != null)
                    MainWindow.Instance.CurrentSave.JSONSave();
                }
            catch (Exception ex)
            {
                ErrorDialog.Show("JSONSave failed to save a save", ex != null ? (ex.Message + "\n" + ex.ToString()) : null);
            }
        }

        private void JSONLoadPClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.Instance.CurrentProfile != null)
                    MainWindow.Instance.CurrentProfile.JSONLoadProfile();
                }
            catch (Exception ex)
            {
                ErrorDialog.Show("JSONLoadP failed to load the profile", ex != null ? (ex.Message + "\n" + ex.ToString()) : null);
            }
        }

        private void JSONSavePClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainWindow.Instance.CurrentProfile != null)
                    MainWindow.Instance.CurrentProfile.JSONSave();
                }
            catch (Exception ex)
            {
                ErrorDialog.Show("JSONSaveP failed to save the profile", ex != null ? (ex.Message + "\n" + ex.ToString()) : null);
            }
        }

        private static JsonSerializerSettings JsonSettSerializer = new JsonSerializerSettings() { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Include, TypeNameHandling = TypeNameHandling.None };

        private void SerializedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                BadData.Visibility = Visibility.Hidden;
                Deserialized.Text = JsonConvert.DeserializeObject(Serialized.Text, JsonSettSerializer).ToString();
            }
            catch (Exception)
            {
                BadData.Visibility = Visibility.Visible;
            }
        }

        private void DeserializedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                BadData.Visibility = Visibility.Hidden;
                Serialized.Text = JsonConvert.SerializeObject(Deserialized.Text, Formatting.None);
            }
            catch (Exception)
            {
                BadData.Visibility = Visibility.Visible;
            }

        }
    }
}
