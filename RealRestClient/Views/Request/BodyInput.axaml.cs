using System;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using JsonBodyInputViewModel = RealRestClient.ViewModels.Requests.JsonBodyInputViewModel;

namespace RealRestClient.Views.Request
{
    public partial class BodyInput : UserControl
    {
        private TextEditor? _jsonEditor;

        public BodyInput()
        {
            InitializeComponent();
            try
            {
                _jsonEditor = this.FindControl<TextEditor>("JsonEditor");
                if (_jsonEditor != null)
                {
                    // Set up TextMate syntax highlighting
                    var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
                    var textMateInstallation = _jsonEditor.InstallTextMate(registryOptions);
                    textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId("json"));
                    // Set some initial text to verify it's working
                    _jsonEditor.Options.IndentationSize = 2;  // Set indent size to 2 spaces
                    _jsonEditor.Options.ConvertTabsToSpaces = true;  // Use spaces instead of tabs
                    _jsonEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy();

                    _jsonEditor.TextChanged += (sender, args) =>
                    {
                        if (DataContext is JsonBodyInputViewModel vm)
                        {
                            vm.JsonText = _jsonEditor.Text;
                        }
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to find JsonEditor control");
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing BodyInput: {ex}");
            }

        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            
            if (DataContext is JsonBodyInputViewModel vm && _jsonEditor != null)
            {
                _jsonEditor.Text = vm.JsonText;

            }
        }
    }
}