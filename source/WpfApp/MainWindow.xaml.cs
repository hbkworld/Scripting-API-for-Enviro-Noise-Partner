using ScriptingAPI;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace ExampleProgram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IScripting api = new Scripting();
        private static readonly HttpClient client = new HttpClient();

        private async Task<string> GetRequestAsync(string url)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                return null;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Startup();
            Closing += MainWindow_Closing; 
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (api != null && api.StartedFromHere)
            {
                e.Cancel = true;
                Task.Factory.StartNew(async () =>
                {
                    await api.ShutDownAppAsync();
                    Dispatcher.Invoke(() => Close());
                });
            }
        }

        private async Task Startup()
        {
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            NASPathBox.Text = Path.Combine(Directory.GetParent(path).Parent.Parent.FullName, "NAS disc", "BK2255-000501");
            projectnameBox.Text = Path.Combine(Directory.GetParent(path).Parent.Parent.FullName, "NAS project.enp");
            outputFile.Text = Path.Combine(Directory.GetParent(path).Parent.Parent.FullName, "output.xlsx");
            reportFile.Text = Path.Combine(Directory.GetParent(path).Parent.Parent.FullName, "report.docx");

            datePicker.SelectedDate = datePicker.DisplayDateEnd = DateTime.Now;
            datePicker_Instrument.SelectedDate = datePicker_Instrument.DisplayDateEnd = DateTime.Now;
            await api.LaunchAppAsync(ScriptingApps.EnviromentalNoisePartner);
            await SetFirstProjectAsync();
            SetFirstNASDate();
            CheckApiVersion();
        }

        private void CheckApiVersion()
        {
            if (api.Version == 1)
                MessageBox.Show($"Enviro Noise partner program is not the latest version. To support all commands please update Enviro Noise Partner");
            else if (api.Version > 2)
                MessageBox.Show($"Enviro Noise partner program is newer than this program. Maybe update this source code from Github");
        }

        private async void LaunchAppClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                await api.LaunchAppAsync(ScriptingApps.EnviromentalNoisePartner);
                CheckApiVersion();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform command: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void ShutdownAppClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                await api.ShutDownAppAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform command: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void LoadProjectClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                if (string.IsNullOrEmpty(projectnameBox.Text) || !File.Exists(projectnameBox.Text))
                    MessageBox.Show($"Project file: {projectnameBox.Text} does not exist");

                await api.OpenProjectAsync(projectnameBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open project {projectnameBox.Text}: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void SaveProjectClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                if (string.IsNullOrEmpty(projectnameBox.Text))
                    MessageBox.Show($"You must specify a full project path");

                await api.SaveProjectAsync(projectnameBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save project {projectnameBox.Text}: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void CloseProjectClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                await api.CloseProjectAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform command: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void SelectAllClick(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).IsEnabled = false;
            try
            {
                await api.SelectAllAsync((sender as ToggleButton).IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform command: {ex.Message}", "Error");
            }
            (sender as ToggleButton).IsEnabled = true;
        }

        private async void ExportProjectClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                if (string.IsNullOrEmpty(outputFile.Text))
                    MessageBox.Show($"No output file is specified");

                await api.ExportAsync(outputFile.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform command: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void ReportProjectClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                if (string.IsNullOrEmpty(reportFile.Text))
                    MessageBox.Show($"No report file is specified");

                await api.ReportAsync(reportFile.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform command: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void ImportInstrumentProjectClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                if (string.IsNullOrEmpty(InstrumentProjectImportText.Text))
                    MessageBox.Show($"No project name is specified");

                await api.ImportFromInstrumentAsync(InstrumentIP.Text, InstrumentProjectImportText.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform command: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void ImportInstrumentMeasurementClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                var measurement = comboBoxInstrumentMeasurement.SelectedValue as string;
                if (measurement == null || measurement == "None")
                    MessageBox.Show($"No measurement is specified");
                else
                {
                    var date = datePicker_Instrument.SelectedDate;
                    var dateString = $"{date.Value.Year}_{date.Value.Month.ToString("00")}_{date.Value.Day.ToString("00")}";
                    string url = measurement == "All" ? $"/WebXi/Applications/SLM/Data/{dateString}" : $"/WebXi/Applications/SLM/Data/{dateString}/{measurement}";
                    await api.ImportMeasurementFromInstrumentAsync(InstrumentIP.Text, url);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform command: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void instrumentDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var date = (sender as DatePicker).SelectedDate;
            var dateString = $"{date.Value.Year}_{date.Value.Month.ToString("00")}_{date.Value.Day.ToString("00")}";
            var measurements = new List<string>();

            string url = $"http://{InstrumentIP.Text}/WebXi/Applications/SLM/Data/{dateString}";
            string result = await GetRequestAsync(url);
            if (!string.IsNullOrEmpty(result) && result.Contains(","))
            {
                measurements.Add("All");

                foreach (var measString in result.Substring(1, result.Length - 3).Split(','))
                    measurements.Add(measString.Substring(2, measString.Length - 2 - 5));
            }
            else
            {
                measurements.Add("None");
            }
            comboBoxInstrumentMeasurement.ItemsSource = measurements;
            comboBoxInstrumentMeasurement.SelectedIndex = 0;
        }

        private async void InstrumentIP_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text == "\r")
            {
                instrumentDatePicker_SelectedDateChanged(datePicker_Instrument, null);
                await SetFirstProjectAsync();
            }
        }

        private async Task SetFirstProjectAsync()
        {
            string url = $"http://{InstrumentIP.Text}/WebXi/Applications/SLM/Appdata/Enviro/9999_12_31_23_59_59/ProjectIndexMapping.xml";
            string result = await GetRequestAsync(url);
            if (result != null)
            {
                var firstNameStart = result.IndexOf("<Name>");
                var firstNameEnd = result.IndexOf("</Name>");
                if (firstNameStart != -1 && firstNameEnd != -1)
                    InstrumentProjectImportText.Text = result.Substring(firstNameStart + "<Name>".Length, firstNameEnd - firstNameStart - "<Name>".Length);
                else
                    InstrumentProjectImportText.Text = "";
            }
            else
                InstrumentProjectImportText.Text = "";
        }

        private async void ImportNASProjectClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                if (string.IsNullOrEmpty(NASProjectImportText.Text))
                    MessageBox.Show($"No NAS project is specified");

                await api.ImportFromNasAsync(NASPathBox.Text, NASProjectImportText.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform command: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private async void ImportNASMeasurementClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                var measurement = comboBoxNASMeasurement.SelectedValue as string;
                if (measurement == null || measurement == "None")
                    MessageBox.Show($"No measurement is specified");
                else
                {
                    var date = datePicker.SelectedDate;
                    var dateString = $"{date.Value.Year}_{date.Value.Month.ToString("00")}_{date.Value.Day.ToString("00")}";
                    string path = measurement == "All" ? $"data/{dateString}" : $"data/{dateString}/{measurement}";

                    await api.ImportMeasurementFromNASAsync(NASPathBox.Text, path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not perform command: {ex.Message}", "Error");
            }
            (sender as Button).IsEnabled = true;
        }

        private void datePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var date = (sender as DatePicker).SelectedDate;
            var dateString = $"{date.Value.Year}_{date.Value.Month.ToString("00")}_{date.Value.Day.ToString("00")}";
            var measurements = new List<string>();

            string path = $"{NASPathBox.Text}/data/{dateString}";
            if (Directory.Exists(path))
            {
                measurements.Add("All");
                foreach (var measString in Directory.GetDirectories(path))
                    measurements.Add(measString.Substring(path.Length + 1));
            }
            else
            {
                measurements.Add("None");
            }

            comboBoxNASMeasurement.ItemsSource = measurements;
            comboBoxNASMeasurement.SelectedIndex = 0;
        }

        private void NAS_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text == "\r")
                SetFirstNASDate();
        }

        private void SetFirstNASDate()
        {
            string path = $"{NASPathBox.Text}/data";
            if (Directory.Exists(path))
            {
                var dateFolders = Directory.GetDirectories(path).OrderByDescending(x => x).ToList();
                datePicker.SelectedDate = DateTime.ParseExact(dateFolders[0].Substring(path.Length + 1), "yyyy_MM_dd", null);
            }
            else
            {
                datePicker.SelectedDate = DateTime.Now;
            }
            SetFirstNASProject();
        }

        private void SetFirstNASProject()
        {
            NASProjectImportText.Text = "";

            string path = $"{NASPathBox.Text}/appdata/UnAttached";
            if (Directory.Exists(path))
            {
                var dateFolders = Directory.GetDirectories(path).Where(d => !d.StartsWith(Path.Combine(path, "9999"))).OrderByDescending(x => x).ToList();
                var latestProject = Path.Combine(dateFolders[0], "project.dat");
                if (File.Exists(latestProject))
                {
                    var content = File.ReadAllText(latestProject);
                    var firstNameStart = content.IndexOf("<Value>");
                    var firstNameEnd = content.IndexOf("</Value>");
                    if (firstNameStart != -1 && firstNameEnd != -1)
                        NASProjectImportText.Text = content.Substring(firstNameStart + "<Value>".Length, firstNameEnd - firstNameStart - "<Value>".Length);
                }
            }
        }
    }
}
