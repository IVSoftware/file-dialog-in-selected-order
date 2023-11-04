using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace file_dialog_in_selected_order
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            buttonOpen.Click += (sender, e) =>
            {
                if (DialogResult.OK == ExecOpenInOrder(sender, e))
                {
                    MessageBox.Show(string.Join(Environment.NewLine, NamesInOrder), caption: "Names in Order");
                }
            }; 
        }

        const int MAX_STRING = 256;
        const string OPEN_FILE_TITLE = "Open";
        private DialogResult ExecOpenInOrder(object? sender, EventArgs e)
        {
            NamesInOrder.Clear();
            openFileDialog.Title = OPEN_FILE_TITLE;
            openFileDialog.InitialDirectory =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetEntryAssembly().GetName().Name
            );
            Directory.CreateDirectory(openFileDialog.InitialDirectory);
            openFileDialog.Multiselect = true;
            var dialogResult = DialogResult.None;
            localStartPollForChanges();
            dialogResult = openFileDialog.ShowDialog();
            return dialogResult;

            async void localStartPollForChanges()
            {
                while (dialogResult == DialogResult.None)
                {
                    var hWndParent = GetForegroundWindow();
                    StringBuilder sb = new StringBuilder(MAX_STRING);
                    if (hWndParent != IntPtr.Zero)
                    {
                        GetWindowText(hWndParent, sb, MAX_STRING);
                    }
                    Debug.WriteLine($"\n\nForeground window title: {sb}");
                    if (sb.ToString() == OPEN_FILE_TITLE)
                    {
                        EnumChildWindows(hWndParent, localEnumChildWindowCallback, IntPtr.Zero);
                    }
                    await Task.Delay(TimeSpan.FromSeconds(0.1));
                }
            }

            bool localEnumChildWindowCallback(IntPtr hWnd, IntPtr lParam)
            {
                StringBuilder className = new StringBuilder(MAX_STRING);
                GetClassName(hWnd, className, MAX_STRING);

                if (className.ToString() == "ComboBoxEx32")
                {
                    StringBuilder windowText = new StringBuilder(MAX_STRING);
                    GetWindowText(hWnd, windowText, MAX_STRING);

                    // Detect multiselect
                    var names = localGetNames(windowText.ToString());
                    foreach (var name in NamesInOrder.ToArray())
                    {
                        if(!names.Contains(name))
                        {
                            // Remove any names that aren't in new selection
                            NamesInOrder.Remove(name);
                        }
                    }
                    foreach (var name in names.ToArray())
                    {
                        // If NamesInOrder doesn't already hold the name, add it to the end.
                        if (!NamesInOrder.Contains(name))
                        {
                            NamesInOrder.Add(name);
                        }
                    }
                    Debug.WriteLine(string.Join(Environment.NewLine, NamesInOrder));
                    return false;
                }
                return true;
            }
            string[] localGetNames(string text)
            {
                string[] names =
                        Regex
                        .Matches(text.ToString(), pattern: @"""(.*?)\""")
                        .Select(_ => _.Value.Trim(@"""".ToCharArray()))
                        .ToArray();
                // But it there's only one name, the pattern
                // will never 'hit' so return the single name 
                return names.Any() ? names : new string[] { text };
            }
        }
        List<string> NamesInOrder { get; } = new List<string>();

        #region P I N V O K E
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        #endregion P I N V O K E
    }
}