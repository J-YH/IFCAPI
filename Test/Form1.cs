using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IFCAPI.Database;

namespace Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Title = "Load IFC File";
            file.Filter = "ifc file (*.ifc)|*.ifc";

            if(file.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = file.FileName;
                    
                IFCDatabase ifcDB = IFCDatabase.Instance;
                //ifcDB.initDB(@"E:\JingYing\IFCPlatform\IFCLib\TestDB\TestDB.db");
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Reset();
                sw.Start();
                richTextBox1.Text = ifcDB.loadIFCFile(file.FileName, "TestIFC");
                sw.Stop();
                richTextBox1.Text += "\r\n共花費:" + sw.Elapsed.TotalMilliseconds.ToString() + "毫秒";
                
            }

            
        }
    }
}
