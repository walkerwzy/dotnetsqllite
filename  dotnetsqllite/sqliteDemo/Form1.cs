using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace sqliteDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            dataGridView1.DataSource = db.getList("").Tables[0];
        }

        private void button1_Click(object sender, EventArgs e)
        {
            model m = new model();
            
            //m.id=
            m.uname = textBox1.Text;
            m.udes = textBox2.Text;
            m.title = textBox3.Text;
            m.remark = textBox4.Text;
            db.add(m);
            dataGridView1.DataSource = db.getList("").Tables[0];
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "a.txt", FileMode.Append);
            //XmlReader xr = db.getXml();
            //while (xr.ReadElementContentAsString)
            //{
                
            //}
            //sw.Flush();
            //sw.Close();
            //fs.Flush();
            //fs.Close();
            //label1.Text = db.getXml().
            //db.getList("").WriteXml(AppDomain.CurrentDomain.BaseDirectory + "a.txt", XmlWriteMode.IgnoreSchema);
        }
    }
}
