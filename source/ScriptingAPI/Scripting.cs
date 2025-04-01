using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace ScriptingAPI
{
    public class Scripting : IScripting
    {
        // List of known commands
        #region Version 1 API
        public const string ShutdownID = "Shutdown";
        public const string OpenProjectID = "OpenProject";
        public const string CloseProjectID = "CloseProject";
        public const string ExportID = "Export";
        public const string ReportID = "Report";
        public const string SwitchTaskID = "SwitchTask";
        public const string SwitchSubTaskID = "SwitchSubTask";
        public const string SelectNodeID = "SelectNode";
        public const string SetLanguageID = "SetLanguage";
        public const string Import2250XMLID = "Import2250XML";
        public const string ToggleRightSideMenuID = "ToggleRightSideMenu";
        public const string SwitchRightSideSubMenuID = "SwitchRightSideSubMenu";
        public const string ToggleLeftSideMenuID = "ToggleLeftSideMenu";
        public const string SwitchLeftSideSubMenuID = "SwitchLeftSideSubMenu";
        public const string SubMenuPropertiesToggleID = "SubMenuPropertiesToggle";
        public const string SubMenuAddLineID = "SubMenuAddLine";
        public const string SelectAllID = "SelectAll";
        public const string ExportImpulsesID = "ImpulsesExport";
        public const string ImportFromNasID = "ImportFromNasNAS";
        public const string ImportFromInstrumentID = "ImportFromInstrumentNAS";
        #endregion

        #region Version 2 API
        public const string SaveProjectID = "SaveProject";
        public const string ImportMeasurementFromInstrumentID = "ImportMeasurementFromInstrument";
        public const string ImportMeasurementFromNASID = "ImportMeasurementFromNAS";
        #endregion

        private const string _pipeName = "ENPAutomation";
        private NamedPipeClientStream _pipeClient;
        private bool _initialized;
        private StreamString _streamString;
        private Process pipeClientProcess;
        private string _installPath;
        private string _processName = "EnviroNoiseOffice";
        
        /// <summary>
        /// Startup app if needed and open the pipe
        /// </summary>
        /// <param name="app"></param>
        /// <returns>False if app was already running; True if it was started up by this method</returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task<bool> EnsureServiceIsStarted(ScriptingApps app)
        {
            if (app != ScriptingApps.EnviromentalNoisePartner)
                throw new NotImplementedException();

            var startup = false;
            var localByName = Process.GetProcessesByName(_processName);
            if (localByName.GetLength(0) == 0)
            {
                var installPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Bruel_and_Kjaer_EnviroNoise_Partner", "EnviroNoiseOffice.exe");

                if (!string.IsNullOrEmpty(_installPath))
                    installPath = _installPath;

                Process.Start(installPath);
                await Task.Delay(1000);
                startup = true;
                StartedFromHere = true;
            }

            localByName = Process.GetProcessesByName(_processName);
            pipeClientProcess = localByName[0];

            int counter = 0;
            while (pipeClientProcess.MainWindowHandle == IntPtr.Zero && counter < 300)
            {
                localByName = Process.GetProcessesByName(_processName);
                pipeClientProcess = localByName[0];
                counter++;
                await Task.Delay(100);
            }

            InitializeService();

            return startup;
        }

        public int Version { get; set; }

        public bool StartedFromHere { get; set; }

        private bool InitializeService()
        {
            if (pipeClientProcess == null || pipeClientProcess.HasExited)
                throw new Exception("No App is connected - you must Launch an app first");

            if (_initialized)
                return true;

            _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);
            try
            {
                _pipeClient.Connect(2000);
            }
            catch (TimeoutException)
            {
                throw new Exception("Application does not respond. Try to update the app to a newer version");
            }

            _streamString = new StreamString(_pipeClient);
            var ENPScriptingVersion = _streamString.ReadString();
            if (ENPScriptingVersion.StartsWith("SXU Office Service v"))
            {
                Version = int.Parse(ENPScriptingVersion.Substring("SXU Office Service v".Length));
                _streamString.WriteString("SXU Office v1");
                if (_streamString.ReadString() == "Connection accepted")
                    _initialized = true;
            }

            return true;
        }

        private async Task CheckInitialized()
        {
            await Task.Delay(1); // Just to avoid warnings for now. Later maybe change the communication to a real async scheme

            InitializeService();
        }

        /// <summary>
        /// Sctripting interface for Building Acoustics partner, Environmental Partner and Noise Partner
        /// </summary>
        public Scripting(string installPath=null)
        {
            _installPath = installPath;
        }

        /// <summary>
        /// Opens the specified app
        /// </summary>
        /// <param name="app"></param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>False if app was already running; True if it was started up by this method</returns>
        public async Task<bool> LaunchAppAsync(ScriptingApps app, CancellationToken cancellation)
        {
            return await EnsureServiceIsStarted(app);
        }

        /// <summary>
        /// Closes the scripting api
        /// </summary>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task CloseAsync(CancellationToken cancellation)
        {
            if (_pipeClient != null)
            {
                _pipeClient.Dispose();
                _pipeClient = null;
            }

            _initialized = false;
        }

        /// <summary>
        /// Shuts down the open app - changes will not be saved
        /// </summary>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task ShutDownAppAsync(CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{ShutdownID}");

            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Shutdown failed: {response}");

            if (_pipeClient != null)
            {
                _pipeClient.Dispose();
                _pipeClient = null;
            }

            while (!pipeClientProcess.HasExited)
                await Task.Delay(100);

            await WaitForProcessToExit();

            _initialized = false;
            StartedFromHere = false;
        }

        public async Task KillProcess()
        {
            if (pipeClientProcess?.Id == null || pipeClientProcess.HasExited)
                return;

            pipeClientProcess.Kill();
            await WaitForProcessToExit();
        }

        public async Task WaitForProcessToExit()
        {
            if (pipeClientProcess?.Id == null)
                return;

            var counter = 0;
            while (Process.GetProcessesByName(_processName).Any(p => p.Id == pipeClientProcess.Id) && counter < 300)
            {
                counter++;
                await Task.Delay(100);
            }
        }

        /// <summary>
        /// Open the specified project
        /// </summary>
        /// <remarks>The first measurement in the project will be selected in the project browser</remarks>
        /// <param name="projectPath">Path to a project file understood by the App (*.enp)</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task OpenProjectAsync(string projectPath, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{OpenProjectID}*{projectPath}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"OpenProject failed: {response}");
        }

        /// <summary>
        /// Saves the current project
        /// </summary>
        /// <param name="projectPath">Path to a project file. If it already exists it will be overwritten</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task SaveProjectAsync(string projectPath, CancellationToken cancellation = default(CancellationToken))
        {
            await CheckInitialized();

            _streamString.WriteString($"{SaveProjectID}*{projectPath}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"SaveProject failed: {response}");
        }


        /// <summary>
        /// Closes the open project
        /// </summary>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task CloseProjectAsync(CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{CloseProjectID}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"CloseProject failed: {response}");
        }

        /// <summary>
        /// Import a 2250 XML file
        /// </summary>
        /// <remarks>The first partition in the project will be selected in the browser</remarks>
        /// <param name="xmlPath">Path to an XML file exported from MPS when using the export to the Partner software (PS: This is an internal format and may differ from the general XML export)</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task Import2250XMLAsync(string xmlPath, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{Import2250XMLID}*{xmlPath}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Import2250XML failed: {response}");
        }

        /// <summary>
        /// Exports the current project using the default export settings.
        /// </summary>
        /// <param name="outputPath">The path of the exported file(s). If a text file is specified text export is used and if an Excel file is specified Excel export is used</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <remarks>Only measurements included in the current selection of the project browser are included in the export</remarks>
        /// <returns>An awaitable task</returns>
        public async Task ExportAsync(string outputPath, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{ExportID}*{outputPath}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Export failed: {response}");
        }

        /// <summary>
        /// Creates a report for the current project using the default export settings.
        /// </summary>
        /// <param name="outputPath">The path of the created report</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <remarks>Only partitions included in the current selection of the project browser are included in the report</remarks>
        /// <returns>An awaitable task</returns>
        public async Task ReportAsync(string outputPath, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{ReportID}*{outputPath}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Report failed: {response}");
        }

        /// <summary>
        /// Switch task (horizontal tab)
        /// </summary>
        /// <param name="task">The task name to switch to</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns></returns>
        public async Task SwitchTaskAsync(string task, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{SwitchTaskID}*{task}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"SwitchTask failed: {response}");
        }

        /// <summary>
        /// Switch language of the user interface and the exports and reports
        /// </summary>
        /// <param name="language">The language identifier, i.e. "en-US" for English or "da-DK" for Danish</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task SetLanguageAsync(string language, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{SetLanguageID}*{language}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Set Language failed: {response}");
        }

        /// <summary>
        /// Toggles the right side panel
        /// </summary>
        /// <param name="open">Open or closes right side menu </param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task ToggleRightSidePanelAsync(bool open, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{ToggleRightSideMenuID}*{open}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Toggle right panel failed: {response}");
        }

        /// <summary>
        /// Toggles the menus in right side panel.
        /// </summary>
        /// <param name="open">true / false to open / close menu expander</param>
        /// <param name="menu">Name of menu to modify</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task SwitchRightSideSubMenuAsync(bool open, string menu, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{SwitchRightSideSubMenuID}*{open}*{menu}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Toggle right menus failed: {response}");
        }

        /// <summary>
        /// Toggles the left side panel
        /// </summary>
        /// <param name="open">Open or closes left side menu </param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task ToggleLeftSidePanelAsync(bool open, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{ToggleLeftSideMenuID}*{open}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Toggle right panel failed: {response}");
        }

        /// <summary>
        /// Toggles the menus in left side panel.
        /// </summary>
        /// <param name="open">true / false to open / close menu expander</param>
        /// <param name="menu">Name of menu to modify</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task SwitchLeftSideSubMenuAsync(bool open, string menu, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{SwitchLeftSideSubMenuID}*{open}*{menu}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Toggle right menus failed: {response}");
        }

        /// <summary>
        /// Toggles the menus in left side panel.
        /// </summary>
        /// <param name="open">true / false to open / close menu expander</param>
        /// <param name="menu">Name of menu to modify</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task SubMenuPropertiesToggle(bool open, string menu, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{SubMenuPropertiesToggleID}*{open}*{menu}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Toggle right menus failed: {response}");
        }

        /// <summary>
        /// Clicks on the + button to add new lines
        /// </summary>
        /// <param name="menu">Name of the sub menu that should add line</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task SubMenuAddLine(string menu, CancellationToken cancellation)
        {
            await CheckInitialized();

            _streamString.WriteString($"{SubMenuAddLineID}*{menu}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"Toggle right menus failed: {response}");
        }

        /// <summary>
        /// Selects or deselects all measurements in project
        /// </summary>
        /// <param name="select">True if all measurements should be selected. False if everything should be deselected</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task SelectAllAsync(bool select, CancellationToken cancellation = default(CancellationToken))
        {
            await CheckInitialized();

            _streamString.WriteString($"{SelectAllID}*{select}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"SelectAll failed: {response}");
        }

        /// <summary>
        /// Exports the current project including impulse analysis. The fast data will be decimated to the specified decimation factor
        /// </summary>
        /// <param name="outputPath">The path of the exported file(s). If a text file is specified text export is used and if an Excel file is specified Excel export is used</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <param name="minDecimation">The minimum decimation factor to use</param>
        /// <param name="maxDecimation">The maximum decimation factor to use</param>
        /// <remarks>Only partitions included in the current selection of the project browser are included in the export</remarks>
        /// <returns>An awaitable task</returns>
        public async Task ExportImpulsesAsync(string outputPath, int minDecimation, int maxDecimation, CancellationToken cancellation = default(CancellationToken))
        {
            await CheckInitialized();

            _streamString.WriteString($"{ExportImpulsesID}*{outputPath}*{minDecimation}*{maxDecimation}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"ExportImpulses failed: {response}");
        }

        /// <summary>
        /// Connects a NAS disc and opens the most recent project from this.
        /// </summary>
        /// <param name="nasPath">The path to the NAS disc (when mounted as a drive in Windows)</param>
        /// <param name="projectName">The name of the project to open</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task ImportFromNasAsync(string nasPath, string projectName, CancellationToken cancellation = default(CancellationToken))
        {
            await CheckInitialized();

            _streamString.WriteString($"{ImportFromNasID}*{nasPath}*{projectName}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"ImportFromNasAsync failed: {response}");
        }

        /// <summary>
        /// Connects an instrument and imports a project from this.
        /// </summary>
        /// <param name="IP">The IP adress of the instrument to connect</param>
        /// <param name="projectName">The name of the project to open</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        /// <remarks>Only the most recent 100 projects are available</remarks>
        public async Task ImportFromInstrumentAsync(string IP, string projectName, CancellationToken cancellation = default(CancellationToken))
        {
            await CheckInitialized();

            _streamString.WriteString($"{ImportFromInstrumentID}*{IP}*{projectName}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"ImportFromInstrumentAsync failed: {response}");
        }

        /// <summary>
        /// Connects an instrument and imports a measurement from this.
        /// </summary>
        /// <param name="IP">The IP adress of the instrument to connect</param>
        /// <param name="measurementURL">The URL of the measurement to import (e.g. "http://192.168.1.87/webxi/applications/SLM/Data/2025_03_25/2025_03_25_11_00_11$00_00_10$11$2"). 
        /// Specify a date folder to get all measurements from that date  (e.g. "http://192.168.1.87/webxi/applications/SLM/Data/2025_03_25")</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task ImportMeasurementFromInstrumentAsync(string IP, string measurementURL, CancellationToken cancellation = default(CancellationToken))
        {
            await CheckInitialized();

            _streamString.WriteString($"{ImportMeasurementFromInstrumentID}*{IP}*{measurementURL}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"ImportMeasurementFromInstrumentAsync failed: {response}");
        }

        /// <summary>
        /// Imports a measurement from NAS disk.
        /// </summary>
        /// <param name="nasPath">The path to the NAS disc (when mounted as a drive in Windows)</param>
        /// <param name="path">The path to the measurement to import (e.g. "C:\BK2255-000501\data\2024_02_19\2024_02_19_11_18_16$00_00_13$01$0"). 
        /// Specify a date folder to get all measurements from that date  (e.g. "C:\BK2255-000501\data\2024_02_19")</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        public async Task ImportMeasurementFromNASAsync(string nasPath, string path, CancellationToken cancellation = default(CancellationToken))
        {
            await CheckInitialized();

            _streamString.WriteString($"{ImportMeasurementFromNASID}*{nasPath}*{path}");
            var response = await _streamString.ReadStringAsync(cancellation);
            if (response != "OK")
                throw new Exception($"ImportMeasurementFromNASAsync failed: {response}");
        }

    }
}
