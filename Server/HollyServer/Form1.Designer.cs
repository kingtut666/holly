namespace HollyServer
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.butStart = new System.Windows.Forms.Button();
            this.butStop = new System.Windows.Forms.Button();
            this.butRecogWav = new System.Windows.Forms.Button();
            this.butLWRF = new System.Windows.Forms.Button();
            this.chkLWRFOn = new System.Windows.Forms.CheckBox();
            this.txtLWRFRoom = new System.Windows.Forms.TextBox();
            this.txtLWRFDevice = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.butSay = new System.Windows.Forms.Button();
            this.txtSpeech = new System.Windows.Forms.TextBox();
            this.chkStartOnConnect = new System.Windows.Forms.CheckBox();
            this.butSayRemote = new System.Windows.Forms.Button();
            this.txtRemoteID = new System.Windows.Forms.TextBox();
            this.butPlayWav = new System.Windows.Forms.Button();
            this.butAlarmRecur = new System.Windows.Forms.Button();
            this.txtAlarmTime = new System.Windows.Forms.TextBox();
            this.txtAlarmRecur = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.radioAlarmTime = new System.Windows.Forms.RadioButton();
            this.radioAlarmS = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtAlarmSpeak = new System.Windows.Forms.TextBox();
            this.chkAlarmSayTime = new System.Windows.Forms.CheckBox();
            this.chkAlarmBeep = new System.Windows.Forms.CheckBox();
            this.txtAlarmID = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.prog1 = new System.Windows.Forms.ProgressBar();
            this.prog2 = new System.Windows.Forms.ProgressBar();
            this.prog3 = new System.Windows.Forms.ProgressBar();
            this.lblProg1 = new System.Windows.Forms.Label();
            this.lblProg2 = new System.Windows.Forms.Label();
            this.lblProg3 = new System.Windows.Forms.Label();
            this.txtProg1 = new System.Windows.Forms.TextBox();
            this.txtProg2 = new System.Windows.Forms.TextBox();
            this.txtProg3 = new System.Windows.Forms.TextBox();
            this.progCmd1 = new System.Windows.Forms.ProgressBar();
            this.progCmd2 = new System.Windows.Forms.ProgressBar();
            this.progCmd3 = new System.Windows.Forms.ProgressBar();
            this.txtProgCmd1 = new System.Windows.Forms.TextBox();
            this.txtProgCmd2 = new System.Windows.Forms.TextBox();
            this.txtProgCmd3 = new System.Windows.Forms.TextBox();
            this.ckUseSphinx = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(2, 295);
            this.txtLog.Name = "txtLog";
            this.txtLog.Size = new System.Drawing.Size(945, 270);
            this.txtLog.TabIndex = 1;
            this.txtLog.Text = "";
            // 
            // butStart
            // 
            this.butStart.Location = new System.Drawing.Point(95, 12);
            this.butStart.Name = "butStart";
            this.butStart.Size = new System.Drawing.Size(75, 23);
            this.butStart.TabIndex = 2;
            this.butStart.Text = "Start";
            this.butStart.UseVisualStyleBackColor = true;
            this.butStart.Click += new System.EventHandler(this.butStart_Click);
            // 
            // butStop
            // 
            this.butStop.Location = new System.Drawing.Point(95, 42);
            this.butStop.Name = "butStop";
            this.butStop.Size = new System.Drawing.Size(75, 23);
            this.butStop.TabIndex = 3;
            this.butStop.Text = "Stop";
            this.butStop.UseVisualStyleBackColor = true;
            this.butStop.Click += new System.EventHandler(this.butStop_Click);
            // 
            // butRecogWav
            // 
            this.butRecogWav.Location = new System.Drawing.Point(243, 11);
            this.butRecogWav.Name = "butRecogWav";
            this.butRecogWav.Size = new System.Drawing.Size(75, 23);
            this.butRecogWav.TabIndex = 5;
            this.butRecogWav.Text = "RecogWav";
            this.butRecogWav.UseVisualStyleBackColor = true;
            this.butRecogWav.Click += new System.EventHandler(this.butRecogWav_Click);
            // 
            // butLWRF
            // 
            this.butLWRF.Location = new System.Drawing.Point(27, 157);
            this.butLWRF.Name = "butLWRF";
            this.butLWRF.Size = new System.Drawing.Size(75, 23);
            this.butLWRF.TabIndex = 6;
            this.butLWRF.Text = "SendLWRF";
            this.butLWRF.UseVisualStyleBackColor = true;
            this.butLWRF.Click += new System.EventHandler(this.butLWRF_Click);
            // 
            // chkLWRFOn
            // 
            this.chkLWRFOn.AutoSize = true;
            this.chkLWRFOn.Location = new System.Drawing.Point(121, 162);
            this.chkLWRFOn.Name = "chkLWRFOn";
            this.chkLWRFOn.Size = new System.Drawing.Size(40, 17);
            this.chkLWRFOn.TabIndex = 7;
            this.chkLWRFOn.Text = "On";
            this.chkLWRFOn.UseVisualStyleBackColor = true;
            // 
            // txtLWRFRoom
            // 
            this.txtLWRFRoom.Location = new System.Drawing.Point(221, 159);
            this.txtLWRFRoom.Name = "txtLWRFRoom";
            this.txtLWRFRoom.Size = new System.Drawing.Size(46, 20);
            this.txtLWRFRoom.TabIndex = 8;
            this.txtLWRFRoom.Text = "0";
            // 
            // txtLWRFDevice
            // 
            this.txtLWRFDevice.Location = new System.Drawing.Point(324, 160);
            this.txtLWRFDevice.Name = "txtLWRFDevice";
            this.txtLWRFDevice.Size = new System.Drawing.Size(36, 20);
            this.txtLWRFDevice.TabIndex = 9;
            this.txtLWRFDevice.Text = "0";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(177, 162);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Room:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(274, 162);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Device:";
            // 
            // butSay
            // 
            this.butSay.Location = new System.Drawing.Point(27, 110);
            this.butSay.Name = "butSay";
            this.butSay.Size = new System.Drawing.Size(75, 23);
            this.butSay.TabIndex = 12;
            this.butSay.Text = "Speak";
            this.butSay.UseVisualStyleBackColor = true;
            this.butSay.Click += new System.EventHandler(this.butSay_Click);
            // 
            // txtSpeech
            // 
            this.txtSpeech.Location = new System.Drawing.Point(317, 113);
            this.txtSpeech.Name = "txtSpeech";
            this.txtSpeech.Size = new System.Drawing.Size(251, 20);
            this.txtSpeech.TabIndex = 13;
            this.txtSpeech.Text = "This is a test message";
            // 
            // chkStartOnConnect
            // 
            this.chkStartOnConnect.AutoSize = true;
            this.chkStartOnConnect.Location = new System.Drawing.Point(95, 71);
            this.chkStartOnConnect.Name = "chkStartOnConnect";
            this.chkStartOnConnect.Size = new System.Drawing.Size(105, 17);
            this.chkStartOnConnect.TabIndex = 14;
            this.chkStartOnConnect.Text = "Start on connect";
            this.chkStartOnConnect.UseVisualStyleBackColor = true;
            // 
            // butSayRemote
            // 
            this.butSayRemote.Location = new System.Drawing.Point(109, 109);
            this.butSayRemote.Name = "butSayRemote";
            this.butSayRemote.Size = new System.Drawing.Size(91, 23);
            this.butSayRemote.TabIndex = 15;
            this.butSayRemote.Text = "Speak Remote";
            this.butSayRemote.UseVisualStyleBackColor = true;
            this.butSayRemote.Click += new System.EventHandler(this.butSayRemote_Click);
            // 
            // txtRemoteID
            // 
            this.txtRemoteID.Location = new System.Drawing.Point(207, 113);
            this.txtRemoteID.Name = "txtRemoteID";
            this.txtRemoteID.Size = new System.Drawing.Size(100, 20);
            this.txtRemoteID.TabIndex = 16;
            this.txtRemoteID.Text = "192.168.1.191:";
            // 
            // butPlayWav
            // 
            this.butPlayWav.Location = new System.Drawing.Point(206, 84);
            this.butPlayWav.Name = "butPlayWav";
            this.butPlayWav.Size = new System.Drawing.Size(124, 23);
            this.butPlayWav.TabIndex = 18;
            this.butPlayWav.Text = "Play Wav Remote";
            this.butPlayWav.UseVisualStyleBackColor = true;
            this.butPlayWav.Click += new System.EventHandler(this.butPlayWav_Click);
            // 
            // butAlarmRecur
            // 
            this.butAlarmRecur.Location = new System.Drawing.Point(200, 58);
            this.butAlarmRecur.Name = "butAlarmRecur";
            this.butAlarmRecur.Size = new System.Drawing.Size(75, 23);
            this.butAlarmRecur.TabIndex = 20;
            this.butAlarmRecur.Text = "Create Alarm";
            this.butAlarmRecur.UseVisualStyleBackColor = true;
            this.butAlarmRecur.Click += new System.EventHandler(this.butAlarmRecur_Click);
            // 
            // txtAlarmTime
            // 
            this.txtAlarmTime.Location = new System.Drawing.Point(175, 15);
            this.txtAlarmTime.Name = "txtAlarmTime";
            this.txtAlarmTime.Size = new System.Drawing.Size(100, 20);
            this.txtAlarmTime.TabIndex = 21;
            this.txtAlarmTime.Text = "10";
            // 
            // txtAlarmRecur
            // 
            this.txtAlarmRecur.Location = new System.Drawing.Point(175, 36);
            this.txtAlarmRecur.Name = "txtAlarmRecur";
            this.txtAlarmRecur.Size = new System.Drawing.Size(41, 20);
            this.txtAlarmRecur.TabIndex = 22;
            this.txtAlarmRecur.Text = "5";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(107, 39);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 13);
            this.label3.TabIndex = 23;
            this.label3.Text = "Recurring (s):";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(107, 18);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(33, 13);
            this.label4.TabIndex = 24;
            this.label4.Text = "Time:";
            // 
            // radioAlarmTime
            // 
            this.radioAlarmTime.AutoSize = true;
            this.radioAlarmTime.Location = new System.Drawing.Point(14, 16);
            this.radioAlarmTime.Name = "radioAlarmTime";
            this.radioAlarmTime.Size = new System.Drawing.Size(57, 17);
            this.radioAlarmTime.TabIndex = 25;
            this.radioAlarmTime.Text = "At time";
            this.radioAlarmTime.UseVisualStyleBackColor = true;
            // 
            // radioAlarmS
            // 
            this.radioAlarmS.AutoSize = true;
            this.radioAlarmS.Checked = true;
            this.radioAlarmS.Location = new System.Drawing.Point(14, 37);
            this.radioAlarmS.Name = "radioAlarmS";
            this.radioAlarmS.Size = new System.Drawing.Size(85, 17);
            this.radioAlarmS.TabIndex = 26;
            this.radioAlarmS.TabStop = true;
            this.radioAlarmS.Text = "In x seconds";
            this.radioAlarmS.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.txtAlarmSpeak);
            this.groupBox1.Controls.Add(this.chkAlarmSayTime);
            this.groupBox1.Controls.Add(this.chkAlarmBeep);
            this.groupBox1.Controls.Add(this.txtAlarmID);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.radioAlarmS);
            this.groupBox1.Controls.Add(this.butAlarmRecur);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.radioAlarmTime);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtAlarmTime);
            this.groupBox1.Controls.Add(this.txtAlarmRecur);
            this.groupBox1.Location = new System.Drawing.Point(16, 200);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(485, 89);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Alarm";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(308, 53);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(28, 13);
            this.label6.TabIndex = 32;
            this.label6.Text = "Say:";
            // 
            // txtAlarmSpeak
            // 
            this.txtAlarmSpeak.Location = new System.Drawing.Point(342, 50);
            this.txtAlarmSpeak.Name = "txtAlarmSpeak";
            this.txtAlarmSpeak.Size = new System.Drawing.Size(100, 20);
            this.txtAlarmSpeak.TabIndex = 31;
            // 
            // chkAlarmSayTime
            // 
            this.chkAlarmSayTime.AutoSize = true;
            this.chkAlarmSayTime.Location = new System.Drawing.Point(308, 34);
            this.chkAlarmSayTime.Name = "chkAlarmSayTime";
            this.chkAlarmSayTime.Size = new System.Drawing.Size(70, 17);
            this.chkAlarmSayTime.TabIndex = 30;
            this.chkAlarmSayTime.Text = "Say Time";
            this.chkAlarmSayTime.UseVisualStyleBackColor = true;
            // 
            // chkAlarmBeep
            // 
            this.chkAlarmBeep.AutoSize = true;
            this.chkAlarmBeep.Location = new System.Drawing.Point(308, 16);
            this.chkAlarmBeep.Name = "chkAlarmBeep";
            this.chkAlarmBeep.Size = new System.Drawing.Size(79, 17);
            this.chkAlarmBeep.TabIndex = 29;
            this.chkAlarmBeep.Text = "Play Beeps";
            this.chkAlarmBeep.UseVisualStyleBackColor = true;
            // 
            // txtAlarmID
            // 
            this.txtAlarmID.Location = new System.Drawing.Point(77, 58);
            this.txtAlarmID.Name = "txtAlarmID";
            this.txtAlarmID.Size = new System.Drawing.Size(100, 20);
            this.txtAlarmID.TabIndex = 28;
            this.txtAlarmID.Text = "alarmName";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 61);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(58, 13);
            this.label5.TabIndex = 27;
            this.label5.Text = "Unique ID:";
            // 
            // prog1
            // 
            this.prog1.Location = new System.Drawing.Point(632, 11);
            this.prog1.Name = "prog1";
            this.prog1.Size = new System.Drawing.Size(100, 23);
            this.prog1.TabIndex = 28;
            // 
            // prog2
            // 
            this.prog2.Location = new System.Drawing.Point(632, 41);
            this.prog2.Name = "prog2";
            this.prog2.Size = new System.Drawing.Size(100, 23);
            this.prog2.TabIndex = 29;
            // 
            // prog3
            // 
            this.prog3.Location = new System.Drawing.Point(632, 71);
            this.prog3.Name = "prog3";
            this.prog3.Size = new System.Drawing.Size(100, 23);
            this.prog3.TabIndex = 30;
            // 
            // lblProg1
            // 
            this.lblProg1.AutoSize = true;
            this.lblProg1.Location = new System.Drawing.Point(524, 14);
            this.lblProg1.Name = "lblProg1";
            this.lblProg1.Size = new System.Drawing.Size(35, 13);
            this.lblProg1.TabIndex = 31;
            this.lblProg1.Text = "label7";
            // 
            // lblProg2
            // 
            this.lblProg2.AutoSize = true;
            this.lblProg2.Location = new System.Drawing.Point(524, 45);
            this.lblProg2.Name = "lblProg2";
            this.lblProg2.Size = new System.Drawing.Size(35, 13);
            this.lblProg2.TabIndex = 32;
            this.lblProg2.Text = "label8";
            // 
            // lblProg3
            // 
            this.lblProg3.AutoSize = true;
            this.lblProg3.Location = new System.Drawing.Point(524, 79);
            this.lblProg3.Name = "lblProg3";
            this.lblProg3.Size = new System.Drawing.Size(35, 13);
            this.lblProg3.TabIndex = 33;
            this.lblProg3.Text = "label9";
            // 
            // txtProg1
            // 
            this.txtProg1.Location = new System.Drawing.Point(738, 11);
            this.txtProg1.Name = "txtProg1";
            this.txtProg1.Size = new System.Drawing.Size(37, 20);
            this.txtProg1.TabIndex = 34;
            // 
            // txtProg2
            // 
            this.txtProg2.Location = new System.Drawing.Point(738, 42);
            this.txtProg2.Name = "txtProg2";
            this.txtProg2.Size = new System.Drawing.Size(37, 20);
            this.txtProg2.TabIndex = 35;
            // 
            // txtProg3
            // 
            this.txtProg3.Location = new System.Drawing.Point(738, 76);
            this.txtProg3.Name = "txtProg3";
            this.txtProg3.Size = new System.Drawing.Size(37, 20);
            this.txtProg3.TabIndex = 36;
            // 
            // progCmd1
            // 
            this.progCmd1.Location = new System.Drawing.Point(781, 12);
            this.progCmd1.Name = "progCmd1";
            this.progCmd1.Size = new System.Drawing.Size(100, 23);
            this.progCmd1.TabIndex = 37;
            // 
            // progCmd2
            // 
            this.progCmd2.Location = new System.Drawing.Point(782, 41);
            this.progCmd2.Name = "progCmd2";
            this.progCmd2.Size = new System.Drawing.Size(100, 23);
            this.progCmd2.TabIndex = 38;
            // 
            // progCmd3
            // 
            this.progCmd3.Location = new System.Drawing.Point(782, 70);
            this.progCmd3.Name = "progCmd3";
            this.progCmd3.Size = new System.Drawing.Size(100, 23);
            this.progCmd3.TabIndex = 39;
            // 
            // txtProgCmd1
            // 
            this.txtProgCmd1.Location = new System.Drawing.Point(888, 13);
            this.txtProgCmd1.Name = "txtProgCmd1";
            this.txtProgCmd1.Size = new System.Drawing.Size(37, 20);
            this.txtProgCmd1.TabIndex = 40;
            // 
            // txtProgCmd2
            // 
            this.txtProgCmd2.Location = new System.Drawing.Point(888, 44);
            this.txtProgCmd2.Name = "txtProgCmd2";
            this.txtProgCmd2.Size = new System.Drawing.Size(37, 20);
            this.txtProgCmd2.TabIndex = 41;
            // 
            // txtProgCmd3
            // 
            this.txtProgCmd3.Location = new System.Drawing.Point(888, 71);
            this.txtProgCmd3.Name = "txtProgCmd3";
            this.txtProgCmd3.Size = new System.Drawing.Size(37, 20);
            this.txtProgCmd3.TabIndex = 42;
            // 
            // ckUseSphinx
            // 
            this.ckUseSphinx.AutoSize = true;
            this.ckUseSphinx.Location = new System.Drawing.Point(324, 19);
            this.ckUseSphinx.Name = "ckUseSphinx";
            this.ckUseSphinx.Size = new System.Drawing.Size(104, 17);
            this.ckUseSphinx.TabIndex = 43;
            this.ckUseSphinx.Text = "Use CMUSphinx";
            this.ckUseSphinx.UseVisualStyleBackColor = true;
            this.ckUseSphinx.CheckedChanged += new System.EventHandler(this.ckUseSphinx_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(959, 567);
            this.Controls.Add(this.ckUseSphinx);
            this.Controls.Add(this.txtProgCmd3);
            this.Controls.Add(this.txtProgCmd2);
            this.Controls.Add(this.txtProgCmd1);
            this.Controls.Add(this.progCmd3);
            this.Controls.Add(this.progCmd2);
            this.Controls.Add(this.progCmd1);
            this.Controls.Add(this.txtProg3);
            this.Controls.Add(this.txtProg2);
            this.Controls.Add(this.txtProg1);
            this.Controls.Add(this.lblProg3);
            this.Controls.Add(this.lblProg2);
            this.Controls.Add(this.lblProg1);
            this.Controls.Add(this.prog3);
            this.Controls.Add(this.prog2);
            this.Controls.Add(this.prog1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.butPlayWav);
            this.Controls.Add(this.txtRemoteID);
            this.Controls.Add(this.butSayRemote);
            this.Controls.Add(this.chkStartOnConnect);
            this.Controls.Add(this.txtSpeech);
            this.Controls.Add(this.butSay);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtLWRFDevice);
            this.Controls.Add(this.txtLWRFRoom);
            this.Controls.Add(this.chkLWRFOn);
            this.Controls.Add(this.butLWRF);
            this.Controls.Add(this.butRecogWav);
            this.Controls.Add(this.butStop);
            this.Controls.Add(this.butStart);
            this.Controls.Add(this.txtLog);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Button butStart;
        private System.Windows.Forms.Button butStop;
        private System.Windows.Forms.Button butRecogWav;
        private System.Windows.Forms.Button butLWRF;
        private System.Windows.Forms.CheckBox chkLWRFOn;
        private System.Windows.Forms.TextBox txtLWRFRoom;
        private System.Windows.Forms.TextBox txtLWRFDevice;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button butSay;
        private System.Windows.Forms.TextBox txtSpeech;
        private System.Windows.Forms.CheckBox chkStartOnConnect;
        private System.Windows.Forms.Button butSayRemote;
        private System.Windows.Forms.TextBox txtRemoteID;
        private System.Windows.Forms.Button butPlayWav;
        private System.Windows.Forms.Button butAlarmRecur;
        private System.Windows.Forms.TextBox txtAlarmTime;
        private System.Windows.Forms.TextBox txtAlarmRecur;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RadioButton radioAlarmTime;
        private System.Windows.Forms.RadioButton radioAlarmS;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtAlarmID;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox chkAlarmSayTime;
        private System.Windows.Forms.CheckBox chkAlarmBeep;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtAlarmSpeak;
        private System.Windows.Forms.ProgressBar prog1;
        private System.Windows.Forms.ProgressBar prog2;
        private System.Windows.Forms.ProgressBar prog3;
        private System.Windows.Forms.Label lblProg1;
        private System.Windows.Forms.Label lblProg2;
        private System.Windows.Forms.Label lblProg3;
        private System.Windows.Forms.TextBox txtProg1;
        private System.Windows.Forms.TextBox txtProg2;
        private System.Windows.Forms.TextBox txtProg3;
        private System.Windows.Forms.ProgressBar progCmd1;
        private System.Windows.Forms.ProgressBar progCmd2;
        private System.Windows.Forms.ProgressBar progCmd3;
        private System.Windows.Forms.TextBox txtProgCmd1;
        private System.Windows.Forms.TextBox txtProgCmd2;
        private System.Windows.Forms.TextBox txtProgCmd3;
        private System.Windows.Forms.CheckBox ckUseSphinx;
    }
}

