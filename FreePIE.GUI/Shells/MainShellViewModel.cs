﻿using System;
using System.Linq;
using AvalonDock;
using AvalonDock.Layout.Serialization;
using Caliburn.Micro;
using FreePIE.Core.Common;
using FreePIE.Core.Persistence;
using FreePIE.GUI.Common.AvalonDock;
using FreePIE.GUI.Events;
using FreePIE.GUI.Result;
using FreePIE.GUI.Views.Main;
using FreePIE.GUI.Views.Plugin;
using FreePIE.GUI.Views.Script;
using FreePIE.GUI.Views.Script.Output;
using IEventAggregator = FreePIE.Core.Common.Events.IEventAggregator;

namespace FreePIE.GUI.Shells
{
    public class MainShellViewModel : ShellPresentationModel
    {
        private const string dockingConfig = "layout.config";
        private readonly IEventAggregator eventAggregator;
        private readonly IPersistanceManager persistanceManager;
        private readonly ConsoleViewModel consoleViewModel;
        private readonly ErrorViewModel errorViewModel;
        private readonly WatchesViewModel watchesViewModel;
        private readonly IFileSystem fileSystem;

        public MainShellViewModel(IResultFactory resultFactory,
            IEventAggregator eventAggregator,
            IPersistanceManager persistanceManager,
            ISettingsManager settingsManager,
            MainMenuViewModel mainMenuViewModel,
            ScriptEditorViewModel scriptEditorViewModel,
            ConsoleViewModel consoleViewModel,
            ErrorViewModel errorViewModel,
            WatchesViewModel watchesViewModel,
            IFileSystem fileSystem) : base(resultFactory)
        {
            this.eventAggregator = eventAggregator;
            eventAggregator.Subscribe(this);
            this.persistanceManager = persistanceManager;

            ScriptEditor = scriptEditorViewModel;

            Scripts = new BindableCollection<ScriptEditorViewModel>();
            Tools = new BindableCollection<PanelViewModel>();

            Scripts.Add(ScriptEditor);
            Tools.Add(consoleViewModel);
            Tools.Add(errorViewModel);
            Tools.Add(watchesViewModel);

            Menu = mainMenuViewModel;
            Menu.Plugins = settingsManager.ListConfigurablePluginSettings().Select(ps => new PluginSettingsMenuViewModel(ps));
            Menu.HelpFiles = settingsManager.ListPluginSettingsWithHelpFile().Select(ps => new PluginHelpFileViewModel(ps)).ToList();
            Menu.Views = Tools;

            this.consoleViewModel = consoleViewModel;
            this.errorViewModel = errorViewModel;
            this.watchesViewModel = watchesViewModel;
            this.fileSystem = fileSystem;

            DisplayName = "FreePIE - Programmable Input Emulator";
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            InitDocking();

        }

        private void InitDocking()
        {
            if (!fileSystem.Exists(dockingConfig)) return;
            
            var layoutSerializer = new XmlLayoutSerializer(DockingManager);
            //layoutSerializer.LayoutSerializationCallback += (s, e) =>
            //{
            //    //if (e.Model.ContentId == FileStatsViewModel.ToolContentId)
            //    //    e.Content = Workspace.This.FileStats;
            //    //else if (!string.IsNullOrWhiteSpace(e.Model.ContentId) &&
            //    //    File.Exists(e.Model.ContentId))
            //    //    e.Content = Workspace.This.Open(e.Model.ContentId);
            //};
            
            layoutSerializer.Deserialize(dockingConfig);
        }

        private DockingManager DockingManager
        {
            get { return (this.GetView() as IDockingManagerSource).DockingManager; }
        }

        public BindableCollection<ScriptEditorViewModel> Scripts { get; set; }
        public BindableCollection<PanelViewModel> Tools { get; set; }

        public ScriptEditorViewModel ScriptEditor { get; set; }
        public MainMenuViewModel Menu { get; set; }

        public override void CanClose(Action<bool> callback)
        {
            eventAggregator.Publish(new ExitingEvent());

            persistanceManager.Save();

            var layoutSerializer = new XmlLayoutSerializer(DockingManager);
            layoutSerializer.Serialize(dockingConfig);

            base.CanClose(callback);
        }
    }
}
