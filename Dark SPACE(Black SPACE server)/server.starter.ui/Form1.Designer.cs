namespace server.starter.ui
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.metroToolTip1 = new MetroFramework.Components.MetroToolTip();
            this.metroButton1 = new MetroFramework.Controls.MetroButton();
            this.metroButton4 = new MetroFramework.Controls.MetroButton();
            this.metroButton5 = new MetroFramework.Controls.MetroButton();
            this.metroButton7 = new MetroFramework.Controls.MetroButton();
            this.metroButton8 = new MetroFramework.Controls.MetroButton();
            this.metroTextBox3 = new MetroFramework.Controls.MetroTextBox();
            this.stopButton = new MetroFramework.Controls.MetroButton();
            this.SuspendLayout();
            // 
            // metroButton1
            // 
            this.metroButton1.Location = new System.Drawing.Point(56, 55);
            this.metroButton1.Name = "metroButton1";
            this.metroButton1.Size = new System.Drawing.Size(286, 57);
            this.metroButton1.TabIndex = 0;
            this.metroButton1.Text = "рестарт сервера";
            this.metroButton1.Click += new System.EventHandler(this.metroButton1_Click);
            // 
            // metroButton4
            // 
            this.metroButton4.Location = new System.Drawing.Point(56, 131);
            this.metroButton4.Name = "metroButton4";
            this.metroButton4.Size = new System.Drawing.Size(122, 74);
            this.metroButton4.TabIndex = 5;
            this.metroButton4.Text = "Апач";
            this.metroButton4.Click += new System.EventHandler(this.metroButton4_Click);
            // 
            // metroButton5
            // 
            this.metroButton5.Location = new System.Drawing.Point(226, 131);
            this.metroButton5.Name = "metroButton5";
            this.metroButton5.Size = new System.Drawing.Size(116, 74);
            this.metroButton5.TabIndex = 6;
            this.metroButton5.Text = "mysql";
            this.metroButton5.Click += new System.EventHandler(this.metroButton5_Click);
            // 
            // metroButton7
            // 
            this.metroButton7.Location = new System.Drawing.Point(554, 131);
            this.metroButton7.Name = "metroButton7";
            this.metroButton7.Size = new System.Drawing.Size(121, 74);
            this.metroButton7.TabIndex = 9;
            this.metroButton7.Text = "уебать базу";
            this.metroButton7.Click += new System.EventHandler(this.metroButton7_Click);
            // 
            // metroButton8
            // 
            this.metroButton8.Location = new System.Drawing.Point(386, 131);
            this.metroButton8.Name = "metroButton8";
            this.metroButton8.Size = new System.Drawing.Size(115, 74);
            this.metroButton8.TabIndex = 8;
            this.metroButton8.Text = "уебать апач";
            this.metroButton8.Click += new System.EventHandler(this.metroButton8_Click);
            // 
            // metroTextBox3
            // 
            this.metroTextBox3.Location = new System.Drawing.Point(-1, 211);
            this.metroTextBox3.Name = "metroTextBox3";
            this.metroTextBox3.Size = new System.Drawing.Size(731, 124);
            this.metroTextBox3.TabIndex = 10;
            this.metroTextBox3.Text = "Логи";
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(386, 55);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(289, 57);
            this.stopButton.TabIndex = 11;
            this.stopButton.Text = "Уебать эмуллятор";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click_1);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Blue;
            this.BorderStyle = MetroFramework.Drawing.MetroBorderStyle.None;
            this.ClientSize = new System.Drawing.Size(730, 342);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.metroTextBox3);
            this.Controls.Add(this.metroButton7);
            this.Controls.Add(this.metroButton8);
            this.Controls.Add(this.metroButton5);
            this.Controls.Add(this.metroButton4);
            this.Controls.Add(this.metroButton1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Resizable = false;
            this.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private MetroFramework.Components.MetroToolTip metroToolTip1;
        private MetroFramework.Controls.MetroButton metroButton1;
        private MetroFramework.Controls.MetroButton metroButton4;
        private MetroFramework.Controls.MetroButton metroButton5;
        private MetroFramework.Controls.MetroButton metroButton7;
        private MetroFramework.Controls.MetroButton metroButton8;
        public MetroFramework.Controls.MetroTextBox metroTextBox3;
        private MetroFramework.Controls.MetroButton stopButton;
    }
}

