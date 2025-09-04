using System;
using System.ComponentModel;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using JsonBodyInputViewModel = RestWave.ViewModels.Requests.JsonBodyInputViewModel;
using Requests_JsonBodyInputViewModel = RestWave.ViewModels.Requests.JsonBodyInputViewModel;

namespace RestWave.Views.Request
{
    public partial class BodyInput : UserControl
    {
        private TextEditor? _jsonEditor;
        public JsonBodyInputViewModel ViewModel => (JsonBodyInputViewModel)this.DataContext!;
 
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
                        if (DataContext is Requests_JsonBodyInputViewModel vm)
                        {
                            vm.JsonText = _jsonEditor.Text;
                        }
                    };

                    _jsonEditor.TextArea.KeyDown += OnTextAreaKeyDown;
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
            
            if (this.ViewModel != null)
            {
                this.ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }


            if (DataContext is Requests_JsonBodyInputViewModel vm && _jsonEditor != null)
            {
                _jsonEditor.Text = vm.JsonText;
                vm.PropertyChanged += OnViewModelPropertyChanged;
            }
        }
        
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(JsonBodyInputViewModel.JsonText) && 
                _jsonEditor != null && 
                DataContext is Requests_JsonBodyInputViewModel vm)
            {
                // Only update if the text is actually different to avoid infinite loops
                if (_jsonEditor.Text != vm.JsonText)
                {
                    _jsonEditor.Text = vm.JsonText;
                }
            }
        }

        private void OnTextAreaKeyDown(object? sender, KeyEventArgs e)
        {
            if (_jsonEditor == null) return;

            var textArea = _jsonEditor.TextArea;
            var document = textArea.Document;
            var caret = textArea.Caret;

            switch (e.Key)
            {
                case Key.OemOpenBrackets when e.KeyModifiers == KeyModifiers.Shift: // '{'
                    InsertMatchingBrace(document, caret, '{', '}');
                    e.Handled = true;
                    break;
                case Key.D9 when e.KeyModifiers == KeyModifiers.Shift: // '('
                    InsertMatchingBrace(document, caret, '(', ')');
                    e.Handled = true;
                    break;
                case Key.OemOpenBrackets when e.KeyModifiers == KeyModifiers.None: // '['
                    InsertMatchingBrace(document, caret, '[', ']');
                    e.Handled = true;
                    break;
                case Key.OemQuotes when e.KeyModifiers == KeyModifiers.None: // '"'
                    InsertMatchingQuote(document, caret, '"');
                    e.Handled = true;
                    break;
            }
        }

        private void InsertMatchingBrace(TextDocument document, Caret caret, char openChar, char closeChar)
        {
            var offset = caret.Offset;

            if (openChar == '{')
            {
                // For braces and brackets, add proper indentation
                var currentLine = document.GetLineByOffset(offset);
                var currentLineText = document.GetText(currentLine.Offset, currentLine.Length);
                var indent = GetIndentation(currentLineText);
                var nextIndent = indent + "  "; // Add 2 spaces for indentation

                var text = $"{openChar}\n{nextIndent}\n{indent}{closeChar}";
                document.Insert(offset, text);
                caret.Offset = offset + openChar.ToString().Length + 1 + nextIndent.Length; // Position cursor after newline and indent
            }
            else
            {
                // For parentheses, just add matching pair
                var text = $"{openChar}{closeChar}";
                document.Insert(offset, text);
                caret.Offset = offset + 1;
            }
        }

        private string GetIndentation(string lineText)
        {
            var indent = "";
            foreach (var ch in lineText)
            {
                if (ch == ' ' || ch == '\t')
                    indent += ch;
                else
                    break;
            }
            return indent;
        }

        private void InsertMatchingQuote(TextDocument document, Caret caret, char quoteChar)
        {
            var offset = caret.Offset;

            // Check if we're next to an existing quote
            if (offset < document.TextLength && document.GetCharAt(offset) == quoteChar)
            {
                // Move cursor past the existing quote
                caret.Offset = offset + 1;
                return;
            }

            // Insert matching quotes
            var text = $"{quoteChar}{quoteChar}";
            document.Insert(offset, text);
            caret.Offset = offset + 1;
        }

        private void FormatJsonButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (this.ViewModel.HasValidationError)
            {
                return;
            }
            
            var jsonDocument = JsonDocument.Parse(this.ViewModel.JsonText);
            var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            this.ViewModel.JsonText = formattedJson;
        }
    }
}