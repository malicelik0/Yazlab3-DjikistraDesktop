﻿using System;
using System.Windows.Forms;

namespace TSP
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();
        }

        private void FormAbout_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void lnklblEmail_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("mailto:" + "mrtebulute@gmail.com" + "?subject=" + "Yazlab-2");
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void lbll_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}
