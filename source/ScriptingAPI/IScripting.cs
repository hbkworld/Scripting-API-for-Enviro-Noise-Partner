using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScriptingAPI
{
    public enum ScriptingApps
    {
        BuildingAcousticsPartner,
        EnviromentalNoisePartner,
        NoisePartner
    }

    public interface IScripting
    {
        /// <summary>
        /// Version of the scripting API for the connected App
        /// </summary>
        int Version { get; set; }

        /// <summary>
        /// Set to true if the scripting api automatically starts up the app. If it does it will close the app when the scripting api is closed
        /// </summary>
        bool StartedFromHere { get; set; }

        /// <summary>
        /// Opens the specified app. If the app is already running this is used
        /// </summary>
        /// <param name="app"></param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task<bool> LaunchAppAsync(ScriptingApps app, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Closes the scripting api
        /// </summary>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task CloseAsync(CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Shuts down the open app - changes will not be saved
        /// </summary>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task ShutDownAppAsync(CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Open the specified project
        /// </summary>
        /// <remarks>The first measurement in the project will be selected in the project browser</remarks>
        /// <param name="projectPath">Path to a project file understood by the App (*.enp)</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task OpenProjectAsync(string projectPath, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Saves the current project
        /// </summary>
        /// <param name="projectPath">Path to a project file. If it already exists it will be overwritten</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task SaveProjectAsync(string projectPath, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Closes the open project
        /// </summary>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task CloseProjectAsync(CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Exports the current project using the default export settings.
        /// </summary>
        /// <param name="outputPath">The path of the exported file(s). If a text file is specified text export is used and if an Excel file is specified Excel export is used</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <remarks>Only measurements included in the current selection of the project browser are included in the export</remarks>
        /// <returns>An awaitable task</returns>
        Task ExportAsync(string outputPath, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Creates a report for the current project using the default export settings.
        /// </summary>
        /// <param name="outputPath">The path of the created report</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <remarks>Only partitions included in the current selection of the project browser are included in the report</remarks>
        /// <returns>An awaitable task</returns>
        Task ReportAsync(string outputPath, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Switch task (horizontal tab)
        /// </summary>
        /// <param name="task">The task name to switch to</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task SwitchTaskAsync(string task, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Switch language of the user interface and the exports and reports
        /// </summary>
        /// <param name="language">The language identifier, i.e. "en-US" for English or "da-DK" for Danish</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task SetLanguageAsync(string language, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Import a 2250 XML file
        /// </summary>
        /// <remarks>The first partition in the project will be selected in the browser</remarks>
        /// <param name="xmlPath">Path to an XML file exported from MPS when using the export to the Partner software (PS: This is an internal format and may differ from the general XML export)</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task Import2250XMLAsync(string xmlPath, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Toggles the right side panel
        /// </summary>
        /// <param name="open">Boolean to open or close side panel</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task ToggleRightSidePanelAsync(bool open, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Toggles the menus in right side panel.
        /// </summary>
        /// <param name="open">Boolean to open or close menu</param>
        /// <param name="menu">Name of the menu that should open/close</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task SwitchRightSideSubMenuAsync(bool open, string menu, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Toggles the left side panel
        /// </summary>
        /// <param name="open">Boolean to open or close side panel</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task ToggleLeftSidePanelAsync(bool open, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Toggles the menus in left side panel.
        /// </summary>
        /// <remarks>The first partition in the project will be selected in the browser</remarks>
        /// <param name="open">Boolean to open or close menu</param>
        /// <param name="menu">Name of the menu that should open/close</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task SwitchLeftSideSubMenuAsync(bool open, string menu, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Toggles the menus in left side panel.
        /// </summary>
        /// <param name="open">Boolean to open or close menu</param>
        /// <param name="menu">Name of the menu that should open/close</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task SubMenuPropertiesToggle(bool open, string menu, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Clicks on the + button to add new lines
        /// </summary>
        /// <param name="menu">Name of the sub menu that should add line</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task SubMenuAddLine(string menu, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Selects or deselects all measurements in project
        /// </summary>
        /// <param name="select">True if all measurements should be selected. False if everything should be deselected</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task SelectAllAsync(bool select, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Exports the current project including impulse analysis. The fast data will be decimated to the specified decimation factor
        /// </summary>
        /// <param name="outputPath">The path of the exported file(s). If a text file is specified text export is used and if an Excel file is specified Excel export is used</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <param name="minDecimation">The minimum decimation factor to use</param>
        /// <param name="maxDecimation">The maximum decimation factor to use</param>
        /// <remarks>Only partitions included in the current selection of the project browser are included in the export</remarks>
        /// <returns>An awaitable task</returns>
        Task ExportImpulsesAsync(string outputPath, int minDecimation, int maxDecimation, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Connects a NAS disc and opens the most recent project from this.
        /// </summary>
        /// <param name="nasPath">The path to the NAS disc (when mounted as a drive in Windows)</param>
        /// <param name="projectName">The name of the project to open</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task ImportFromNasAsync(string nasPath, string projectName, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Connects an instrument and imports a project from this.
        /// </summary>
        /// <param name="IP">The IP adress of the instrument to connect</param>
        /// <param name="projectName">The name of the project to open</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        /// <remarks>Only the most recent 100 projects are available</remarks>
        Task ImportFromInstrumentAsync(string IP, string projectName, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Connects an instrument and imports a measurement from this.
        /// </summary>
        /// <param name="IP">The IP adress of the instrument to connect</param>
        /// <param name="measurementURL">The URL of the measurement to import (e.g. "http://192.168.1.87/webxi/applications/SLM/Data/2025_03_25/2025_03_25_11_00_11$00_00_10$11$2"). 
        /// Specify a date folder to get all measurements from that date  (e.g. "http://192.168.1.87/webxi/applications/SLM/Data/2025_03_25")</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task ImportMeasurementFromInstrumentAsync(string IP, string measurementURL, CancellationToken cancellation = default(CancellationToken));

        /// <summary>
        /// Imports a measurement from NAS disk.
        /// </summary>
        /// <param name="nasPath">The path to the NAS disc (when mounted as a drive in Windows)</param>
        /// <param name="path">The path to the measurement to import (e.g. "C:\BK2255-000501\data\2024_02_19\2024_02_19_11_18_16$00_00_13$01$0"). 
        /// Specify a date folder to get all measurements from that date  (e.g. "C:\BK2255-000501\data\2024_02_19")</param>
        /// <param name="cancellation">Allows for cancelling the asynchronous task</param>
        /// <returns>An awaitable task</returns>
        Task ImportMeasurementFromNASAsync(string nasPath, string path, CancellationToken cancellation = default(CancellationToken));
    }
}
