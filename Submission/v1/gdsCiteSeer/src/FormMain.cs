//
//  Acknowledgements:
//      - Async processing code based on MSDN library series 
//          "Safe, Simple Multithreading in Windows Forms" by Chris Sells
//
//      - Browse for folder component from MSKB sample
//          MSKB306825: How To Implement a Managed Component that Wraps the Browse For Folder Common Dialog Box by Using Visual C# .NET
//          http://support.microsoft.com/default.aspx?scid=kb;en-us;306285
//  
//      - Google Interop library from Google code sample MSNMessengerComponent
//
//
using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using Microsoft.Win32;

namespace gdsCiteSeer
{
    public class FormMain : System.Windows.Forms.Form
    {
        #region ShowProgress
        internal class ShowProgressArgs : EventArgs 
        {
            public string   FilePath;
            public int      FileCount;
            public int      FileCountSoFar;
            public bool     Completed;
            public bool     Cancel;
            public string   Message;

            public ShowProgressArgs(string filepath) 
            {
                this.FilePath = filepath;
                this.FileCount = 0;
                this.FileCountSoFar = 0;
                this.Completed = false;
                this.Cancel = false;
                this.Message = "";
            }
        }

        delegate void ShowProgressHandler(object sender, ShowProgressArgs e);
        delegate void CiteSeerIndexDelegate(string filepath);

        private enum EProcessingState 
        {
            Idle,
            Working,
            Canceled,
        };

        private EProcessingState processingState = EProcessingState.Idle;

        void ShowProgress(object sender, ShowProgressArgs e) 
        {
            // Send to right thread
            if (!this.InvokeRequired) 
            {
                progressBarIndex.Maximum = e.FileCount;
                progressBarIndex.Value = e.FileCountSoFar;
                if (e.Message != null & e.Message.Length > 0)
                {
                    listViewMessages.Items.Insert(0, e.Message);
                    listViewMessages.Items[0].Selected = true;
                    listViewMessages.Focus();
                }

                // Check for Cancel
                e.Cancel = (this.processingState == EProcessingState.Canceled);


                // Check for completion
                if (e.Cancel || e.Completed) 
                {
                    this.processingState = EProcessingState.Idle;
                    buttonIndex.Text = "Index";
                    buttonIndex.Enabled = true;
                    textBoxFilePath.Enabled = true;
                    buttonBrowseFilePath.Enabled = true;
                    buttonClose.Enabled = true;
                    progressBarIndex.Value = 0;
                }
            }
            else 
            {
                // Transfer control to correct thread
                ShowProgressHandler showProgress = new ShowProgressHandler(ShowProgress);
                Invoke(showProgress, new object[] { sender, e});
            }
        }
        #endregion ShowProgress

        #region CiteSeerIndex
        private void CiteSeerIndex(string filepath) 
        {
            object sender = System.Threading.Thread.CurrentThread;
            ShowProgressArgs processDetails = new ShowProgressArgs(filepath);

            listViewMessages.Items.Clear();

            // Get list of files to process
            ArrayList citeSeerFilenames = new ArrayList();

            if (File.Exists(filepath))
            {
                citeSeerFilenames.Add(filepath);
            }
            else if (Directory.Exists(filepath))
            {
                string[] filelist = Directory.GetFiles(filepath);
                if (filelist != null)
                {
                    for (int idx=0; idx < filelist.Length; idx++)
                        citeSeerFilenames.Add(filelist[idx]);
                }
            }

            // Make sure at least file is available
            if (citeSeerFilenames.Count == 0) 
            {
                MessageBox.Show("'" + filepath + "' does not specify a file or a directory.");
                processDetails.Completed = true;
                ShowProgress(sender, processDetails);
                return;
            }
            //throw new ArgumentException("'" + filepath + "' does not specify a file or a directory.");

            // Initialize index task
            CiteSeerIndexer indexTask = new CiteSeerIndexer();
            try
            {
                indexTask.Register();

                processDetails.FileCount = citeSeerFilenames.Count;
                processDetails.FileCountSoFar = 0;

                // Show progress (ignoring Cancel so soon)
                ShowProgress(sender, processDetails);

                // Drop process priority temporarily
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

                // Process all metadata files
                for (int idx=0; idx < citeSeerFilenames.Count; idx++)
                {
                    string filename = (string) citeSeerFilenames[idx];
                
                    indexTask.CiteSeerIndex(filename);

                    processDetails.FileCountSoFar++;

                    processDetails.Message = "Processing file " + filename;

                    ShowProgress(sender, processDetails);

                    if (processDetails.Cancel) 
                        break;
                }

                processDetails.Message = String.Empty;
                processDetails.Completed = true;
                ShowProgress(sender, processDetails);
            }
            catch(Exception e)
            {
                processDetails.Completed = true;
                ShowProgress(sender, processDetails);
                MessageBox.Show(e.Message);
            }
            finally
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                indexTask.Unregister();
            }
        }
        #endregion CiteSeerIndex

