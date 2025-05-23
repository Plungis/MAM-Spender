using System;
using System.Drawing;
using System.Windows.Forms;

namespace MAMAutoPoints
{
    public class ErrorDialog : Form
    {
        private TextBox textBoxError;
        private Button buttonCopy;
        private Button buttonOK;

        public ErrorDialog(string errorMessage)
        {
            this.Text = "Error";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            textBoxError = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Top,
                Height = 300,
                Text = errorMessage,
                Font = new Font("Consolas", 10),
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            this.Controls.Add(textBoxError);

            Panel panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };
            this.Controls.Add(panelButtons);

            buttonCopy = new Button
            {
                Text = "Copy",
                Location = new Point(10, 10),
                Size = new Size(100, 30)
            };
            buttonCopy.Click += (s, e) =>
            {
                Clipboard.SetText(errorMessage);
            };
            panelButtons.Controls.Add(buttonCopy);

            buttonOK = new Button
            {
                Text = "OK",
                Location = new Point(120, 10),
                Size = new Size(100, 30)
            };
            buttonOK.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            panelButtons.Controls.Add(buttonOK);
        }
    }
}
