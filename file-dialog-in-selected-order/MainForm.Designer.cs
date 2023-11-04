namespace file_dialog_in_selected_order
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            openFileDialog = new OpenFileDialog();
            buttonOpen = new Button();
            SuspendLayout();
            // 
            // buttonOpen
            // 
            buttonOpen.Location = new Point(49, 62);
            buttonOpen.Name = "buttonOpen";
            buttonOpen.Size = new Size(112, 34);
            buttonOpen.TabIndex = 0;
            buttonOpen.Text = "Open...";
            buttonOpen.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(478, 244);
            Controls.Add(buttonOpen);
            Name = "MainForm";
            Text = "Main Form";
            ResumeLayout(false);
        }

        #endregion

        private OpenFileDialog openFileDialog;
        private Button buttonOpen;
    }
}