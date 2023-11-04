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
            IntPtr hook = IntPtr.Zero;
            IntPtr hWndCombo = IntPtr.Zero;
            string oldValue = string.Empty;

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

            // Hook the message filter
            hookProc = new HookProc(localComboBoxMessageFilter);
            hook = SetWindowsHookEx(WH_CALLWNDPROC, hookProc, IntPtr.Zero, GetCurrentThreadId());

            if (hook != IntPtr.Zero)
            {
                localStartPollForOpenFileDialogWindow();
                dialogResult = openFileDialog.ShowDialog();
                UnhookWindowsHookEx(hook); // Dispose the hook
            }

            return dialogResult;

            async void localStartPollForOpenFileDialogWindow()
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
                        return;
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
                    // Capture the hWnd of the combo box here
                    hWndCombo = hWnd;
                    return false; // Stop enumerating child windows
                }
                return true;
            }


            nint localComboBoxMessageFilter(int nCode, nint wParam, nint lParam)
            {
                if (nCode >= 0)
                {
                    CWPSTRUCT cwp = (CWPSTRUCT)Marshal.PtrToStructure(lParam, typeof(CWPSTRUCT));
                    if (cwp.message == WM_SETTEXT)
                    {
                        localDetectChangeAsync();
                    }
                }
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

                async void localDetectChangeAsync()
                {
                    StringBuilder sb = new StringBuilder(MAX_STRING);
                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.1));
                        GetWindowText(hWndCombo, sb, MAX_STRING);
                        var newValue = sb.ToString();
                        if (newValue != oldValue)
                        {
                            // Detect multiselect
                            var names = localGetNames(sb.ToString());
                            foreach (var name in NamesInOrder.ToArray())
                            {
                                if (!names.Contains(name))
                                {
                                    // Remove any names that aren't in the new selection
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
                            oldValue = newValue;
                            Debug.WriteLine("\n\n");
                            Debug.WriteLine(string.Join(Environment.NewLine, NamesInOrder));
                            break;
                        }
                    }
                }
            }

            string[] localGetNames(string text)
            {
                string[] names =
                        Regex
                        .Matches(text.ToString(), pattern: @"""(.*?)\""")
                        .Select(_ => _.Value.Trim(@"""".ToCharArray()))
                        .ToArray();
                // But if there's only one name, the pattern
                // will never 'hit', so return the single name
                return names.Any() ? names : new string[] { text };
            }
        }

        List<string> NamesInOrder { get; } = new List<string>();

        #region P I N V O K E

        private const int WH_CALLWNDPROC = 4;
        private const int WM_SETTEXT = 0x000C; 
        private const int CBN_EDITCHANGE = 5;

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static HookProc hookProc;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        private struct CWPSTRUCT
        {
            public IntPtr lParam;
            public IntPtr wParam;
            public int message;
            public IntPtr hwnd;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        #endregion P I N V O K E
    }
}