Since `OpenFileDialog` is a sealed class it's probably going to require hacking in one form or another. And the way I personally would hack it is to have a polling loop that makes calls on P/Invoke in order to scrape the child window that holds the file names as they're being selected, and when that changes maintain an external list that we'll call `NamesInOrder` that is FIFO for any filename that is newly selected.

In this case, I first selected B then C and A.

[![names in order of selection][1]][1]

```
public MainForm()
{
    InitializeComponent();
    StartPosition = FormStartPosition.CenterScreen;
    buttonOpen.Click += (sender, e) =>
    {
        ExecOpenInOrder(sender, e);
        MessageBox.Show(string.Join(Environment.NewLine, NamesInOrder), caption: "Names in Order");
    }; 
}
```

[![popup][2]][2]
___

**Method to show the open file dialog**

```
private void ExecOpenInOrder(object? sender, EventArgs e)
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
    openFileDialog.ShowDialog();
    .
    .
    .
}
List<string> NamesInOrder { get; } = new List<string>();
```

**Detecting hWnd for OpenFileDialog**

Although the caption of OpenFileDialog reads "Open" by default, calling `GetWindowText` returns empty unless we *explicitly* set a recognizable value, in this case using `const string OPEN_FILE_TITLE = "Open"`. Once we obtain it, we're going to enumerate its child windows.

```
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
            Debug.WriteLine($"\nForeground window title: {sb}");
            if (sb.ToString() == OPEN_FILE_TITLE)
            {
                EnumChildWindows(hWndParent, localEnumChildWindowCallback, IntPtr.Zero);
            }
            await Task.Delay(TimeSpan.FromSeconds(0.1));
        }
    }
```

**Find the correct child window and read its text**

We're looking for a child window whose class is ""ComboBoxEx32".

```
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

            if (windowText.ToString().Contains(".txt"))
            {
                return false;
            }
        }
        return true;
    }
```

**Extract the file names**

When this window text is retrieved, use a `RegEx` to separate multiple file names where names are in quotes and separated by a space.

```
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
```


  [1]: https://i.stack.imgur.com/bqWmW.png
  [2]: https://i.stack.imgur.com/9gby3.png