        #region Form
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageIndex;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ListView listViewMessages;
        private System.Windows.Forms.ProgressBar progressBarIndex;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonBrowseFilePath;
        private System.Windows.Forms.Button buttonIndex;
        private System.Windows.Forms.TextBox textBoxFilePath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPageQueryBuilder;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxFirstName;
        private System.Windows.Forms.TextBox textBoxLastName;
        private System.Windows.Forms.Button buttonGenerateQuery;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxEncodedQuery;
        private System.Windows.Forms.TextBox textBoxPlainTextQuery;
        private System.Windows.Forms.Button buttonSendQueryToGDS;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxPubMonth;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBoxPubYear;
        private System.Windows.Forms.Button buttonClearAllValues;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public FormMain()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
        }

        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if (components != null) 
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }
        #endregion Form

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(FormMain));
            this.buttonClose = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageIndex = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.listViewMessages = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.progressBarIndex = new System.Windows.Forms.ProgressBar();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonBrowseFilePath = new System.Windows.Forms.Button();
            this.buttonIndex = new System.Windows.Forms.Button();
            this.textBoxFilePath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPageQueryBuilder = new System.Windows.Forms.TabPage();
            this.buttonSendQueryToGDS = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxPlainTextQuery = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonClearAllValues = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.textBoxPubYear = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxPubMonth = new System.Windows.Forms.TextBox();
            this.buttonGenerateQuery = new System.Windows.Forms.Button();
            this.textBoxLastName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxFirstName = new System.Windows.Forms.TextBox();
            this.textBoxEncodedQuery = new System.Windows.Forms.TextBox();
            this.tabControl1.SuspendLayout();
            this.tabPageIndex.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabPageQueryBuilder.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonClose
            // 
            this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClose.Location = new System.Drawing.Point(432, 472);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(88, 24);
            this.buttonClose.TabIndex = 4;
            this.buttonClose.Text = "Close";
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageIndex);
            this.tabControl1.Controls.Add(this.tabPageQueryBuilder);
            this.tabControl1.Location = new System.Drawing.Point(8, 8);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(520, 440);
            this.tabControl1.TabIndex = 14;
            // 
            // tabPageIndex
            // 
            this.tabPageIndex.Controls.Add(this.groupBox3);
            this.tabPageIndex.Controls.Add(this.groupBox2);
            this.tabPageIndex.Location = new System.Drawing.Point(4, 22);
            this.tabPageIndex.Name = "tabPageIndex";
            this.tabPageIndex.Size = new System.Drawing.Size(512, 414);
            this.tabPageIndex.TabIndex = 0;
            this.tabPageIndex.Text = "Index";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.listViewMessages);
            this.groupBox3.Controls.Add(this.progressBarIndex);
            this.groupBox3.Location = new System.Drawing.Point(8, 136);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(496, 264);
            this.groupBox3.TabIndex = 14;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Progress";
            // 
            // listViewMessages
            // 
            this.listViewMessages.AutoArrange = false;
            this.listViewMessages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                               this.columnHeader1});
            this.listViewMessages.FullRowSelect = true;
            this.listViewMessages.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listViewMessages.HideSelection = false;
            this.listViewMessages.Location = new System.Drawing.Point(16, 64);
            this.listViewMessages.MultiSelect = false;
            this.listViewMessages.Name = "listViewMessages";
            this.listViewMessages.Size = new System.Drawing.Size(464, 192);
            this.listViewMessages.TabIndex = 10;
            this.listViewMessages.TabStop = false;
            this.listViewMessages.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Messages";
            this.columnHeader1.Width = 120;
            // 
            // progressBarIndex
            // 
            this.progressBarIndex.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarIndex.Location = new System.Drawing.Point(16, 32);
            this.progressBarIndex.Name = "progressBarIndex";
            this.progressBarIndex.Size = new System.Drawing.Size(464, 16);
            this.progressBarIndex.TabIndex = 9;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.buttonBrowseFilePath);
            this.groupBox2.Controls.Add(this.buttonIndex);
            this.groupBox2.Controls.Add(this.textBoxFilePath);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(8, 8);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(504, 112);
            this.groupBox2.TabIndex = 13;
            this.groupBox2.TabStop = false;
            // 
            // buttonBrowseFilePath
            // 
            this.buttonBrowseFilePath.Location = new System.Drawing.Point(456, 32);
            this.buttonBrowseFilePath.Name = "buttonBrowseFilePath";
            this.buttonBrowseFilePath.Size = new System.Drawing.Size(32, 24);
            this.buttonBrowseFilePath.TabIndex = 11;
            this.buttonBrowseFilePath.Text = "...";
            this.buttonBrowseFilePath.Click += new System.EventHandler(this.buttonBrowseFilePath_Click);
            // 
            // buttonIndex
            // 
            this.buttonIndex.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonIndex.Location = new System.Drawing.Point(424, 72);
            this.buttonIndex.Name = "buttonIndex";
            this.buttonIndex.Size = new System.Drawing.Size(72, 24);
            this.buttonIndex.TabIndex = 10;
            this.buttonIndex.Text = "Index";
            this.buttonIndex.Click += new System.EventHandler(this.buttonIndex_Click);
            // 
            // textBoxFilePath
            // 
            this.textBoxFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFilePath.Location = new System.Drawing.Point(176, 32);
            this.textBoxFilePath.Name = "textBoxFilePath";
            this.textBoxFilePath.Size = new System.Drawing.Size(272, 20);
            this.textBoxFilePath.TabIndex = 9;
            this.textBoxFilePath.Text = "";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(155, 16);
            this.label1.TabIndex = 8;
            this.label1.Text = "CiteSeer Metadata Files Path:";
            // 
            // tabPageQueryBuilder
            // 
            this.tabPageQueryBuilder.Controls.Add(this.buttonSendQueryToGDS);
            this.tabPageQueryBuilder.Controls.Add(this.label5);
            this.tabPageQueryBuilder.Controls.Add(this.label4);
            this.tabPageQueryBuilder.Controls.Add(this.textBoxPlainTextQuery);
            this.tabPageQueryBuilder.Controls.Add(this.panel1);
            this.tabPageQueryBuilder.Controls.Add(this.textBoxEncodedQuery);
            this.tabPageQueryBuilder.Location = new System.Drawing.Point(4, 22);
            this.tabPageQueryBuilder.Name = "tabPageQueryBuilder";
            this.tabPageQueryBuilder.Size = new System.Drawing.Size(512, 414);
            this.tabPageQueryBuilder.TabIndex = 1;
            this.tabPageQueryBuilder.Text = "Query Builder";
            // 
            // buttonSendQueryToGDS
            // 
            this.buttonSendQueryToGDS.Enabled = false;
            this.buttonSendQueryToGDS.Location = new System.Drawing.Point(288, 376);
            this.buttonSendQueryToGDS.Name = "buttonSendQueryToGDS";
            this.buttonSendQueryToGDS.Size = new System.Drawing.Size(192, 24);
            this.buttonSendQueryToGDS.TabIndex = 7;
            this.buttonSendQueryToGDS.Text = "Send Query to Google Desktop";
            this.buttonSendQueryToGDS.Click += new System.EventHandler(this.buttonSendQueryToGDS_Click);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(16, 264);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(136, 16);
            this.label5.TabIndex = 12;
            this.label5.Text = "Encoded Query:";
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(16, 136);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(136, 16);
            this.label4.TabIndex = 11;
            this.label4.Text = "Plain-text Query:";
            // 
            // textBoxPlainTextQuery
            // 
            this.textBoxPlainTextQuery.Location = new System.Drawing.Point(16, 152);
            this.textBoxPlainTextQuery.Multiline = true;
            this.textBoxPlainTextQuery.Name = "textBoxPlainTextQuery";
            this.textBoxPlainTextQuery.ReadOnly = true;
            this.textBoxPlainTextQuery.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxPlainTextQuery.Size = new System.Drawing.Size(480, 88);
            this.textBoxPlainTextQuery.TabIndex = 7;
            this.textBoxPlainTextQuery.Text = "";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.buttonClearAllValues);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.textBoxPubYear);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.textBoxPubMonth);
            this.panel1.Controls.Add(this.buttonGenerateQuery);
            this.panel1.Controls.Add(this.textBoxLastName);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.textBoxFirstName);
            this.panel1.Location = new System.Drawing.Point(8, 16);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(488, 112);
            this.panel1.TabIndex = 9;
            // 
            // buttonClearAllValues
            // 
            this.buttonClearAllValues.Location = new System.Drawing.Point(240, 72);
            this.buttonClearAllValues.Name = "buttonClearAllValues";
            this.buttonClearAllValues.Size = new System.Drawing.Size(104, 24);
            this.buttonClearAllValues.TabIndex = 5;
            this.buttonClearAllValues.Text = "Clear all values";
            this.buttonClearAllValues.Click += new System.EventHandler(this.buttonClearAllValues_Click);
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(272, 40);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(64, 16);
            this.label7.TabIndex = 17;
            this.label7.Text = "Pub Year:";
            // 
            // textBoxPubYear
            // 
            this.textBoxPubYear.AutoSize = false;
            this.textBoxPubYear.Location = new System.Drawing.Point(336, 40);
            this.textBoxPubYear.MaxLength = 4;
            this.textBoxPubYear.Name = "textBoxPubYear";
            this.textBoxPubYear.Size = new System.Drawing.Size(48, 20);
            this.textBoxPubYear.TabIndex = 4;
            this.textBoxPubYear.Text = "";
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(272, 8);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(64, 16);
            this.label6.TabIndex = 15;
            this.label6.Text = "Pub Month:";
            // 
            // textBoxPubMonth
            // 
            this.textBoxPubMonth.AutoSize = false;
            this.textBoxPubMonth.Location = new System.Drawing.Point(336, 8);
            this.textBoxPubMonth.MaxLength = 2;
            this.textBoxPubMonth.Name = "textBoxPubMonth";
            this.textBoxPubMonth.Size = new System.Drawing.Size(32, 20);
            this.textBoxPubMonth.TabIndex = 3;
            this.textBoxPubMonth.Text = "";
            // 
            // buttonGenerateQuery
            // 
            this.buttonGenerateQuery.Location = new System.Drawing.Point(352, 72);
            this.buttonGenerateQuery.Name = "buttonGenerateQuery";
            this.buttonGenerateQuery.Size = new System.Drawing.Size(120, 24);
            this.buttonGenerateQuery.TabIndex = 6;
            this.buttonGenerateQuery.Text = "Generate Query";
            this.buttonGenerateQuery.Click += new System.EventHandler(this.buttonGenerateQuery_Click);
            // 
            // textBoxLastName
            // 
            this.textBoxLastName.AutoSize = false;
            this.textBoxLastName.Location = new System.Drawing.Point(88, 40);
            this.textBoxLastName.Name = "textBoxLastName";
            this.textBoxLastName.Size = new System.Drawing.Size(168, 20);
            this.textBoxLastName.TabIndex = 2;
            this.textBoxLastName.Text = "";
            this.textBoxLastName.TextChanged += new System.EventHandler(this.textBoxLastName_TextChanged);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(16, 40);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 16);
            this.label3.TabIndex = 11;
            this.label3.Text = "Last name:";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(16, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 16);
            this.label2.TabIndex = 10;
            this.label2.Text = "First name:";
            // 
            // textBoxFirstName
            // 
            this.textBoxFirstName.AutoSize = false;
            this.textBoxFirstName.Location = new System.Drawing.Point(88, 8);
            this.textBoxFirstName.Name = "textBoxFirstName";
            this.textBoxFirstName.Size = new System.Drawing.Size(168, 20);
            this.textBoxFirstName.TabIndex = 1;
            this.textBoxFirstName.Text = "";
            this.textBoxFirstName.TextChanged += new System.EventHandler(this.textBoxFirstName_TextChanged);
            // 
            // textBoxEncodedQuery
            // 
            this.textBoxEncodedQuery.Location = new System.Drawing.Point(16, 280);
            this.textBoxEncodedQuery.Multiline = true;
            this.textBoxEncodedQuery.Name = "textBoxEncodedQuery";
            this.textBoxEncodedQuery.ReadOnly = true;
            this.textBoxEncodedQuery.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxEncodedQuery.Size = new System.Drawing.Size(480, 80);
            this.textBoxEncodedQuery.TabIndex = 8;
            this.textBoxEncodedQuery.Text = "";
            // 
            // FormMain
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(546, 520);
            this.ControlBox = false;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.buttonClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "gdsCiteSeer";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPageIndex.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.tabPageQueryBuilder.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() 
        {
            Application.Run(new FormMain());
        }

        private void buttonBrowseFilePath_Click(object sender, System.EventArgs e)
        {
            Microsoft.Samples.WinForms.Extras.FolderBrowser folderBrowser = new Microsoft.Samples.WinForms.Extras.FolderBrowser();

            folderBrowser.StartLocation = Microsoft.Samples.WinForms.Extras.FolderBrowser.FolderID.MyComputer;
            if (DialogResult.OK == folderBrowser.ShowDialog())
                textBoxFilePath.Text = folderBrowser.DirectoryPath;
        }

        #region Event Handlers
        private void buttonClose_Click(object sender, System.EventArgs e)
        {
            Application.Exit();
        }

        private void buttonIndex_Click(object sender, System.EventArgs e)
        {
            switch (this.processingState) 
            {
                case EProcessingState.Idle:
                    this.processingState = EProcessingState.Working;
                    buttonIndex.Text = "Cancel";
                    textBoxFilePath.Enabled = false;
                    buttonBrowseFilePath.Enabled = false;
                    buttonClose.Enabled = false;

                    CiteSeerIndexDelegate citeseerIndex = new CiteSeerIndexDelegate(CiteSeerIndex);
                    citeseerIndex.BeginInvoke((string) textBoxFilePath.Text, null, null);
                    break;

                case EProcessingState.Working:
                    this.processingState = EProcessingState.Canceled;
                    buttonIndex.Enabled = false;
                    break;
            }
        }

        private void FormMain_Load(object sender, System.EventArgs e)
        {
            textBoxFilePath.Text = Path.Combine(Application.StartupPath, "citeseer");
            listViewMessages.Columns[0].Width = 1000;
        }

        private void textBoxFirstName_TextChanged(object sender, System.EventArgs e)
        {
            textBoxPlainTextQuery.Text = String.Empty;
            textBoxEncodedQuery.Text = String.Empty;
            buttonSendQueryToGDS.Enabled = false;
        }

        private void textBoxLastName_TextChanged(object sender, System.EventArgs e)
        {
            textBoxPlainTextQuery.Text = String.Empty;
            textBoxEncodedQuery.Text = String.Empty;
            buttonSendQueryToGDS.Enabled = false;
        }

        private void buttonSendQueryToGDS_Click(object sender, System.EventArgs e)
        {
            try
            {
                RegistryKey GoogleAPIKey = Registry.CurrentUser.OpenSubKey(@"Software\Google\Google Desktop\API");
                string searchURL = (string) GoogleAPIKey.GetValue("search_url");

                if (searchURL != null)
                    Process.Start(searchURL + textBoxEncodedQuery.Text);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Unable to start Google Desktop query", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void buttonGenerateQuery_Click(object sender, System.EventArgs e)
        {
            string queryName        = String.Empty;
            string queryPubMonth    = String.Empty;
            string queryPubYear     = String.Empty;

            textBoxPlainTextQuery.Text = String.Empty;
            textBoxEncodedQuery.Text = String.Empty;
            buttonSendQueryToGDS.Enabled = false;

            // Build name query
            if (textBoxFirstName.Text != String.Empty && textBoxLastName.Text != String.Empty)
            {
                // full name
                queryName = FieldBuilder.BuildField(FieldBuilder.EFieldNames.FullName, textBoxFirstName.Text.Trim() + " " + textBoxLastName.Text.Trim());
            }
            else if (textBoxFirstName.Text != String.Empty)
            {
                // first name
                queryName = FieldBuilder.BuildField(FieldBuilder.EFieldNames.FirstName, textBoxFirstName.Text);
            }
            else if (textBoxLastName.Text != String.Empty)
            {
                // last name
                queryName = FieldBuilder.BuildField(FieldBuilder.EFieldNames.LastName, textBoxLastName.Text);
            }

            // Build publication month query
            if (textBoxPubMonth.Text != String.Empty)
            {
                queryPubMonth = FieldBuilder.BuildField(FieldBuilder.EFieldNames.PubMonth, textBoxPubMonth.Text);
            }

            // Build publication year query
            if (textBoxPubYear.Text != String.Empty)
            {
                queryPubYear = FieldBuilder.BuildField(FieldBuilder.EFieldNames.PubYear, textBoxPubYear.Text);
            }

            // Show constructed query
            if (queryName.Length > 0)
            {
                if (textBoxPlainTextQuery.Text.Length > 0)
                    textBoxPlainTextQuery.Text += " AND ";
                textBoxPlainTextQuery.Text += queryName;

                if (textBoxEncodedQuery.Text.Length > 0)
                    textBoxEncodedQuery.Text += " AND ";
                textBoxEncodedQuery.Text += FieldBuilder.EncodeField(queryName);
            }

            if (queryPubMonth.Length > 0)
            {
                if (textBoxPlainTextQuery.Text.Length > 0)
                    textBoxPlainTextQuery.Text += " AND ";
                textBoxPlainTextQuery.Text += queryPubMonth;

                if (textBoxEncodedQuery.Text.Length > 0)
                    textBoxEncodedQuery.Text += " AND ";
                textBoxEncodedQuery.Text += FieldBuilder.EncodeField(queryPubMonth);
            }

            if (queryPubYear.Length > 0)
            {
                if (textBoxPlainTextQuery.Text.Length > 0)
                    textBoxPlainTextQuery.Text += " AND ";
                textBoxPlainTextQuery.Text += queryPubYear;

                if (textBoxEncodedQuery.Text.Length > 0)
                    textBoxEncodedQuery.Text += " AND ";
                textBoxEncodedQuery.Text += FieldBuilder.EncodeField(queryPubYear);
            }

            buttonSendQueryToGDS.Enabled = (textBoxPlainTextQuery.Text != String.Empty);
        }

        private void buttonClearAllValues_Click(object sender, System.EventArgs e)
        {
            textBoxFirstName.Text = String.Empty;
            textBoxLastName.Text = String.Empty;
            textBoxPubMonth.Text = String.Empty;
            textBoxPubYear.Text = String.Empty;
        }
        #endregion Event Handlers
	}
}
