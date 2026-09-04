using System.Windows;
using System.Windows.Documents;

namespace EZPos.UI.Dialogs
{
    /// <summary>Read-only print preview for a built label FixedDocument, using WPF's native DocumentViewer.</summary>
    public partial class PrintPreviewWindow : Window
    {
        public PrintPreviewWindow(FixedDocument document)
        {
            InitializeComponent();
            Viewer.Document = document;
        }
    }
}
